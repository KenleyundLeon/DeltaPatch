using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaPatch;

public static class Utils
{
    private static bool allowPrerelease = Main.Instance.Config.AllowPrerelease;
    private static string? Token => Main.Instance?.Config?.GithubApiKey;
    public static DateTime Timeout;

    private static HttpClient CreateHttpClient(TimeSpan? timeout = null)
    {
        var client = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DeltaPatch-Updater/2.0");
        if (!string.IsNullOrEmpty(Token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", Token);
        return client;
    }

    public static List<(string GithubRepo, string PluginName, string FilePath, Version pluginVersion)> GetPluginInfos(DirectoryInfo confDir)
    {
        var pluginRepos = new List<(string GithubRepo, string PluginName, string FilePath, Version pluginVersion)>();
        const string prefix = "https://github.com/";

        void AddOrUpdatePluginStatus(string name, string status)
        {
            if (!Main.Instance.pluginUpdates.Any(x => x.pluginName == name && x.pluginStatus == status))
            {
                Main.Instance.pluginUpdates.RemoveAll(x => x.pluginName == name);
                Main.Instance.pluginUpdates.Add((name, status));
            }
        }

        foreach (var pl in PluginLoader.Plugins)
        {
            if (pl.Key.FilePath == null)
                continue;

            string? repoUrl = null;

            foreach (var attr in pl.Value.GetCustomAttributes<AssemblyMetadataAttribute>())
            {
                if (attr.Key == "RepositoryUrl" && !string.IsNullOrEmpty(attr.Value))
                {
                    repoUrl = attr.Value;
                    break;
                }
            }

            if (repoUrl == null)
            {
                foreach (var attr in pl.Value.GetCustomAttributes<AssemblyCompanyAttribute>())
                {
                    var url = attr.Company;
                    if (!string.IsNullOrEmpty(url) && url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        repoUrl = url;
                        break;
                    }
                }
            }

            bool repoFound = false;

            if (!string.IsNullOrEmpty(repoUrl))
            {
                var repoPath = repoUrl.StartsWith(prefix) ? repoUrl.Substring(prefix.Length) : repoUrl;
                pluginRepos.Add((repoPath, pl.Key.Name, pl.Key.FilePath, pl.Key.Version));
                LogMsg("debug", repoPath);
                repoFound = true;
            }

            if ((!repoFound) && (!Server.Host?.IsDestroyed ?? false))
            {
                foreach (Plugin plugin in PluginLoader.EnabledPlugins)
                {
                    var repo = plugin.GetType()
                        .GetField("githubRepo", BindingFlags.Public | BindingFlags.Instance)?
                        .GetValue(plugin)?
                        .ToString();

                    var pluginFile = plugin.GetType()
                        .GetProperty("FilePath", BindingFlags.Public | BindingFlags.Instance)?
                        .GetValue(plugin)?
                        .ToString();

                    if (!string.IsNullOrEmpty(repo) && !string.IsNullOrEmpty(pluginFile) && plugin.Name == pl.Key.Name)
                    {
                        pluginRepos.Add((repo, plugin.Name, pluginFile, plugin.Version));
                        repoFound = true;
                        break;
                    }
                }
            }

            if (!repoFound)
            {
                AddOrUpdatePluginStatus(pl.Key.Name, "Not Supported");
            }
        }

        return pluginRepos;
    }

    private static async Task<(string? tag, string? dllAssetId, string? zipAssetId, DateTime? publishedAt, string? dllName)> GetLatestReleaseAsync(string githubRepo, string pluginName)
    {
        try
        {
            HttpClient client = CreateHttpClient();

            string url = allowPrerelease
                ? $"https://api.github.com/repos/{githubRepo}/releases"
                : $"https://api.github.com/repos/{githubRepo}/releases/latest";

            LogMsg("info", $"[FETCHING] {(allowPrerelease ? "latest pre-release" : "latest release")} for {githubRepo}...");

            HttpResponseMessage resp = await client.GetAsync(url);

            if (resp.ReasonPhrase == "rate limit exceeded")
            {
                Timeout = DateTime.Now.AddMinutes(2);
                return default;
            }
            else if (!resp.IsSuccessStatusCode) 
            {
                LogMsg("warn", $"[SKIPPING] repo '{githubRepo}' – Status: {resp.StatusCode}");
                return default;
            }

            string json = await resp.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);

            JsonElement latest = doc.RootElement.EnumerateArray().FirstOrDefault();

            if (latest.ValueKind == JsonValueKind.Undefined)
                return default;

            string tag = latest.GetProperty("tag_name").GetString();
            DateTime publishedAt = DateTime.Parse(latest.GetProperty("published_at").GetString());

            string? dllAssetId = null;
            string? zipAssetId = null;
            string? dllName = null;

            if (latest.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string? name = asset.GetPropertyOrDefault("name")?.GetString();

                    if (string.IsNullOrEmpty(name)) continue;

                    if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && string.Equals(Path.GetFileNameWithoutExtension(name), pluginName, StringComparison.OrdinalIgnoreCase))
                    {
                        dllAssetId = asset.GetPropertyOrDefault("id")?.GetRawText();
                        dllName = name;
                    }
                    else if (name.Equals("dependencies.zip", StringComparison.OrdinalIgnoreCase))
                    {
                        zipAssetId = asset.GetPropertyOrDefault("id")?.GetRawText();
                    }
                    else continue;
                }
            }

            return (tag, dllAssetId, zipAssetId, publishedAt, dllName);
        }
        catch (Exception ex)
        {
            LogMsg("error", $"Request failed for {githubRepo}: {ex.Message}");
            return default;
        }
    }

    public static async Task<(string githubRepo, string? tag, DateTime? publishedAt)> GetLatestReleaseDateAsync(string githubRepo)
    {
        var (tag, _, _, publishedAt, _) = await GetLatestReleaseAsync(githubRepo, "");
        return (githubRepo, tag, publishedAt);
    }

    public static async Task<bool> UpdatePlugin(string filePath, string githubRepo)
    {
        try
        {
            string pluginName = Path.GetFileNameWithoutExtension(filePath);

            var (tag, dllAssetId, zipAssetId, _, dllName) = await GetLatestReleaseAsync(githubRepo, pluginName);

            if (string.IsNullOrEmpty(dllAssetId))
            {
                LogMsg("warn", $"No matching DLL found for plugin '{pluginName}' in repo {githubRepo}");
                return false;
            }

            string targetPath = Path.Combine(Path.GetDirectoryName(filePath)!, dllName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            using var http = CreateHttpClient(TimeSpan.FromMinutes(2));
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

            LogMsg("info", $"[DOWNLOADING] DLL '{dllName}' for {githubRepo}");
            var resp = await http.GetAsync($"https://api.github.com/repos/{githubRepo}/releases/assets/{dllAssetId}", HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();

            using (var stream = await resp.Content.ReadAsStreamAsync())
            using (var file = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(file);
            }

            LogMsg("info", $"[UPDATED] {githubRepo} DLL '{dllName}' to version {tag}");

            if (!string.IsNullOrEmpty(zipAssetId))
            {
                LogMsg("info", $"[DOWNLOADING] dependencies.zip for {githubRepo}");

                string pluginDir = Path.GetDirectoryName(filePath)!;
                var portFolder = new DirectoryInfo(pluginDir).Name;
                var baseDir = new DirectoryInfo(pluginDir).Parent!.Parent!.FullName;
                string depDir = Path.Combine(baseDir, "dependencies", portFolder);
                Directory.CreateDirectory(depDir);

                string zipPath = Path.Combine(depDir, "dependencies.zip");

                var response = await http.GetAsync($"https://api.github.com/repos/{githubRepo}/releases/assets/{zipAssetId}", HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using (var zipStream = await response.Content.ReadAsStreamAsync())
                using (var zipFs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await zipStream.CopyToAsync(zipFs);
                }

                LogMsg("info", $"[EXTRACTING] dependencies.zip for {githubRepo}");

                using var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

                foreach (var entry in archive.Entries)
                {
                    string destPath = Path.Combine(depDir, entry.FullName);
                    string? destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir))
                        Directory.CreateDirectory(destDir);

                    entry.ExtractToFile(destPath, true);
                    LogMsg("info", $"[EXTRACTED] {entry.FullName}");
                }

                File.Delete(zipPath);
                LogMsg("info", $"[DONE] dependencies.zip processed for {githubRepo}");
            }
            else
            {
                LogMsg("info", $"[SKIPPED] No dependencies.zip found for {githubRepo}");
            }

            return true;
        }
        catch (IOException ioEx)
        {
            LogMsg("error", $"[IO ERROR] Update failed for {githubRepo}: {ioEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            LogMsg("error", $"[ERROR] Update failed for {githubRepo}: {ex.Message}");
            return false;
        }
    }

    public static void LogMsg(string status, string msg)
    {
        if (!Main.Instance.Config.EnableLogging) return;
        switch (status.ToLower())
        {
            case "info":
                Logger.Info(msg);
                break;
            case "debug":
                Logger.Debug(msg);
                break;
            case "warn":
                Logger.Warn(msg);
                break;
            case "error":
                Logger.Error(msg);
                break;
            default:
                Logger.Info(msg);
                break;
        }
    }

    private static JsonElement? GetPropertyOrDefault(this JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop) ? prop : null;

    public static async Task SendDiscordWebhookMessage(string pluginName, string oldVersion, string newVersion)
    {
        if (!Main.Instance.Config.DiscordWebhookNotification) return;
        if (!Main.Instance.Config.DiscordWebhook.Any()) return;

        var payload = new
        {
            content = "",
            tts = false,
            embeds = new[]
        {
            new
            {
                id = 652627557,
                title = "Plugin Updated - Requesting Reboot",
                description = $"{pluginName} - Current Version: {oldVersion} - New Version {newVersion}",
                color = 2326507,
                fields = Array.Empty<object>()
            }
        },
            components = Array.Empty<object>(),
            actions = new { },
            flags = 0,
            username = Server.ServerListName,
            avatar_url = "https://image2url.com/images/1759611430674-abc9ea56-8150-475c-a673-24db66c2b634.png"
        };

        HttpClient client = CreateHttpClient();
        var json = JsonSerializer.Serialize(payload);

        await client.PostAsync(
            Main.Instance.Config.DiscordWebhook,
            new StringContent(json, Encoding.UTF8, "application/json")
        );
    }
}
