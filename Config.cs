using System.ComponentModel;

namespace DeltaPatch;

public class Config
{
    [Description("Enables/Disables the Plugin.")]
    public bool IsEnabled { get; set; } = true;
    [Description("Enables/Disables logging to the console.")]
    public bool EnableLogging { get; set; } = true;

    [Description("Specifies which framework the plugin should update: 0 = LabApi, 1 = Exiled (Not implemented), 2 = Both (Not implemented)")]
    public int UpdateFramework { get; set; } = 0;
    [Description("If some repository's are private, you can enter a GitHub API key (PAT-token) here to access them. Otherwise, leave it empty.")]
    public string GithubApiKey { get; set; } = null;
    [Description("If true, the server will automatically reboot after applying updates. If false, you will need to manually restart the server to apply updates.")]
    public bool RebootOnUpdate { get; set; } = true;
    [Description("If true, the server will reboot while a round is ongoing. If false, the server will wait until the round ends to reboot.")]
    public bool RebootWhileRound { get; set; } = false;
    [Description("Specifies the interval (in seconds) at which the plugin checks for updates.")]
    public int UpdateTimer { get; set; } = 5;
}
