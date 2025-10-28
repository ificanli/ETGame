namespace ET.Client
{
    [EntitySystemOf(typeof(ClientQuest))]
    public static partial class ClientQuestSystem
    {
        [EntitySystem]
        private static void Awake(this ClientQuest self)
        {
            self.Objectives.Clear();
        }

        [EntitySystem]
        private static void Destroy(this ClientQuest self)
        {
            // 清理所有目标Entity
            foreach (var objRef in self.Objectives)
            {
                ClientQuestObjective obj = objRef;
                if (obj != null)
                {
                    obj.Dispose();
                }
            }
            self.Objectives.Clear();
        }

        /// <summary>
        /// 添加任务目标
        /// </summary>
        public static void AddObjective(this ClientQuest self, long objectiveId, int count, int needCount)
        {
            ClientQuestObjective objective = self.AddChildWithId<ClientQuestObjective>(objectiveId);
            objective.Count = count;
            objective.NeedCount = needCount;
            
            self.Objectives.Add(objective);
        }

        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public static void UpdateObjective(this ClientQuest self, long objectiveId, int count)
        {
            foreach (var objRef in self.Objectives)
            {
                ClientQuestObjective obj = objRef;
                if (obj != null && obj.Id == objectiveId)
                {
                    obj.Count = count;
                    break;
                }
            }
        }

        /// <summary>
        /// 获取任务目标
        /// </summary>
        public static ClientQuestObjective GetObjective(this ClientQuest self, long objectiveId)
        {
            foreach (var objRef in self.Objectives)
            {
                ClientQuestObjective obj = objRef;
                if (obj != null && obj.Id == objectiveId)
                {
                    return obj;
                }
            }
            return null;
        }

        /// <summary>
        /// 检查任务是否可以提交
        /// </summary>
        public static bool IsFinished(this ClientQuest self)
        {
            foreach (var objRef in self.Objectives)
            {
                ClientQuestObjective obj = objRef;
                if (!obj.IsFinished())
                {
                    return false;
                }
            }
            return true;
        }
    }
}