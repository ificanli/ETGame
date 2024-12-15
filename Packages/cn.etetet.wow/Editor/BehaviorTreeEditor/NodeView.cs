using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace ET
{
    public class NodeView: Node
    {
        public IMGUIContainer imgui;
        
        public BTNode node { get; }

        private readonly List<NodeView> children = new();
        
        public NodeView(BTNode node)
        {
            this.node = node;
        }
        
        public List<NodeView> GetChildren()
        {
            return this.children;
        }
    }
}