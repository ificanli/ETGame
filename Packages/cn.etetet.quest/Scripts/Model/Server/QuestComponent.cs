using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class QuestComponent: Entity, IAwake, IDestroy, IScene
    {
        /// <summary>
        /// 已完成的任务Id集合
        /// </summary>
        public HashSet<int> FinishedQuests = new();

        /// <summary>
        /// 任务目标类型映射（QuestObjectiveType->EntityRef<QuestObjective>集合）
        /// </summary>
        public MultiMapSet<QuestObjectiveType, EntityRef<QuestObjective>> QuestObjectives = new();
        
        [BsonIgnore]
        public Fiber Fiber { get; set; }
        
        [BsonIgnore]
        public int SceneType { get; set; }
    }
}