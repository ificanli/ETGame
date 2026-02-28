using System.Collections.Generic;
using System.Linq;
using ET;

namespace ET.Server
{
    public static class ECAFlowGraphRunner
    {
        private const int MaxSteps = 256;
        private const string ParamState = "state";

        public static void TriggerEvent(FlowGraphData graph, ECAPointComponent point, Unit player, string eventType)
        {
            TriggerEvent(graph, point, player, eventType, null);
        }

        public static void TriggerEvent(FlowGraphData graph, ECAPointComponent point, Unit player, string eventType, List<FlowParam> eventParams)
        {
            if (graph == null || graph.Nodes == null || graph.Nodes.Count == 0)
            {
                return;
            }

            Dictionary<int, FlowNodeData> nodeMap = graph.Nodes.ToDictionary(n => n.Id, n => n);
            Dictionary<int, Dictionary<string, List<int>>> adjacency = BuildAdjacency(graph);

            Queue<int> queue = new();
            foreach (FlowNodeData node in graph.Nodes)
            {
                if (node.NodeType == ECAFlowNodeType.Event && node.NodeKey == eventType && MatchEventParams(node.Params, eventParams))
                {
                    EnqueueNext(queue, adjacency, node.Id, "Out");
                }
            }

            int steps = 0;
            while (queue.Count > 0 && steps < MaxSteps)
            {
                steps++;
                int nodeId = queue.Dequeue();
                if (!nodeMap.TryGetValue(nodeId, out FlowNodeData node))
                {
                    continue;
                }

                switch (node.NodeType)
                {
                    case ECAFlowNodeType.Condition:
                        bool result = EvaluateCondition(node, point, player);
                        EnqueueNext(queue, adjacency, nodeId, result ? "True" : "False");
                        break;
                    case ECAFlowNodeType.Action:
                        ExecuteAction(node, point, player);
                        EnqueueNext(queue, adjacency, nodeId, "Out");
                        break;
                    case ECAFlowNodeType.State:
                        EnqueueNext(queue, adjacency, nodeId, "Out");
                        break;
                    case ECAFlowNodeType.Event:
                        EnqueueNext(queue, adjacency, nodeId, "Out");
                        break;
                    default:
                        EnqueueNext(queue, adjacency, nodeId, "Out");
                        break;
                }
            }
        }

        private static Dictionary<int, Dictionary<string, List<int>>> BuildAdjacency(FlowGraphData graph)
        {
            Dictionary<int, Dictionary<string, List<int>>> result = new();
            if (graph.Connections == null)
            {
                return result;
            }

            foreach (FlowConnectionData connection in graph.Connections)
            {
                if (!result.TryGetValue(connection.FromNodeId, out var branchMap))
                {
                    branchMap = new Dictionary<string, List<int>>();
                    result[connection.FromNodeId] = branchMap;
                }

                string branch = string.IsNullOrEmpty(connection.Branch) ? "Out" : connection.Branch;
                if (!branchMap.TryGetValue(branch, out var list))
                {
                    list = new List<int>();
                    branchMap[branch] = list;
                }

                list.Add(connection.ToNodeId);
            }

            return result;
        }

        private static void EnqueueNext(Queue<int> queue, Dictionary<int, Dictionary<string, List<int>>> adjacency, int nodeId, string branch)
        {
            if (!adjacency.TryGetValue(nodeId, out var branchMap))
            {
                return;
            }

            string branchKey = string.IsNullOrEmpty(branch) ? "Out" : branch;
            if (!branchMap.TryGetValue(branchKey, out var nextList))
            {
                return;
            }

            foreach (int nextId in nextList)
            {
                queue.Enqueue(nextId);
            }
        }

        private static bool MatchEventParams(List<FlowParam> nodeParams, List<FlowParam> eventParams)
        {
            if (nodeParams == null || nodeParams.Count == 0)
            {
                return true;
            }

            if (eventParams == null || eventParams.Count == 0)
            {
                return false;
            }

            foreach (FlowParam nodeParam in nodeParams)
            {
                if (nodeParam == null)
                {
                    continue;
                }

                bool matched = false;
                foreach (FlowParam eventParam in eventParams)
                {
                    if (eventParam == null)
                    {
                        continue;
                    }

                    if (eventParam.Key == nodeParam.Key && eventParam.Value == nodeParam.Value)
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EvaluateCondition(FlowNodeData node, ECAPointComponent point, Unit player)
        {
            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<Unit> playerRef = player;
            ECAFlowConditionInvoke args = new()
            {
                Key = node.NodeKey,
                Node = node,
                Point = pointRef,
                Player = playerRef
            };

            return EventSystem.Instance.Invoke<ECAFlowConditionInvoke, bool>(args);
        }

        private static void ExecuteAction(FlowNodeData node, ECAPointComponent point, Unit player)
        {
            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<Unit> playerRef = player;
            ECAFlowActionInvoke args = new()
            {
                Key = node.NodeKey,
                Node = node,
                Point = pointRef,
                Player = playerRef
            };

            EventSystem.Instance.Invoke(args);
        }
    }
}
