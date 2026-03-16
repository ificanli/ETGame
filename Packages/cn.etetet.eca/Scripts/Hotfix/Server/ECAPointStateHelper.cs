namespace ET.Server
{
    /// <summary>
    /// 统一处理 ECA 点位状态写入和事件发布。
    /// </summary>
    public static class ECAPointStateHelper
    {
        public static bool SetState(ECAPointComponent point, int state)
        {
            if (point == null || point.IsDisposed)
            {
                return false;
            }

            int previousState = point.CurrentState;
            point.CurrentState = state;

            if (previousState == state)
            {
                Log.Info($"[ECAServer][PointState] point={point.PointId}, state unchanged={state}");
                return false;
            }

            Log.Info($"[ECAServer][PointState] point={point.PointId}, prev={previousState}, next={state}, type={point.PointType}");

            Scene scene = point.Scene();
            if (scene != null && !scene.IsDisposed)
            {
                EventSystem.Instance.Publish(scene, new ECAPointStateChangedEvent
                {
                    PointId = point.PointId,
                    PreviousState = previousState,
                    CurrentState = state
                });
            }

            return true;
        }
    }
}
