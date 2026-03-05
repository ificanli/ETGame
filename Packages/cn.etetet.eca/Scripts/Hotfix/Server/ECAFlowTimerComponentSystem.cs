using ET;

namespace ET.Server
{
    [EntitySystemOf(typeof(ECAFlowTimerComponent))]
    [FriendOf(typeof(ECAFlowTimerComponent))]
    public static partial class ECAFlowTimerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAFlowTimerComponent self, string timerId, string timerKey, long pointUnitId)
        {
            self.TimerId = timerId;
            self.TimerKey = timerKey;
            self.PointUnitId = pointUnitId;
        }
    }
}
