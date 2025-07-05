using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace ET
{
    public class RightClickMenu : ScriptableObject, ISearchWindowProvider
    {
        private List<SearchTreeEntry> entries;

        private ETTask<(SearchTreeEntry, SearchWindowContext)> tcs;
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (this.entries != null)
            {
                return this.entries;
            }
            this.entries = new List<SearchTreeEntry>();
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Nodes")));

            List<SearchTreeEntry> list = new();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<BTNode>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }
                if (type.IsSubclassOf(typeof(BTNode)))
                {
                    list.Add(new SearchTreeEntry(new GUIContent(type.Name)) { level = 1, userData = type});
                }
            }
            list.Sort((x, y)=>string.Compare(x.name, y.name, StringComparison.Ordinal));
            
            this.entries.AddRange(list);

            return this.entries;
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