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
using System.Threading.Tasks;

namespace DeltaPatch;

public class Main : Plugin<Config>
{
    public RoundEvents RoundEvents { get; } = new();
    public static Main Instance { get; private set; }
    public override string Name => "DeltaPatch";

    public override string Description => "Made by Kenley M. with ❤";

    public override string Author => "Kenley M.";

    public override Version Version => Version.Parse("1.1.1");

    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);
    public override bool IsTransparent => true;

    public string githubRepo = "KenleyundLeon/DeltaPatch";
    public override void Enable()
    {
        Instance = this;
        try
        {
            Instance = this;
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
            if (awaitingReboot) Logger.Warn("Awaiting reboot to apply updates...");
            Update(this.GetConfigDirectory());
            yield return Timing.WaitForSeconds(Config.UpdateTimer);
        }
    }
    private void Update(DirectoryInfo confDir)
    {
        Utils.LogMsg("info", "Checking for updates . . .");
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
                    var path = item.FilePath.Contains("global")
                        ? "global"
                        : (item.FilePath.Contains(Server.Port.ToString())
                            ? Server.Port.ToString()
                            : "unknown");

                    Utils.LogMsg("warn", $"[UPDATE AVAILABLE] In {path} there is a new version ({results.tag}) available for {item.GithubRepo}.");

                    if (Utils.UpdatePlugin(item.FilePath, item.GithubRepo))
                    {
                        if (!Config.RebootOnUpdate) return;

                        awaitingReboot = true;

                        if (Round.IsRoundStarted) return;

                        Timing.CallDelayed(1, Server.Restart);
                    }
                    else
                    {
                        Logger.Error($"[ERROR] couldn't update: {item.GithubRepo}");
                    }
                }
                else
                {
                    Utils.LogMsg("info", $"[UP-TO-DATE] The plugin {item.GithubRepo} is up-to-date.");
                    if (awaitingReboot && !Round.IsRoundStarted) Timing.CallDelayed(1, Server.Restart);
                }
            }
        });

        scanTask.GetAwaiter().GetResult();
    }
    
    public bool awaitingReboot = false;
    private const string startmsg = "\n ____  _____ _   _____  _    ____   _  _____ ____ _   _ \r\n|  _ \\| ____| | |_   _|/ \\  |  _ \\ / \\|_   _/ ___| | | |\r\n| | | |  _| | |   | | / _ \\ | |_) / _ \\ | || |   | |_| |\r\n| |_| | |___| |___| |/ ___ \\|  __/ ___ \\| || |___|  _  |\r\n|____/|_____|_____|_/_/   \\_\\_| /_/   \\_\\_| \\____|_| |_| \nMade by Kenley M.";
}
