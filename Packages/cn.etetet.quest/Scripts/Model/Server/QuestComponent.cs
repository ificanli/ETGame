using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class QuestComponent: Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 已完成的任务Id集合
        /// </summary>
        public HashSet<int> FinishedQuests = new();

        public MultiMapSet<QuestObjectiveType, EntityRef<QuestObjective>> QuestObjectives = new();
    }
}