using CommandSystem;
using System;
using System.Linq;

namespace DeltaPatch.Commands.Remote;

[CommandHandler(typeof(DeltaPatchParent))]
public class Updates : ICommand
{
    public string Command => "updates";

    public string[] Aliases => ["infos"];

    public string Description => "Shows the Update Informations.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string result = string.Join(Environment.NewLine, Main.Instance.pluginUpdates.Select(x => $"{x.pluginName} => {x.pluginStatus}"));
        if (!result.IsEmpty())
        {
            response = result;
            return true;
        }
        else
        {
            response = "There are no updates available.";
            return false;
        }
    }
}
