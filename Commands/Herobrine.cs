using CommandSystem;
using System;

namespace DeltaPatch.Commands;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public class Herobrine : ICommand
{
    public string Command => "herobrine";

    public string[] Aliases => ["hb"];

    public string Description => "Random Herobrine meme lol.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "Herobrine is watching you...";
        return true;
    }
}
