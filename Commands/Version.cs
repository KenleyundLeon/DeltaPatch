using CommandSystem;
using System;

namespace DeltaPatch.Commands.Remote;

[CommandHandler(typeof(DeltaPatchParent))]
public class Version : ICommand
{
    public string Command => "version";

    public string[] Aliases => ["v", "ver"];

    public string Description => "Shows the current Delta-Patch Version.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = $"DeltaPatch Version: {Main.Instance.Version}";
        return true;
    }
}
