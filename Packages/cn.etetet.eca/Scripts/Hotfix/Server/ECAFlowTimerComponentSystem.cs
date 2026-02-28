using ET;

namespace ET.Server
{
    [EntitySystemOf(typeof(ECAFlowTimerComponent))]
    [FriendOf(typeof(ECAFlowTimerComponent))]
    public static partial class ECAFlowTimerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAFlowTimerComponent self, string timerId, long pointUnitId, long playerUnitId)
        {
            self.TimerId = timerId;
            self.PointUnitId = pointUnitId;
            self.PlayerUnitId = playerUnitId;
        }
    }
}
