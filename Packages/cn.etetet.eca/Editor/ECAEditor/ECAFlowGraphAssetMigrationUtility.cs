using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ET.Client
{
    public static class ECAFlowGraphAssetMigrationUtility
    {
        private static readonly Regex LegacyNodeIdRegex = new(@"^\s*-\s+(?:Id|_id):\s*(\d+)\s*$", RegexOptions.Compiled);

        public static bool TryPrepareReferencedFlowGraphs(IEnumerable<ECAPointMarker> markers, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (markers == null)
            {
                return true;
            }

            bool anyChanged = false;
            List<string> errors = new();
            HashSet<FlowGraphAsset> visited = new();

            foreach (ECAPointMarker marker in markers)
            {
                FlowGraphAsset graphAsset = marker?.FlowGraph;
                if (graphAsset == null || !visited.Add(graphAsset))
                {
                    continue;
                }

                if (!TryPrepareFlowGraph(graphAsset, out bool changed, out string graphError))
                {
                    errors.Add(graphError);
                    continue;
                }

                anyChanged |= changed;
            }

            if (anyChanged)
            {
                AssetDatabase.SaveAssets();
            }

            if (errors.Count == 0)
            {
                return true;
            }

            errorMessage = string.Join("\n", errors);
            return false;
        }

        public static bool TryPrepareFlowGraph(FlowGraphAsset graphAsset, out bool changed, out string errorMessage)
        {
            changed = false;
            errorMessage = string.Empty;

            if (graphAsset?.Graph?.Nodes == null || graphAsset.Graph.Nodes.Count == 0)
            {
                return true;
            }

            changed = TryMigrateLegacyNodeIds(graphAsset);
            changed |= TrySanitizePlaceholderParams(graphAsset);

            if (TryValidateGraph(graphAsset, out errorMessage))
            {
                return true;
            }

            return false;
        }

        public static int ResolveNodeId(FlowNodeData node)
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

        private static bool TryMigrateLegacyNodeIds(FlowGraphAsset graphAsset)
        {
            if (graphAsset?.Graph?.Nodes == null || graphAsset.Graph.Nodes.Count == 0)
            {
                return false;
            }

            if (graphAsset.Graph.Nodes.All(node => ResolveNodeId(node) > 0))
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (string.IsNullOrWhiteSpace(assetPath) || !File.Exists(assetPath))
            {
                Debug.LogError($"[ECAFlowGraph] Graph '{graphAsset?.name}' asset path invalid, cannot migrate legacy node ids.");
                return false;
            }

            List<int> legacyNodeIds = ReadLegacyNodeIdsFromAsset(assetPath);
            if (legacyNodeIds.Count != graphAsset.Graph.Nodes.Count)
            {
                Debug.LogError($"[ECAFlowGraph] Graph '{graphAsset?.name}' legacy node id count mismatch: asset={legacyNodeIds.Count}, graph={graphAsset.Graph.Nodes.Count}");
                return false;
            }

            bool changed = false;
            for (int i = 0; i < graphAsset.Graph.Nodes.Count; ++i)
            {
                FlowNodeData node = graphAsset.Graph.Nodes[i];
                if (ResolveNodeId(node) > 0)
                {
                    continue;
                }

                int legacyNodeId = legacyNodeIds[i];
                if (legacyNodeId <= 0)
                {
                    continue;
                }

                node.NodeId = legacyNodeId;
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            Debug.Log($"[ECAFlowGraph] Graph '{graphAsset.name}' migrated legacy node ids before use.");
            EditorUtility.SetDirty(graphAsset);
            return true;
        }

        private static bool TryValidateGraph(FlowGraphAsset graphAsset, out string errorMessage)
        {
            errorMessage = string.Empty;
            FlowGraphData graph = graphAsset?.Graph;
            if (graph?.Nodes == null || graph.Nodes.Count == 0)
            {
                return true;
            }

            StringBuilder builder = new();
            Dictionary<int, FlowNodeData> nodeMap = new();

            foreach (FlowNodeData node in graph.Nodes)
            {
                int nodeId = ResolveNodeId(node);
                if (nodeId <= 0)
                {
                    builder.AppendLine($"图 '{graphAsset.name}' 存在无效节点ID: title={node?.Title}, key={node?.NodeKey}");
                    continue;
                }

                if (!nodeMap.TryAdd(nodeId, node))
                {
                    builder.AppendLine($"图 '{graphAsset.name}' 存在重复节点ID: nodeId={nodeId}, key={node?.NodeKey}");
                }
            }

            if (graph.Connections != null)
            {
                foreach (FlowConnectionData connection in graph.Connections)
                {
                    if (!nodeMap.ContainsKey(connection.FromNodeId))
                    {
                        builder.AppendLine($"图 '{graphAsset.name}' 连线起点不存在: from={connection.FromNodeId}, to={connection.ToNodeId}, branch={connection.Branch}");
                    }

                    if (!nodeMap.ContainsKey(connection.ToNodeId))
                    {
                        builder.AppendLine($"图 '{graphAsset.name}' 连线终点不存在: from={connection.FromNodeId}, to={connection.ToNodeId}, branch={connection.Branch}");
                    }
                }
            }

            if (builder.Length == 0)
            {
                return true;
            }

            errorMessage = builder.ToString().TrimEnd();
            return false;
        }

        private static bool TrySanitizePlaceholderParams(FlowGraphAsset graphAsset)
        {
            FlowGraphData graph = graphAsset?.Graph;
            if (graph?.Nodes == null || graph.Nodes.Count == 0)
            {
                return false;
            }

            bool changed = false;
            foreach (FlowNodeData node in graph.Nodes)
            {
                if (node?.Params == null || node.Params.Count == 0)
                {
                    continue;
                }

                int removedCount = node.Params.RemoveAll(param => !FlowParamHelper.IsMeaningfulParam(param));
                if (removedCount <= 0)
                {
                    continue;
                }

                changed = true;
                Debug.Log($"[ECAFlowGraph] Graph '{graphAsset.name}' removed {removedCount} placeholder params from node {ResolveNodeId(node)}({node.NodeKey}).");
            }

            if (!changed)
            {
                return false;
            }

            EditorUtility.SetDirty(graphAsset);
            return true;
        }

        private static List<int> ReadLegacyNodeIdsFromAsset(string assetPath)
        {
            List<int> nodeIds = new();
            foreach (string line in File.ReadLines(assetPath))
            {
                Match match = LegacyNodeIdRegex.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                if (int.TryParse(match.Groups[1].Value, out int nodeId))
                {
                    nodeIds.Add(nodeId);
                }
            }

            return nodeIds;
        }
    }
}
