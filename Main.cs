using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using MEC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeltaPatch;

public class Main : Plugin<Config>
{
    public bool awaitingReboot = false;
    public List<(string pluginName, string pluginStatus)> pluginUpdates = [];
    public RoundEvents RoundEvents { get; } = new();
    public static Main Instance { get; private set; }
    public override string Name => "DeltaPatch";

    public override string Description => "Made by Kenley M. with ❤";

    public override string Author => "Kenley M.";

    public override Version Version => Version.Parse("2.0.0");

    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);
    public override bool IsTransparent => true;

    public string githubRepo = "KenleyundLeon/DeltaPatch";
    public override void Enable()
    {
        Instance = this;
        try
        {
            CustomHandlersManager.RegisterEventsHandler(RoundEvents);
            if (!Config.IsEnabled) return;
            Logger.Info(startmsg);
            Timing.RunCoroutine(CheckUpdatesCoroutine(), Segment.RealtimeUpdate);
        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message);
        }
    }

    public override void Disable()
    {
        try {
            Instance = null;
            CustomHandlersManager.UnregisterEventsHandler(RoundEvents);
            Logger.Info("DeltaPatch has been Disabled.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message);
        }
    }

    private IEnumerator<float> CheckUpdatesCoroutine()
    {
        while (true)
        {
            if (awaitingReboot) Utils.LogMsg("warn", "Awaiting reboot to apply updates...");
            Update(this.GetConfigDirectory());
            yield return Timing.WaitForSeconds(Config.UpdateTimer);
        }
    }
    private void Update(DirectoryInfo confDir)
    {
        Utils.LogMsg("info", "Checking for updates . . .");
        if (Server.Host?.IsDestroyed ?? true) return;
        if (Utils.Timeout > DateTime.Now)
        {
            Utils.LogMsg("warn", "[SKIPPING] Rate limit in effect.");
            return;
        }

        var pluginInfos = Utils.GetPluginInfos(confDir);

        if (pluginInfos.Count == 0)
        {
            Logger.Warn("No compatible plugins found.");
            return;
        }

        Task.Run(async () =>
        {
        try
        {
            foreach (var item in pluginInfos)
            {
                    var results = await Utils.GetLatestReleaseDateAsync(item.GithubRepo);

                    string pluginVersionStr = Regex.Replace(item.pluginVersion?.ToString() ?? "", @"\D", "");
                    string versionTagStr = Regex.Replace(results.tag ?? "", @"\D", "");

                    // ACHTUNG TEST HIER!!
                    int pluginVersion = int.TryParse(pluginVersionStr, out var pv) ? pv : 0;
                    int versionTag = int.TryParse(versionTagStr, out var vt) ? vt : 0;

                    if (results.publishedAt > File.GetLastWriteTime(item.FilePath) || versionTag > pluginVersion)
                    {
                        var path = item.FilePath.Contains("global")
                            ? "global"
                            : (item.FilePath.Contains(Server.Port.ToString())
                                ? Server.Port.ToString()
                                : "unknown");

                        Utils.LogMsg("warn", $"[UPDATE AVAILABLE] In {path} there is a new version ({results.tag}) available for {item.GithubRepo}.");

                        bool updated = await Utils.UpdatePlugin(item.FilePath, item.GithubRepo);
                        if (updated)
                        {
                            if (!Config.RebootOnUpdate)
                            {
                                var existing = pluginUpdates.FirstOrDefault(x => x.pluginName == item.PluginName);
                                if (existing.pluginStatus != "Update Available")
                                {
                                    pluginUpdates.RemoveAll(x => x.pluginName == item.PluginName);
                                    pluginUpdates.Add((item.PluginName, "Update Available"));
                                    awaitingReboot = true;
                                    await Utils.SendDiscordWebhookMessage(item.PluginName, item.pluginVersion.ToString(), results.tag);
                                }
                                continue;
                            }

                            if (!Round.IsRoundStarted)
                            {
                                Utils.LogMsg("info", $"[RESTARTING] Restarting server to apply updates...");
                                Timing.CallDelayed(1, Server.Restart);
                            }
                        }
                        else
                        {
                            Logger.Error($"[ERROR] Couldn't update: {item.GithubRepo}");

                            pluginUpdates.RemoveAll(x => x.pluginName == item.PluginName);
                            pluginUpdates.Add((item.PluginName, "Error while Updating"));
                        }
                    }
                    else
                    {
                        Utils.LogMsg("info", $"[UP-TO-DATE] The plugin {item.GithubRepo} is up-to-date.");

                        if (!pluginUpdates.Any(x => x.pluginName == item.PluginName && x.pluginStatus == "Up-to-date"))
                        {
                            pluginUpdates.RemoveAll(x => x.pluginName == item.PluginName);
                            pluginUpdates.Add((item.PluginName, "Up-to-date"));
                        }
                    }
                }

                if (awaitingReboot && !Round.IsRoundStarted && Config.RebootOnUpdate)
                    Timing.CallDelayed(1, Server.Restart);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Update] Unhandled exception during update check: {ex.Message}");
            }
        });
    }

    private const string startmsg = "\n ____  _____ _   _____  _    ____   _  _____ ____ _   _ \r\n|  _ \\| ____| | |_   _|/ \\  |  _ \\ / \\|_   _/ ___| | | |\r\n| | | |  _| | |   | | / _ \\ | |_) / _ \\ | || |   | |_| |\r\n| |_| | |___| |___| |/ ___ \\|  __/ ___ \\| || |___|  _  |\r\n|____/|_____|_____|_/_/   \\_\\_| /_/   \\_\\_| \\____|_| |_| \nMade by Kenley M.";
}
