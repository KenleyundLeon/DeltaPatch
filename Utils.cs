using LabApi.Features.Console;
using System;
using System.Collections.Generic;
using System.IO;
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
        if (!string.IsNullOrEmpty(Token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", Token);
        return client;
    }

    public static List<(string GithubRepo, string FilePath)> GetGithubReposWithFile(DirectoryInfo confDir)
    {
        string pluginPath = Path.Combine(confDir.Parent!.Parent!.Parent!.FullName, "plugins", confDir.Parent.Name);

        return Directory.GetFiles(pluginPath, "*.dll")
            .SelectMany(file =>
            {
                var asm = Assembly.LoadFrom(file);
                return asm.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.BaseType?.Name.StartsWith("Plugin") == true)
                    .Select(t => (Type: t, File: file));
            })
            .Select(info =>
            {
                var instance = info.Type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
                var repo = info.Type.GetField("githubRepo", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance)?.ToString();
                return string.IsNullOrEmpty(repo) ? default : (repo, info.File);
            })
            .Where(r => r.repo != null)
            .Cast<(string GithubRepo, string FilePath)>()
            .ToList();
    }

    private static async Task<(string? tag, string? assetId, DateTime? publishedAt)> GetLatestReleaseAsync(string githubRepo)
    {
        try
        {
            using var http = CreateHttpClient();
            if (Main.Instance.Config.EnableLogging)
                Logger.Info($"[FETCHING] latest release for {githubRepo} . . .");
            using var resp = await http.GetAsync($"https://api.github.com/repos/{githubRepo}/releases/latest");
            if (!resp.IsSuccessStatusCode)
            {
                if (Main.Instance.Config.EnableLogging)
                    Logger.Info($"[SKIPPING] repo '{githubRepo}' – Status: {resp.StatusCode}");
                return default;
            }

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            var root = doc.RootElement;

            var tag = root.GetPropertyOrDefault("tag_name")?.GetString();
            DateTime? publishedAt = DateTime.TryParse(root.GetPropertyOrDefault("published_at")?.GetString(), out var date) ? date : (DateTime?)null;
            var assetId = root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array
                ? assets.EnumerateArray()
                    .FirstOrDefault(a => a.GetPropertyOrDefault("name")?.GetString()?.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) == true)
                    .GetPropertyOrDefault("id")?.GetRawText()
                : null;

            return (tag, assetId, publishedAt);
        }
        catch (Exception ex)
        {
            Logger.Error($"Request failed for {githubRepo}: {ex.Message}");
            return default;
        }
    }

    public static async Task<(string githubRepo, string? tag, DateTime? publishedAt)> GetLatestReleaseDateAsync(string githubRepo)
    {
        var (tag, _, publishedAt) = await GetLatestReleaseAsync(githubRepo);
        return (githubRepo, tag, publishedAt);
    }

    public static bool UpdatePlugin(string filePath, string githubRepo)
    {
        try
        {
            return Task.Run(async () =>
            {
                var (tag, assetId, _) = await GetLatestReleaseAsync(githubRepo);
                if (string.IsNullOrEmpty(assetId))
                {
                    if (Main.Instance.Config.EnableLogging)
                        Logger.Warn($"No DLL asset found for {githubRepo}");
                    return false;
                }

                var targetPath = Path.Combine(Path.GetDirectoryName(filePath)!, Path.GetFileName(filePath));
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                using var http = CreateHttpClient(TimeSpan.FromMinutes(2));
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                using var resp = await http.GetAsync($"https://api.github.com/repos/{githubRepo}/releases/assets/{assetId}", HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();

                using var stream = await resp.Content.ReadAsStreamAsync();
                using var file = File.Create(targetPath);
                await stream.CopyToAsync(file);
                if (Main.Instance.Config.EnableLogging)
                    Logger.Info($"[UPDATED] {githubRepo} to version {tag}");

                return true;
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Error($"Update failed: {ex.Message}");
            return false;
        }
    }

    private static JsonElement? GetPropertyOrDefault(this JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop) ? prop : null;
}