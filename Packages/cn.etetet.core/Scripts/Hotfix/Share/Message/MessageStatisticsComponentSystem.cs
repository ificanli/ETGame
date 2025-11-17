using System;
using System.Collections.Generic;
using System.Text;

namespace ET
{
    /// <summary>
    /// 消息统计组件系统
    /// </summary>
    [EntitySystemOf(typeof(MessageStatisticsComponent))]
    public static partial class MessageStatisticsComponentSystem
    {
        #region 生命周期方法

        [EntitySystem]
        private static void Awake(this MessageStatisticsComponent self)
        {
            self.CurrentSecondStart = TimeInfo.Instance.ClientNow();
        }

        [EntitySystem]
        private static void Destroy(this MessageStatisticsComponent self)
        {
            self.MessageStats.Clear();
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 记录消息类型
        /// </summary>
        /// <param name="self">当前组件实例</param>
        /// <param name="messageType">消息类型</param>
        /// <param name="maxMessageCount">消息数量阈值</param>
        /// <returns>true: 正常, false: 超过阈值</returns>
        public static bool Check(this MessageStatisticsComponent self, Type messageType, int maxMessageCount)
        {
            long now = TimeInfo.Instance.ClientNow();

            // 检查是否超过统计周期，超过则清空重新统计
            if (now - self.CurrentSecondStart >= self.WindowSize)
            {
                self.MessageStats.Clear();
                self.TotalCount = 0;
                self.CurrentSecondStart = now;
            }

            // 累加消息计数
            int count = self.MessageStats.GetValueOrDefault(messageType, 0);
            self.MessageStats[messageType] = count + 1;
            
            // 累加消息总数
            self.TotalCount++;

            // 如果消息总数超过阈值，打印统计信息并重置
            if (self.TotalCount <= maxMessageCount)
            {
                return true;
            }

            self.PrintStatistics(maxMessageCount);
                
            // 重置统计
            self.MessageStats.Clear();
            self.TotalCount = 0;
            self.CurrentSecondStart = now;
                
            return false;
        }

        /// <summary>
        /// 打印统计信息
        /// 输出当前秒内的消息统计
        /// </summary>
        /// <param name="self">当前组件实例</param>
        /// <param name="maxMessageCount"></param>
        public static void PrintStatistics(this MessageStatisticsComponent self, int maxMessageCount)
        {
            Dictionary<Type, int> stats = self.MessageStats;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("========== Message Statistics ==========");
            sb.AppendLine($"Window: Current Second ({self.WindowSize}ms)");
            sb.AppendLine($"Total Messages: {self.TotalCount}, MaxMessageCount: {maxMessageCount}");

            if (stats.Count > 0)
            {
                sb.AppendLine("Message Types:");
                foreach (var kvp in stats)
                {
                    sb.AppendLine($"  {kvp.Key.FullName}: {kvp.Value}");
                }
            }
            else
            {
                sb.AppendLine("No messages in current second");
            }

            sb.AppendLine("========================================");
            Log.Error(sb.ToString());
        }

        #endregion
    }
}