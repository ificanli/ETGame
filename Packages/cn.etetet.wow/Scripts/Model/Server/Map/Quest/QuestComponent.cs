using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class QuestComponent: Entity, IAwake, IDestroy, IScene
    {
        // 已完成的任务
        public HashSet<int> FinishedQuests = new();
        
        public MultiMapSet<QuestObjectiveType, EntityRef<QuestObjective>> QuestObjectives = new();
        
        [BsonIgnore]
        public Fiber Fiber { get; set; }
        
        [BsonIgnore]
        public int SceneType { get; set; }
    }
}