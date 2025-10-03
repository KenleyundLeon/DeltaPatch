using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DeltaPatch;

public class Main : Plugin
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
        Logger.Info("DeltaPatch has been Enabled.");
        try
        {
            // LoadHandlers("LAPI.Modules", "[OK] Registered module");
            // LoadHandlers("LAPI.Sounds", "[OK] Registered sound");
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
        try
        {
            // LoadHandlers("LAPI.Modules", "[OK] Registered module");
            // LoadHandlers("LAPI.Sounds", "[OK] Registered sound");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to get config directory: {ex.Message}");
        }
    }

    public override void LoadConfigs()
    {
        base.LoadConfigs();
        DirectoryInfo confDir = this.GetConfigDirectory();
        string pluginPath = Path.Combine(confDir.Parent.Parent.Parent.FullName, "plugins", confDir.Parent.Name);

        foreach (var item in Directory.GetFiles(pluginPath, "*.dll"))
        {
            var fileInfo = new FileInfo(item);
            Logger.Info(item);
            Logger.Info(fileInfo.Name);

            var asm = Assembly.LoadFrom(item);
            var pluginTypes = asm.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.BaseType != null &&
                            (t.BaseType.Name.StartsWith("Plugin") || t.BaseType.FullName.Contains("Exiled.API.Features.Plugin")))
                .ToList();

            foreach (var type in pluginTypes)
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                var instance = ctor?.Invoke(null);

                if (instance != null)
                {
                    string pluginName = type.GetProperty("Name")?.GetValue(instance)?.ToString();
                    Logger.Info($"Plugin Name: {pluginName}");
                    string githubRepo = type.GetProperty("githubRepo")?.GetValue(instance)?.ToString();
                    Logger.Info(githubRepo);
                }
            }
        }
    }
}
