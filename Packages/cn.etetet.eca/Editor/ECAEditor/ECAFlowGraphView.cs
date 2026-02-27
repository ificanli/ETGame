using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ET;

namespace ET.Client
{
    public class ECAFlowGraphView : GraphView
    {
        private FlowGraphAsset graphAsset;
        private int nextNodeId = 1;

        public ECAFlowGraphView(EditorWindow window)
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public void BindGraphAsset(FlowGraphAsset asset)
        {
            graphAsset = asset;
            ClearGraph();
            if (graphAsset != null)
            {
                LoadGraph();
            }
        }

        public void CreateNode(string nodeType)
        {
            ECAFlowNodeView node = new ECAFlowNodeView(nodeType, nextNodeId++);
            node.SetPosition(new Rect(Vector2.zero, node.DefaultSize));
            AddElement(node);
        }

        public override System.Collections.Generic.List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(p => p.direction != startPort.direction && p.node != startPort.node).ToList();
        }

        public void SaveGraph()
        {
            if (graphAsset == null)
            {
                return;
            }

            FlowGraphData data = new FlowGraphData();
            foreach (ECAFlowNodeView node in nodes.OfType<ECAFlowNodeView>())
            {
                data.Nodes.Add(node.ToData());
            }

            foreach (Edge edge in edges.ToList())
            {
                if (edge.output?.node is ECAFlowNodeView fromNode && edge.input?.node is ECAFlowNodeView toNode)
                {
                    data.Connections.Add(new FlowConnectionData
                    {
                        FromNodeId = fromNode.NodeId,
                        ToNodeId = toNode.NodeId,
                        Branch = edge.output.portName
                    });
                }
            }

            ValidateGraph(data);
            graphAsset.Graph = data;
            EditorUtility.SetDirty(graphAsset);
            AssetDatabase.SaveAssets();
        }

        public void LoadGraph()
        {
            if (graphAsset == null)
            {
                return;
            }

            ClearGraph();
            if (graphAsset.Graph == null)
            {
                graphAsset.Graph = new FlowGraphData();
            }

            nextNodeId = 1;
            foreach (FlowNodeData nodeData in graphAsset.Graph.Nodes)
            {
                ECAFlowNodeView node = new ECAFlowNodeView(nodeData);
                AddElement(node);
                nextNodeId = Math.Max(nextNodeId, nodeData.Id + 1);
            }

            var nodeMap = nodes.OfType<ECAFlowNodeView>().ToDictionary(n => n.NodeId, n => n);
            foreach (FlowConnectionData connection in graphAsset.Graph.Connections)
            {
                if (!nodeMap.TryGetValue(connection.FromNodeId, out ECAFlowNodeView fromNode))
                {
                    continue;
                }

                if (!nodeMap.TryGetValue(connection.ToNodeId, out ECAFlowNodeView toNode))
                {
                    continue;
                }

                Port outputPort = fromNode.GetOutputPort(connection.Branch);
                Port inputPort = toNode.InputPort;
                if (outputPort == null || inputPort == null)
                {
                    continue;
                }

                Edge edge = outputPort.ConnectTo(inputPort);
                AddElement(edge);
            }
        }

        private void ClearGraph()
        {
            DeleteElements(graphElements.Where(e => e is Node || e is Edge).ToList());
        }

        private void ValidateGraph(FlowGraphData data)
        {
            if (data == null)
            {
                return;
            }

            int eventCount = data.Nodes.Count(n => n.NodeType == ECAFlowNodeType.Event);
            if (eventCount == 0)
            {
                Debug.LogWarning($"[ECAFlowGraph] Graph '{graphAsset?.name}' has no Event nodes.");
            }

            var outgoing = data.Connections
                .GroupBy(c => c.FromNodeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (FlowNodeData node in data.Nodes)
            {
                if (string.IsNullOrWhiteSpace(node.NodeKey))
                {
                    Debug.LogWarning($"[ECAFlowGraph] Node {node.Id}({node.NodeType}) has empty Key.");
                }

                if (!outgoing.TryGetValue(node.Id, out var list))
                {
                    if (node.NodeType != ECAFlowNodeType.Action)
                    {
                        Debug.LogWarning($"[ECAFlowGraph] Node {node.Id}({node.NodeType}) has no outgoing connection.");
                    }
                    continue;
                }

                if (node.NodeType == ECAFlowNodeType.Condition)
                {
                    bool hasTrue = list.Any(c => string.Equals(c.Branch, "True", StringComparison.OrdinalIgnoreCase));
                    bool hasFalse = list.Any(c => string.Equals(c.Branch, "False", StringComparison.OrdinalIgnoreCase));
                    if (!hasTrue || !hasFalse)
                    {
                        Debug.LogWarning($"[ECAFlowGraph] Condition node {node.Id} missing branch: True={hasTrue}, False={hasFalse}");
                    }
                }
            }
        }
    }
}
