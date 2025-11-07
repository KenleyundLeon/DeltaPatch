using System.ComponentModel;

namespace DeltaPatch;

public class Config
{
    [Description("Enables or disables the plugin.")]
    public bool IsEnabled { get; set; } = true;

    [Description("Enables or disables logging to the console.")]
    public bool EnableLogging { get; set; } = false;

    [Description("Specifies which framework the plugin should update: 0 = LabAPI, 1 = Exiled (Not implemented), 2 = Both (Half implemented).")]
    public int UpdateFramework { get; set; } = 0;

    [Description("If true, the plugin will also consider pre-release versions when checking for updates.")]
    public bool AllowPrerelease { get; set; } = false;

    [Description("Specifies the interval (in seconds) at which the plugin checks for updates.")]
    public int UpdateTimer { get; set; } = 300;

    [Description("If some repositories are private, you can enter a GitHub API key (PAT token) here to access them. Otherwise, leave it empty.")]
    public string GithubApiKey { get; set; } = null;

    [Description("If true, the server will automatically reboot after applying updates. If false, you will need to manually restart the server to apply updates.")]
    public bool RebootOnUpdate { get; set; } = true;

    [Description("If true, the server will reboot while a round is ongoing. If false, the server will wait until the round ends to reboot. (Gets ignored if RebootOnUpdate is false.)")]
    public bool RebootWhileRound { get; set; } = false;
    [Description("If true, the plugin will send a notification to a Discord webhook when an update can be applied. Make sure to set the webhook URL in DiscordWebhook.")]
    public bool DiscordWebhookNotification { get; set; } = false;
    [Description("The Discord webhook URL where update notifications will be sent.")]
    public string DiscordWebhook { get; set; } = null;
}