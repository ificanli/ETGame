using System.Collections.Generic;
using ET;

namespace ET.Server
{
    public static class ECAFlowTimerHelper
    {
        private const string ParamTimerId = "timer_id";

        public static bool StartTimer(ECAPointComponent point, Unit player, string timerId, long durationMs)
        {
            if (point == null || player == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(timerId) || durationMs <= 0)
            {
                return false;
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null)
            {
                return false;
            }

            Scene scene = pointUnit.Scene();
            if (scene == null || scene.TimerComponent == null)
            {
                return false;
            }

            string timerKey = BuildTimerKey(timerId, player.Id);
            CancelTimer(point, scene, timerKey);

            ECAFlowTimerComponent timerContext = scene.AddChild<ECAFlowTimerComponent, string, string, long>(
                timerId,
                timerKey,
                pointUnit.Id);
            timerContext.PlayerUnitId = player.Id;
            long tillTime = TimeInfo.Instance.ServerNow() + durationMs;
            long timerActionId = scene.TimerComponent.NewOnceTimer(tillTime, TimerInvokeType.ECAFlowTimer, timerContext);
            timerContext.TimerActionId = timerActionId;

            point.FlowTimers[timerKey] = timerContext.Id;
            return true;
        }

        public static bool CancelTimer(ECAPointComponent point, Unit player, string timerId)
        {
            if (point == null || player == null || string.IsNullOrWhiteSpace(timerId))
            {
                return false;
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null)
            {
                return false;
            }

            Scene scene = pointUnit.Scene();
            if (scene == null || scene.TimerComponent == null)
            {
                return false;
            }

            string timerKey = BuildTimerKey(timerId, player.Id);
            return CancelTimer(point, scene, timerKey);
        }

        public static void CancelAllTimers(ECAPointComponent point)
        {
            if (point == null || point.FlowTimers.Count == 0)
            {
                return;
            }

            Scene scene = point.Scene();
            if (scene == null || scene.TimerComponent == null)
            {
                point.FlowTimers.Clear();
                return;
            }

            List<string> timerKeys = new(point.FlowTimers.Keys);
            foreach (string timerKey in timerKeys)
            {
                CancelTimer(point, scene, timerKey);
            }
        }

        public static void HandleTimerElapsed(ECAFlowTimerComponent timerContext)
        {
            if (timerContext == null)
            {
                return;
            }

            Scene scene = timerContext.Scene();
            if (scene == null)
            {
                timerContext.Dispose();
                return;
            }

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                timerContext.Dispose();
                return;
            }

            Unit pointUnit = unitComponent.Get(timerContext.PointUnitId);
            Unit player = unitComponent.Get(timerContext.PlayerUnitId);
            if (pointUnit == null)
            {
                timerContext.Dispose();
                return;
            }

            // 触发玩家已离开/销毁时，不再执行流程图动作，避免无效玩家触发容器开启。
            if (player == null || player.IsDisposed)
            {
                ECAPointComponent disposedPoint = pointUnit.GetComponent<ECAPointComponent>();
                RemoveTimerMapping(disposedPoint, timerContext.TimerKey, timerContext.Id);
                timerContext.Dispose();
                return;
            }

            ECAPointComponent point = pointUnit.GetComponent<ECAPointComponent>();
            if (point != null)
            {
                RemoveTimerMapping(point, timerContext.TimerKey, timerContext.Id);
                List<FlowParam> eventParams = new()
                {
                    new FlowParam { Key = ParamTimerId, Value = timerContext.TimerId }
                };
                ECAFlowGraphHelper.TriggerEvent(point, player, ECAFlowEventType.OnTimerElapsed, eventParams);
            }

            timerContext.Dispose();
        }

        private static bool CancelTimer(ECAPointComponent point, Scene scene, string timerKey)
        {
            if (!point.FlowTimers.TryGetValue(timerKey, out long timerEntityId))
            {
                return false;
            }

            ECAFlowTimerComponent oldTimer = scene.GetChild<ECAFlowTimerComponent>(timerEntityId);
            if (oldTimer != null)
            {
                long actionId = oldTimer.TimerActionId;
                scene.TimerComponent.Remove(ref actionId);
                oldTimer.Dispose();
            }

            point.FlowTimers.Remove(timerKey);
            return true;
        }

        private static void RemoveTimerMapping(ECAPointComponent point, string timerKey, long timerEntityId)
        {
            if (point == null || string.IsNullOrEmpty(timerKey))
            {
                return;
            }

            if (point.FlowTimers.TryGetValue(timerKey, out long existId) && existId == timerEntityId)
            {
                point.FlowTimers.Remove(timerKey);
            }
        }

        private static string BuildTimerKey(string timerId, long playerId)
        {
            return $"{timerId}:{playerId}";
        }
    }
}
