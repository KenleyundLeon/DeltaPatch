using CommandSystem;
using System;

namespace DeltaPatch.Commands.Remote;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public class DeltaPatchParent : ParentCommand
{
    public override string Command => "deltapatch";
    public override string[] Aliases => ["dp", "delta"];
    public override string Description => "DeltaPatch Commands.";

    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new Version());
        RegisterCommand(new Updates());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "Subcommands: version, updates";
        return false;
    }
}
