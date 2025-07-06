using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    [Module(ModuleName.Quest)]
    [ComponentOf(typeof(Unit))]
    public class QuestComponent: Entity, IAwake, IDestroy, IScene
    {
        /// <summary>
        /// 已完成的任务Id集合
        /// </summary>
        public HashSet<int> FinishedQuests = new();

        /// <summary>
        /// 进行中的任务（任务Id->EntityRef<Quest>）
        /// </summary>
        public Dictionary<int, EntityRef<Quest>> ActiveQuests = new();

        /// <summary>
        /// 可接任务Id集合
        /// </summary>
        public HashSet<int> AvailableQuests = new();

        /// <summary>
        /// 任务进度字典（任务Id->进度字典）
        /// </summary>
        public Dictionary<int, Dictionary<int, int>> QuestProgressDict = new();

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