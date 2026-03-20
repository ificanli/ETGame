using System;
using System.Collections.Generic;
using ET;

namespace ET.Server
{
    public static class ECAFlowGraphRunner
    {
        private const int MaxSteps = 256;

        public static void TriggerEvent(FlowGraphData graph, ECAPointComponent point, Unit player, string eventType)
        {
            TriggerEventAsync(graph, point, player, eventType).Coroutine();
        }

        public static void TriggerEvent(FlowGraphData graph, ECAPointComponent point, Unit player, string eventType, List<FlowParam> eventParams)
        {
            TriggerEventAsync(graph, point, player, eventType, eventParams).Coroutine();
        }

        public static ETTask TriggerEventAsync(FlowGraphData graph, ECAPointComponent point, Unit player, string eventType)
        {
            return TriggerEventAsync(graph, point, player, eventType, null);
        }

        public static async ETTask TriggerEventAsync(FlowGraphData graph, ECAPointComponent point, Unit player, string eventType, List<FlowParam> eventParams)
        {
            if (graph == null || graph.Nodes == null || graph.Nodes.Count == 0)
            {
                return;
            }

            EnsureRuntimeCache(graph);
            Dictionary<int, FlowNodeData> nodeMap = graph.RuntimeNodeMap;
            Dictionary<int, Dictionary<string, List<int>>> adjacency = graph.RuntimeAdjacency;

            Queue<int> queue = new();
            foreach (FlowNodeData node in graph.Nodes)
            {
                int nodeId = ResolveNodeId(node);
                if (node.NodeType == ECAFlowNodeType.Event &&
                    node.NodeKey == eventType &&
                    FlowParamHelper.MatchAllParams(node.Params, eventParams))
                {
                    EnqueueNext(queue, adjacency, nodeId, "Out");
                }
            }

            EntityRef<ECAPointComponent> pointRef = point;
            EntityRef<Unit> playerRef = player;
            int steps = 0;
            while (queue.Count > 0 && steps < MaxSteps)
            {
                steps++;
                int nodeId = queue.Dequeue();
                point = pointRef;
                player = playerRef;
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
                        await ExecuteActionAsync(node, point, player);
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

            if (queue.Count > 0)
            {
                Log.Warning($"[ECAFlow] graph execution exceeded max steps: event={eventType}, remaining={queue.Count}, max={MaxSteps}");
            }
        }

        private static void EnsureRuntimeCache(FlowGraphData graph)
        {
            bool repairedLegacyNodeIds = NormalizeLegacyNodeIds(graph);
            int nodeCount = graph?.Nodes?.Count ?? 0;
            int connectionCount = graph?.Connections?.Count ?? 0;
            if (graph == null)
            {
                return;
            }

            if (repairedLegacyNodeIds)
            {
                graph.RuntimeNodeMap = null;
                graph.RuntimeAdjacency = null;
            }

            if (graph.RuntimeNodeMap != null &&
                graph.RuntimeAdjacency != null &&
                graph.RuntimeNodeCount == nodeCount &&
                graph.RuntimeConnectionCount == connectionCount)
            {
                return;
            }

            Dictionary<int, FlowNodeData> nodeMap = new(nodeCount);
            if (graph.Nodes != null)
            {
                foreach (FlowNodeData node in graph.Nodes)
                {
                    if (node == null)
                    {
                        continue;
                    }

                    int nodeId = ResolveNodeId(node);
                    if (nodeMap.TryGetValue(nodeId, out FlowNodeData existNode))
                    {
                        Log.Warning($"[ECAFlow] duplicate node id detected: nodeId={nodeId}, existKey={existNode.NodeKey}, incomingKey={node.NodeKey}");
                    }

                    nodeMap[nodeId] = node;
                }
            }

            graph.RuntimeNodeMap = nodeMap;
            graph.RuntimeAdjacency = BuildAdjacency(graph);
            graph.RuntimeNodeCount = nodeCount;
            graph.RuntimeConnectionCount = connectionCount;
        }

        private static bool NormalizeLegacyNodeIds(FlowGraphData graph)
        {
            if (graph?.Nodes == null || graph.Nodes.Count == 0)
            {
                return false;
            }

            int repairedCount = 0;
            foreach (FlowNodeData node in graph.Nodes)
            {
                if (node == null || node.NodeId != 0 || node.LegacyId != 0)
                {
                    continue;
                }

                if (!TryParseNodeIdFromTitle(node.Title, out int parsedNodeId))
                {
                    continue;
                }

                node.NodeId = parsedNodeId;
                repairedCount++;
            }

            if (repairedCount == 0)
            {
                return false;
            }

            Log.Warning($"[ECAFlow] repaired zero node ids from title fallback: repaired={repairedCount}");
            return true;
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

        private static int ResolveNodeId(FlowNodeData node)
        {
            if (node == null)
            {
                return 0;
            }

            if (node.NodeId != 0)
            {
                return node.NodeId;
            }

            return node.LegacyId;
        }

        private static bool TryParseNodeIdFromTitle(string title, out int nodeId)
        {
            nodeId = 0;
            if (string.IsNullOrWhiteSpace(title))
            {
                return false;
            }

            int end = title.Length - 1;
            while (end >= 0 && char.IsWhiteSpace(title[end]))
            {
                end--;
            }

            if (end < 0 || !char.IsDigit(title[end]))
            {
                return false;
            }

            int start = end;
            while (start >= 0 && char.IsDigit(title[start]))
            {
                start--;
            }

            string rawNodeId = title.Substring(start + 1, end - start);
            return int.TryParse(rawNodeId, out nodeId) && nodeId > 0;
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

        private static async ETTask ExecuteActionAsync(FlowNodeData node, ECAPointComponent point, Unit player)
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

            await EventSystem.Instance.Invoke<ECAFlowActionInvoke, ETTask>(args);
        }
    }
}
