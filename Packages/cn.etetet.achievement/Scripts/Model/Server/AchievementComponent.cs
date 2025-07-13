using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    /// <summary>
    /// 成就管理组件，挂载在Unit上
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class AchievementComponent: Entity, IAwake, IDestroy, IScene
    {
        /// <summary>
        /// 已完成的成就Id集合
        /// </summary>
        public HashSet<int> CompletedAchievements = new();

        /// <summary>
        /// 已领取奖励的成就Id集合
        /// </summary>
        public HashSet<int> ClaimedAchievements = new();

        /// <summary>
        /// 进行中的成就（成就Id->EntityRef<Achievement>）
        /// </summary>
        public Dictionary<int, EntityRef<Achievement>> ActiveAchievements = new();

        /// <summary>
        /// 成就进度字典（成就Id->当前进度）
        /// </summary>
        public Dictionary<int, int> AchievementProgress = new();

        /// <summary>
        /// 成就类型映射（AchievementType->成就Id集合）
        /// </summary>
        public MultiMapSet<AchievementType, int> TypeMapping = new();

        /// <summary>
        /// 总成就点数
        /// </summary>
        public int TotalPoints;

        /// <summary>
        /// 已获得成就点数
        /// </summary>
        public int EarnedPoints;

        /// <summary>
        /// 最近完成的成就列表（最多保存10个）
        /// </summary>
        public List<int> RecentAchievements = new();

        [BsonIgnore]
        public Fiber Fiber { get; set; }
        
        [BsonIgnore]
        public int SceneType { get; set; }
    }
}