#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;

namespace ET
{
    internal static class BTInputDropdownHelper
    {
        internal sealed class OptionItem
        {
            public OptionItem(string value, Type type)
            {
                this.Value = value;
                this.Type = type;
            }

            public string Value { get; }
            public Type Type { get; }
        }

        internal sealed class OptionResult
        {
            public OptionResult(List<OptionItem> options)
            {
                this.Options = options;
            }

            public List<OptionItem> Options { get; }
        }

        internal static OptionResult GetOptionsForProperty(InspectorProperty property, BTInput inputAttribute)
        {
            if (property == null || inputAttribute == null)
            {
                return new OptionResult(new List<OptionItem>());
            }

            BTNode targetNode = ResolveTargetNode(property);
            if (targetNode == null)
            {
                return new OptionResult(new List<OptionItem>());
            }

            TreeView treeView = TreeView.ActiveTreeView;
            if (treeView == null)
            {
                return new OptionResult(new List<OptionItem>());
            }

            NodeView nodeView = treeView.Nodes.Values.FirstOrDefault(node => ReferenceEquals(node.Node, targetNode));
            if (nodeView == null)
            {
                return new OptionResult(new List<OptionItem>());
            }

            var outputs = new Dictionary<string, Type>();
            CollectOutputsAlongPath(treeView, nodeView, outputs);

            List<OptionItem> items = new();
            Type expectedType = inputAttribute.Type;

            foreach ((string name, Type type) in outputs.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                if (type != expectedType)
                {
                    continue;
                }

                items.Add(new OptionItem(name, type));
            }

            return new OptionResult(items);
        }

        internal static string GetDisplayText(string value, Type expectedType, List<OptionItem> options)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            OptionItem option = options.FirstOrDefault(item => item.Value == value);
            Type type = option?.Type ?? expectedType;
            string typeLabel = FormatTypeName(type);
            return $"{value} ({typeLabel})";
        }

        internal static string GetMissingValue(string currentValue, List<string> options)
        {
            if (string.IsNullOrEmpty(currentValue))
            {
                return string.Empty;
            }

            if (options.Contains(currentValue))
            {
                return string.Empty;
            }

            return currentValue;
        }

        private static BTNode ResolveTargetNode(InspectorProperty property)
        {
            InspectorProperty current = property;
            while (current != null)
            {
                object target = current.ValueEntry?.WeakSmartValue;
                if (target is BTNode node)
                {
                    return node;
                }

                current = current.Parent;
            }

            return null;
        }

        private static void CollectOutputsAlongPath(TreeView treeView, NodeView targetNode, Dictionary<string, Type> outputs)
        {
            if (treeView == null || targetNode == null)
            {
                return;
            }

            NodeView root = treeView.Nodes.Values.FirstOrDefault(node => node.Parent == null);
            if (root == null)
            {
                return;
            }

            CollectOutputsFromRoot(root, targetNode, outputs);
        }

        private static bool CollectOutputsFromRoot(NodeView currentNode, NodeView targetNode, Dictionary<string, Type> outputs)
        {
            if (currentNode == null)
            {
                return false;
            }

            if (currentNode.Id == targetNode.Id)
            {
                return true;
            }

            FieldInfo[] fieldInfos = currentNode.Node.GetType()
                .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var btOutputFields = fieldInfos.Where(field => field.GetCustomAttributes(typeof(BTOutput), false).Any());

            foreach (FieldInfo field in btOutputFields)
            {
                string outputValue = field.GetValue(currentNode.Node) as string;
                if (string.IsNullOrEmpty(outputValue))
                {
                    continue;
                }

                BTOutput btOutputAttr = field.GetCustomAttribute<BTOutput>();
                if (btOutputAttr != null)
                {
                    outputs[outputValue] = btOutputAttr.Type;
                }
            }

            foreach (NodeView child in currentNode.GetChildren())
            {
                if (CollectOutputsFromRoot(child, targetNode, outputs))
                {
                    return true;
                }
            }

            return false;
        }

        internal static string GetTypeLabel(Type type)
        {
            return FormatTypeName(type);
        }

        private static string FormatTypeName(Type type)
        {
            if (type == null)
            {
                return "unknown";
            }

            if (type.IsGenericType)
            {
                string genericName = type.Name.Split('`')[0];
                string args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
                return $"{genericName}<{args}>";
            }

            return type.Name;
        }
    }
}
#endif
