using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Wrappers;
using MEC;

namespace DeltaPatch;

public class RoundEvents : CustomEventsHandler
{
    public override void OnServerRoundEnding(RoundEndingEventArgs ev)
    {
        ev.IsAllowed = false;
        if (!Main.Instance.Config.RebootWhileRound && Main.Instance.awaitingReboot)
        {
            Broadcast.Singleton.RpcAddElement("Server will be restarted in 5 seconds", 5, Broadcast.BroadcastFlags.Normal);
            Timing.CallDelayed(5, () => RestartServer(ev));
        }
    }

    private void RestartServer(RoundEndingEventArgs ev)
    {
        Server.Restart();
    }
}
