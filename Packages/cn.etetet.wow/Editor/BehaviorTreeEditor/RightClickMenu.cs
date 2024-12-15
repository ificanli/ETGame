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
        
        public Func<SearchTreeEntry, SearchWindowContext, bool> OnSelectEntryHandler;
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (this.entries != null)
            {
                return this.entries;
            }
            this.entries = new List<SearchTreeEntry>();
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Create Node")));
            foreach (Type type in TypeCache.GetTypesDerivedFrom<BTNode>())
            {
                if (type.IsAbstract)
                {
                    continue;
                }
                if (type.IsSubclassOf(typeof(BTNode)))
                {
                    this.entries.Add(new SearchTreeEntry(new GUIContent(type.Name)) { level = 1, userData = type});
                }
            }

            return this.entries;
        }
        
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (OnSelectEntryHandler == null)
            {
                return false;
            }
            return OnSelectEntryHandler(SearchTreeEntry, context);
        }
    }
}