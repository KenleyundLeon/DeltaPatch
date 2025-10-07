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
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaPatch;

public static class Utils
{
    private static string? Token => Main.Instance?.Config?.GithubApiKey;

    private static HttpClient CreateHttpClient(TimeSpan? timeout = null)
    {
        var client = new HttpClient { Timeout = timeout ?? TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("DeltaPatch-Updater");
        if (!string.IsNullOrEmpty(Token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", Token);
        return client;
    }

    public static List<(string GithubRepo, string FilePath)> GetGithubReposWithFile(DirectoryInfo confDir)
    {
        List<(string GithubRepo, string FilePath)> pluginRepos = [];

        var prefix = "https://github.com/";

        foreach (var pl in PluginLoader.Plugins)
        {
            var metadataAttributes = pl.Value.GetCustomAttributes<AssemblyMetadataAttribute>();

            foreach (var attr in metadataAttributes)
            {
                if (attr.Key != "RepositoryUrl") continue;

                if (pl.Key.FilePath != null && attr.Value != null)
                {
                    var pluginFile = attr.Value.StartsWith(prefix)
                        ? attr.Value.Substring(prefix.Length)
                        : attr.Value;

                    pluginRepos.Add((pluginFile, pl.Key.FilePath));
                    Logger.Debug(pluginFile);
                }
            }
        }

        if (!Server.Host?.IsDestroyed ?? false)
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

                if (!string.IsNullOrEmpty(repo) && !string.IsNullOrEmpty(pluginFile))
                {
                    var cleanedRepo = repo.StartsWith(prefix)
                        ? repo.Substring(prefix.Length)
                        : repo;

                    pluginRepos.Add((cleanedRepo, pluginFile));
                }
            }
        }

        return pluginRepos.Any() ? pluginRepos : [];
    }

    private static async Task<(string? tag, string? dllAssetId, string? zipAssetId, DateTime? publishedAt, string? dllName)> GetLatestReleaseAsync(string githubRepo, string pluginName)
    {
        try
        {
            using var http = CreateHttpClient();
            bool allowPrerelease = Main.Instance.Config.AllowPrerelease;

            string url = allowPrerelease
                ? $"https://api.github.com/repos/{githubRepo}/releases"
                : $"https://api.github.com/repos/{githubRepo}/releases/latest";

            LogMsg("info", $"[FETCHING] {(allowPrerelease ? "latest pre-release" : "latest release")} for {githubRepo}...");

            using var resp = await http.GetAsync(url).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                LogMsg("warn", $"[SKIPPING] repo '{githubRepo}' – Status: {resp.StatusCode}");
                return default;
            }

            using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

            JsonElement releaseElement;
            if (allowPrerelease)
            {
                var releases = doc.RootElement.EnumerateArray()
                    .OrderByDescending(r => DateTime.TryParse(r.GetPropertyOrDefault("published_at")?.GetString(), out var d) ? d : DateTime.MinValue)
                    .ToList();

                releaseElement = releases.FirstOrDefault(r =>
                    r.TryGetProperty("prerelease", out var pre) && pre.GetBoolean());

                if (releaseElement.ValueKind == JsonValueKind.Undefined)
                    releaseElement = releases.FirstOrDefault();
            }
            else releaseElement = doc.RootElement;

            if (releaseElement.ValueKind == JsonValueKind.Undefined)
                return default;

            var tag = releaseElement.GetPropertyOrDefault("tag_name")?.GetString();
            DateTime? publishedAt = DateTime.TryParse(releaseElement.GetPropertyOrDefault("published_at")?.GetString(), out var date)
                ? date
                : (DateTime?)null;

            string? dllAssetId = null;
            string? zipAssetId = null;
            string? dllName = null;

            if (releaseElement.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string? name = asset.GetPropertyOrDefault("name")?.GetString();
                    if (string.IsNullOrEmpty(name)) continue;

                    // LogMsg("info", $"[ASSET FOUND] {name}");

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

    public static bool UpdatePlugin(string filePath, string githubRepo)
    {
        try
        {
            return Task.Run(async () =>
            {
                string pluginName = Path.GetFileNameWithoutExtension(filePath);
                var (tag, dllAssetId, zipAssetId, _, dllName) = await GetLatestReleaseAsync(githubRepo, pluginName);

                if (string.IsNullOrEmpty(dllAssetId))
                {
                    LogMsg("warn", $"No matching DLL found for plugin '{pluginName}' in repo {githubRepo}");
                    return false;
                }

                var targetPath = Path.Combine(Path.GetDirectoryName(filePath)!, dllName);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                using (var http = CreateHttpClient(TimeSpan.FromMinutes(2)))
                {
                    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                    LogMsg("info", $"[DOWNLOADING] DLL '{dllName}' for {githubRepo}");
                    using (var resp = await http.GetAsync($"https://api.github.com/repos/{githubRepo}/releases/assets/{dllAssetId}", HttpCompletionOption.ResponseHeadersRead))
                    {
                        resp.EnsureSuccessStatusCode();
                        using var stream = await resp.Content.ReadAsStreamAsync();
                        using var file = File.Create(targetPath);
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
                        Logger.Error($"{filePath} | {pluginDir} | {portFolder} | {baseDir} | {depDir} | {zipPath}");
                        using (var resp = await http.GetAsync($"https://api.github.com/repos/{githubRepo}/releases/assets/{zipAssetId}", HttpCompletionOption.ResponseHeadersRead))
                        {
                            resp.EnsureSuccessStatusCode();
                            using (var zipStream = await resp.Content.ReadAsStreamAsync())
                            using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await zipStream.CopyToAsync(fs);
                            }
                        }

                        LogMsg("info", $"[EXTRACTING] dependencies.zip for {githubRepo}");

                        using (var fs = new FileStream(zipPath, FileMode.Open))
                        using (var archive = new ZipArchive(fs, ZipArchiveMode.Read))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                string destPath = Path.Combine(depDir, entry.FullName);
                                string? destDir = Path.GetDirectoryName(destPath);
                                if (!string.IsNullOrEmpty(destDir))
                                    Directory.CreateDirectory(destDir);

                                entry.ExtractToFile(destPath, true);
                                LogMsg("info", $"[EXTRACTED] {entry.FullName}");
                            }
                        }

                        File.Delete(zipPath);
                        LogMsg("info", $"[DONE] dependencies.zip processed for {githubRepo}");
                    }
                    else
                    {
                        LogMsg("info", $"[SKIPPED] No dependencies.zip found for {githubRepo}");
                    }
                }

                return true;
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            LogMsg("error", $"Update failed for {githubRepo}: {ex.Message}");
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
}
