using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 服务信息系统
    /// </summary>
    [EntitySystemOf(typeof(ServiceInfo))]
    public static partial class ServiceInfoSystem
    {
        [EntitySystem]
        private static void Awake(this ServiceInfo self, string sceneName, ActorId actorId)
        {
            self.SceneName = sceneName;
            self.ActorId = actorId;
            self.Metadata.Clear();
            self.RegisterTime = TimeInfo.Instance.ServerNow();
            self.LastHeartbeatTime = self.RegisterTime;
        }

        [EntitySystem]
        private static void Destroy(this ServiceInfo self)
        {
        }

        /// <summary>
        /// 更新心跳时间
        /// </summary>
        public static void UpdateHeartbeat(this ServiceInfo self)
        {
            self.LastHeartbeatTime = TimeInfo.Instance.ServerNow();
        }

        /// <summary>
        /// 检查是否心跳超时
        /// </summary>
        public static bool IsHeartbeatTimeout(this ServiceInfo self, long timeoutMs)
        {
            long now = TimeInfo.Instance.ServerNow();
            return now - self.LastHeartbeatTime > timeoutMs;
        }

        /// <summary>
        /// 检查元数据是否匹配过滤条件
        /// 支持多值匹配，过滤条件的值可以用逗号分割，如 "Gate,Realm"
        /// </summary>
        public static bool MatchesFilter(this ServiceInfo self, StringKV filter)
        {
            if (filter.Count == 0)
            {
                return true;
            }

            foreach (var kvp in filter)
            {
                if (!self.Metadata.TryGetValue(kvp.Key, out string metadataValue))
                {
                    return false;
                }

                bool matched = false;

                // 只有包含逗号时才进行分割，优化性能
                if (kvp.Value.Contains(','))
                {
                    // 支持多值匹配，用逗号分割
                    string[] filterValues = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string filterValue in filterValues)
                    {
                        string trimmedFilterValue = filterValue.Trim();
                        if (metadataValue == trimmedFilterValue)
                        {
                            matched = true;
                            break;
                        }
                    }
                }
                else
                {
                    // 单值匹配，直接比较
                    matched = metadataValue == kvp.Value;
                }

                if (!matched)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 转换为Proto格式
        /// </summary>
        public static ServiceInfoProto ToProto(this ServiceInfo self)
        {
            ServiceInfoProto proto = ServiceInfoProto.Create();
            proto.SceneName = self.SceneName;
            proto.ActorId = self.ActorId;

            foreach (var kvp in self.Metadata)
            {
                proto.Metadata[kvp.Key] = kvp.Value;
            }

            return proto;
        }
    }
}