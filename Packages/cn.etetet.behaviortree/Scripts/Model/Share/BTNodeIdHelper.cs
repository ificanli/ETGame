using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 运行时代码构造行为树时，兜底补齐节点 Id，避免调试路径记录因 Id 为 0 或重复而异常。
    /// </summary>
    public static class BTNodeIdHelper
    {
        public static void EnsureIds(BTRoot root)
        {
            if (root == null)
            {
                return;
            }

            HashSet<int> usedIds = new();
            int maxId = 0;
            EnsureNodeIds(root, usedIds, ref maxId);
        }

        private static void EnsureNodeIds(BTNode node, HashSet<int> usedIds, ref int maxId)
        {
            if (node == null)
            {
                return;
            }

            node.Children ??= new List<BTNode>();

            int nodeId = node.Id;
            if (nodeId > 0 && usedIds.Add(nodeId))
            {
                if (nodeId > maxId)
                {
                    maxId = nodeId;
                }
            }
            else
            {
                do
                {
                    ++maxId;
                }
                while (maxId <= 0 || usedIds.Contains(maxId));

                node.Id = maxId;
                usedIds.Add(node.Id);
            }

            foreach (BTNode child in node.Children)
            {
                EnsureNodeIds(child, usedIds, ref maxId);
            }
        }
    }
}
