using System;
using System.Collections.Generic;
using System.Text;

namespace ET
{
    /// <summary>
    /// 行为树调试路径记录信息
    /// </summary>
    public class BTDebugPathInfo
    {
        /// <summary>
        /// 实体ID
        /// </summary>
        public long EntityId;

        /// <summary>
        /// 行为树ID
        /// </summary>
        public long TreeId;

        /// <summary>
        /// 执行路径（节点ID列表）
        /// </summary>
        public List<int> Path;

        /// <summary>
        /// 记录时间
        /// </summary>
        public DateTime RecordTime;

        /// <summary>
        /// 执行次数计数
        /// </summary>
        public int Count;

        /// <summary>
        /// Snapshot StringBuilder reference (for lazy conversion)
        /// </summary>
        public StringBuilder Snapshot;

        public BTDebugPathInfo(long entityId, long treeId, List<int> path, StringBuilder snapshot)
        {
            this.EntityId = entityId;
            this.TreeId = treeId;
            this.Path = path;
            this.RecordTime = DateTime.Now;
            this.Count = 1;
            // 保存StringBuilder引用，延迟转换
            this.Snapshot = snapshot;
        }
    }

    /// <summary>
    /// 行为树调试路径记录器（编辑器实例）
    /// </summary>
    public class BTDebugPathRecorder
    {
        /// <summary>
        /// 所有记录的路径信息
        /// </summary>
        private readonly List<BTDebugPathInfo> pathInfos = new();

        /// <summary>
        /// 当前关注的TreeId（只记录此TreeId的路径）
        /// </summary>
        private long currentTreeId;

        /// <summary>
        /// 当前关注的EntityId（只记录此EntityId的路径，0表示记录所有）
        /// </summary>
        private long currentEntityId;

        /// <summary>
        /// 设置当前关注的TreeId
        /// </summary>
        public void SetCurrentTreeId(long treeId)
        {
            this.currentTreeId = treeId;
        }

        /// <summary>
        /// 设置当前关注的EntityId（0表示记录所有）
        /// </summary>
        public void SetCurrentEntityId(long entityId)
        {
            this.currentEntityId = entityId;
        }

        /// <summary>
        /// 记录一次路径
        /// </summary>
        public void RecordPath(long entityId, long treeId, List<int> path, StringBuilder snapshot)
        {
            if (path == null || path.Count == 0)
            {
                return;
            }

            // 只记录当前关注的TreeId的路径
            if (this.currentTreeId != 0 && treeId != this.currentTreeId)
            {
                return;
            }

            // 只记录当前关注的EntityId的路径（0表示记录所有）
            if (this.currentEntityId != 0 && entityId != this.currentEntityId)
            {
                return;
            }

            // 检查是否与上次路径相同（包括entityId），如果相同则增加计数
            if (this.pathInfos.Count > 0)
            {
                BTDebugPathInfo lastInfo = this.pathInfos[this.pathInfos.Count - 1];
                if (lastInfo.EntityId == entityId && IsPathEqual(lastInfo.Path, path))
                {
                    lastInfo.Count++;
                    lastInfo.RecordTime = DateTime.Now;
                    lastInfo.Snapshot = snapshot;
                    return;
                }
            }

            // 记录路径
            BTDebugPathInfo info = new(entityId, treeId, path, snapshot);
            this.pathInfos.Add(info);
        }

        /// <summary>
        /// 比较两个路径是否相同
        /// </summary>
        private static bool IsPathEqual(List<int> path1, List<int> path2)
        {
            if (path1.Count != path2.Count)
            {
                return false;
            }

            for (int i = 0; i < path1.Count; i++)
            {
                if (path1[i] != path2[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 获取所有路径信息
        /// </summary>
        public List<BTDebugPathInfo> GetAllPaths()
        {
            return this.pathInfos;
        }

        /// <summary>
        /// 清除所有记录
        /// </summary>
        public void Clear()
        {
            this.pathInfos.Clear();
        }
    }
}
