using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace ET
{
    public class RightClickMenu : ScriptableObject, ISearchWindowProvider
    {
        public delegate bool SelectEntryDelegate(SearchTreeEntry searchTreeEntry
            , SearchWindowContext context);

        public SelectEntryDelegate OnSelectEntryHandler;
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>();
            entries.Add(new SearchTreeGroupEntry(new GUIContent("Create Node")));
            entries = AddNodeType<BTNode>();
            return entries;
        }
        
        /// <summary>
        /// 通过反射获取对应的菜单数据
        /// </summary>
        private List<SearchTreeEntry> AddNodeType<T>()
        {
            List<SearchTreeEntry> list = new();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(T)))
                    {
                        list.Add(new SearchTreeEntry(new GUIContent(type.Name)) { level = 1, userData = type});
                    }
                }
            }

            return list;
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