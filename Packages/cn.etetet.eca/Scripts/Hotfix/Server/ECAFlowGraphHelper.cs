using System.Collections.Generic;

namespace ET.Server
{
    public static class ECAFlowGraphHelper
    {
        public static void TriggerEvent(ECAPointComponent point, Unit player, string eventType)
        {
            if (point == null || point.FlowGraph == null)
            {
                return;
            }

            ECAFlowGraphRunner.TriggerEvent(point.FlowGraph, point, player, eventType);
        }

        public static void TriggerEvent(ECAPointComponent point, Unit player, string eventType, List<FlowParam> eventParams)
        {
            if (point == null || point.FlowGraph == null)
            {
                return;
            }

            ECAFlowGraphRunner.TriggerEvent(point.FlowGraph, point, player, eventType, eventParams);
        }
    }
}
