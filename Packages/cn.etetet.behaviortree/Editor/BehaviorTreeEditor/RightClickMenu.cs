using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace ET
{
    public class RightClickMenu : ScriptableObject, ISearchWindowProvider
    {
        private List<SearchTreeEntry> entries;

        private ETTask<(SearchTreeEntry, SearchWindowContext)> tcs;

        private sealed class GroupNode
        {
            public string Name;
            public SortedDictionary<string, GroupNode> Children = new(StringComparer.Ordinal);
            public List<Type> LeafTypes = new();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (this.entries != null)
            {
                return this.entries;
            }

            this.entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Nodes"), 0),
            };

            GroupNode root = new() { Name = "Nodes" };
            foreach (Type type in TypeCache.GetTypesDerivedFrom<BTNode>())
            {
                if (type == null || type.IsAbstract)
                {
                    continue;
                }

                if (!type.IsSubclassOf(typeof(BTNode)))
                {
                    continue;
                }

                if (typeof(BTRoot).IsAssignableFrom(type))
                {
                    continue;
                }

                if (type.ContainsGenericParameters)
                {
                    continue;
                }

                List<string> groupPath = GetGroupPath(type);
                GroupNode current = root;
                for (int i = 0; i < groupPath.Count; i++)
                {
                    string groupName = groupPath[i];
                    if (!current.Children.TryGetValue(groupName, out GroupNode child))
                    {
                        child = new GroupNode { Name = groupName };
                        current.Children.Add(groupName, child);
                    }

                    current = child;
                }

                current.LeafTypes.Add(type);
            }

            EmitEntries(root, level: 1);
            return this.entries;
        }

        private static List<string> GetGroupPath(Type type)
        {
            List<string> reversed = new();
            Type current = type.BaseType;
            while (current != null && typeof(BTNode).IsAssignableFrom(current) && current != typeof(BTNode))
            {
                reversed.Add(current.Name);
                current = current.BaseType;
            }

            reversed.Reverse();
            return reversed;
        }

        private void EmitEntries(GroupNode node, int level)
        {
            foreach ((string _, GroupNode child) in node.Children)
            {
                this.entries.Add(new SearchTreeGroupEntry(new GUIContent(child.Name), level));
                EmitEntries(child, level + 1);
            }

            if (node.LeafTypes.Count <= 0)
            {
                return;
            }

            node.LeafTypes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            foreach (Type leafType in node.LeafTypes)
            {
                this.entries.Add(new SearchTreeEntry(new GUIContent(leafType.Name)) { level = level, userData = leafType });
            }
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (this.tcs == null)
            {
                return false;
            }

            this.tcs.SetResult((searchTreeEntry, context));
            return true;
        }

        public async ETTask<(SearchTreeEntry, SearchWindowContext)> WaitSelect(Vector2 pos)
        {
            SearchWindow.Open(new SearchWindowContext(pos), this);

            this.tcs = ETTask<(SearchTreeEntry, SearchWindowContext)>.Create();
            return await this.tcs;
        }
    }
}
