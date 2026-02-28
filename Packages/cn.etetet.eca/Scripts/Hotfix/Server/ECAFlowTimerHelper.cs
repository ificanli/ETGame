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

            CancelTimer(point, scene, timerId);

            ECAFlowTimerComponent timerContext = scene.AddChild<ECAFlowTimerComponent, string, long, long>(timerId, pointUnit.Id, player.Id);
            long tillTime = TimeInfo.Instance.ServerNow() + durationMs;
            long timerActionId = scene.TimerComponent.NewOnceTimer(tillTime, TimerInvokeType.ECAFlowTimer, timerContext);
            timerContext.TimerActionId = timerActionId;

            point.FlowTimers[timerId] = timerContext.Id;
            return true;
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

            ECAPointComponent point = pointUnit.GetComponent<ECAPointComponent>();
            if (point != null)
            {
                RemoveTimerMapping(point, timerContext.TimerId, timerContext.Id);
                List<FlowParam> eventParams = new()
                {
                    new FlowParam { Key = ParamTimerId, Value = timerContext.TimerId }
                };
                ECAFlowGraphHelper.TriggerEvent(point, player, ECAFlowEventType.OnTimerElapsed, eventParams);
            }

            timerContext.Dispose();
        }

        private static void CancelTimer(ECAPointComponent point, Scene scene, string timerId)
        {
            if (!point.FlowTimers.TryGetValue(timerId, out long timerEntityId))
            {
                return;
            }

            ECAFlowTimerComponent oldTimer = scene.GetChild<ECAFlowTimerComponent>(timerEntityId);
            if (oldTimer != null)
            {
                long actionId = oldTimer.TimerActionId;
                scene.TimerComponent.Remove(ref actionId);
                oldTimer.Dispose();
            }

            point.FlowTimers.Remove(timerId);
        }

        private static void RemoveTimerMapping(ECAPointComponent point, string timerId, long timerEntityId)
        {
            if (point == null || string.IsNullOrEmpty(timerId))
            {
                return;
            }

            if (point.FlowTimers.TryGetValue(timerId, out long existId) && existId == timerEntityId)
            {
                point.FlowTimers.Remove(timerId);
            }
        }
    }
}
