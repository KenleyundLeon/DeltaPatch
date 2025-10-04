using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using MEC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DeltaPatch;

public class Main : Plugin<Config>
{
    public static Main Instance { get; private set; }
    public override string Name => "DeltaPatch";

    public override string Description => "Made by Kenley M. with ❤";

    public override string Author => "Kenley M.";

    public override Version Version => Version.Parse("1.0.0");

    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);
    public string githubRepo = "KenleyundLeon/DeltaPatch";
    public override void Enable()
    {
        Instance = this;
        try
        {
            Instance = this;
            if (!Config.IsEnabled) return;
            Logger.Info(startmsg);
            Timing.RunCoroutine(CheckUpdatesCoroutine(), Segment.RealtimeUpdate);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to get config directory: {ex.Message}");
        }
    }

    public override void Disable()
    {
        Instance = null;
        Logger.Info("DeltaPatch has been Disabled.");
    }

    private IEnumerator<float> CheckUpdatesCoroutine()
    {
        var confDir = this.GetConfigDirectory();
        while (true)
        {
            if (awaitingReboot) Logger.Warn("Awaiting reboot to apply updates...");
            Update(confDir);
            yield return Timing.WaitForSeconds(Config.UpdateTimer);
        }
    }
    private void Update(DirectoryInfo confDir)
    {
        if (Config.EnableLogging)
            Logger.Debug("Checking for updates . . .");
        var githubRepo = Utils.GetGithubReposWithFile(confDir);

        if (githubRepo.Count == 0)
        {
            Logger.Warn("No compatible plugins found.");
            return;
        }

        var scanTask = Task.Run(async () =>
        {
            foreach (var item in githubRepo)
            {
                var results = await Utils.GetLatestReleaseDateAsync(item.GithubRepo);
                if (results.publishedAt > File.GetLastWriteTime(item.FilePath))
                {
                    if (Config.EnableLogging)
                        Logger.Warn($"[UPDATE AVAILABLE] A new version ({results.tag}) is available for {item.GithubRepo}.");
                    if (Utils.UpdatePlugin(item.FilePath, item.GithubRepo))
                    {
                        if (!Config.RebootOnUpdate) return;

                        awaitingReboot = true;

                        if (Round.IsRoundStarted)
                        {
                            if (!Config.RebootWhileRound) return;
                            Broadcast.Singleton.RpcAddElement("Server will be restarted in 5 seconds", 5, Broadcast.BroadcastFlags.Normal);
                            Timing.CallDelayed(1, Server.Restart);
                        }
                        else
                        {
                            if (Config.EnableLogging)
                                Logger.Warn("[REBOOT] Rebooting server to apply updates . . .");
                            Timing.CallDelayed(1, Server.Restart);
                        }
                    }
                    else
                    {
                        Logger.Error($"[ERROR] couldn't update: {item.GithubRepo}");
                    }
                }
                else
                {
                    if (Config.EnableLogging)
                        Logger.Info($"[UP-TO-DATE] The plugin {item.GithubRepo} is up-to-date.");

                    if (awaitingReboot && !Round.IsRoundStarted)
                        Timing.CallDelayed(1, Server.Restart);
                }
            }
        });

        scanTask.GetAwaiter().GetResult();
    }
    
    private bool awaitingReboot = false;
    private const string startmsg = "\n ____  _____ _   _____  _    ____   _  _____ ____ _   _ \r\n|  _ \\| ____| | |_   _|/ \\  |  _ \\ / \\|_   _/ ___| | | |\r\n| | | |  _| | |   | | / _ \\ | |_) / _ \\ | || |   | |_| |\r\n| |_| | |___| |___| |/ ___ \\|  __/ ___ \\| || |___|  _  |\r\n|____/|_____|_____|_/_/   \\_\\_| /_/   \\_\\_| \\____|_| |_| \nMade by Kenley M.";
}
