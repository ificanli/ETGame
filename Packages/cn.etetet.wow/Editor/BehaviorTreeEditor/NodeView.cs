using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET
{
    public class NodeIMGUI: SerializedScriptableObject
    {
        [NonSerialized, OdinSerialize]
        [HideReferenceObjectPicker]
        public BTNode Node;
    }
    
    public class NodeView: Node
    {
        private BTNode Node { get; }

        private readonly List<NodeView> children = new();
        
        public NodeView(BTNode node)
        {
            this.Node = node;
            base.title = node.GetType().Name;

            NodeIMGUI nodeIMGUI = ScriptableObject.CreateInstance<NodeIMGUI>();
            nodeIMGUI.Node = this.Node;
            
            Editor editor = Editor.CreateEditor(nodeIMGUI);
            
            IMGUIContainer imgui = new(() => { editor.OnInspectorGUI(); });
            
            Add(imgui);
            
            this.AddManipulator(new ResizableManipulator());
        }
        
        public List<NodeView> GetChildren()
        {
            return this.children;
        }
    }
}