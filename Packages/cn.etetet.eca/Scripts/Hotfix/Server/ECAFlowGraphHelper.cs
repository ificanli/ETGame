using System.Collections.Generic;

namespace ET.Server
{
    public static class ECAFlowGraphHelper
    {
        public static void TriggerEvent(ECAPointComponent point, Unit player, string eventType)
        {
            TriggerEventAsync(point, player, eventType).Coroutine();
        }

        public static void TriggerEvent(ECAPointComponent point, Unit player, string eventType, List<FlowParam> eventParams)
        {
            TriggerEventAsync(point, player, eventType, eventParams).Coroutine();
        }

        public static ETTask TriggerEventAsync(ECAPointComponent point, Unit player, string eventType)
        {
            return TriggerEventAsync(point, player, eventType, null);
        }

        public static ETTask TriggerEventAsync(ECAPointComponent point, Unit player, string eventType, List<FlowParam> eventParams)
        {
            if (point == null || point.FlowGraph == null)
            {
                return ETTask.CompletedTask;
            }

            return ECAFlowGraphRunner.TriggerEventAsync(point.FlowGraph, point, player, eventType, eventParams);
        }
    }
}
