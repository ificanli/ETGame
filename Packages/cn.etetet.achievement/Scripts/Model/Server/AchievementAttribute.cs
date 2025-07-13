using System;

namespace ET.Server
{
    /// <summary>
    /// 成就处理器标记特性
    /// </summary>
    [EnableClass]
    [AttributeUsage(AttributeTargets.Class)]
    public class AchievementHandlerAttribute: Attribute
    {
        /// <summary>
        /// 成就类型
        /// </summary>
        public AchievementType AchievementType { get; }

        public AchievementHandlerAttribute(AchievementType achievementType)
        {
            AchievementType = achievementType;
        }
    }
}