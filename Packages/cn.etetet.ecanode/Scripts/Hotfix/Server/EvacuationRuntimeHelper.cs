namespace ET.Server
{
    /// <summary>
    /// 撤离点运行时客户端同步辅助。
    /// </summary>
    public static class EvacuationRuntimeHelper
    {
        public static void SendEvacuationState(ECAPointComponent point, Unit player, int state, long remainMs)
        {
            if (point == null || player == null || player.IsDisposed)
            {
                return;
            }

            M2C_ECAEvacuationState message = M2C_ECAEvacuationState.Create();
            message.PointId = point.PointId;
            message.State = state;
            message.RemainMs = remainMs;
            MapMessageHelper.NoticeClient(player, message, NoticeType.Self);
        }

        public static void SendEvacuationState(long evacuationPointId, Unit player, int state, long remainMs)
        {
            if (player == null || player.IsDisposed)
            {
                return;
            }

            Scene scene = player.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            Unit pointUnit = unitComponent?.Get(evacuationPointId);
            ECAPointComponent point = pointUnit?.GetComponent<ECAPointComponent>();
            if (point == null || point.IsDisposed)
            {
                return;
            }

            SendEvacuationState(point, player, state, remainMs);
        }
    }
}
