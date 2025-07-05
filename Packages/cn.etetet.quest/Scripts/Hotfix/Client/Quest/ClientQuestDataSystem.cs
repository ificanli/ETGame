namespace ET.Client
{
    [Module(ModuleName.Quest)]
    [EntitySystemOf(typeof(ClientQuestData))]
    public static partial class ClientQuestDataSystem
    {
        [EntitySystem]
        private static void Awake(this ClientQuestData self)
        {
            self.Objectives.Clear();
        }

        [EntitySystem]
        private static void Destroy(this ClientQuestData self)
        {
            // 清理所有目标Entity
            foreach (var objRef in self.Objectives)
            {
                ClientQuestObjectiveData obj = objRef;
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
        public static void AddObjective(this ClientQuestData self, int objectiveId, int currentCount, int requiredCount, string description = "")
        {
            ClientQuestObjectiveData objective = self.AddChild<ClientQuestObjectiveData>();
            objective.ObjectiveId = objectiveId;
            objective.CurrentCount = currentCount;
            objective.RequiredCount = requiredCount;
            objective.Description = description;
            objective.IsCompleted = currentCount >= requiredCount;
            
            self.Objectives.Add(objective);
        }

        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public static void UpdateObjective(this ClientQuestData self, int objectiveId, int currentCount)
        {
            foreach (var objRef in self.Objectives)
            {
                ClientQuestObjectiveData obj = objRef;
                if (obj != null && obj.ObjectiveId == objectiveId)
                {
                    obj.CurrentCount = currentCount;
                    obj.IsCompleted = currentCount >= obj.RequiredCount;
                    break;
                }
            }
        }

        /// <summary>
        /// 获取任务目标
        /// </summary>
        public static ClientQuestObjectiveData GetObjective(this ClientQuestData self, int objectiveId)
        {
            foreach (var objRef in self.Objectives)
            {
                ClientQuestObjectiveData obj = objRef;
                if (obj != null && obj.ObjectiveId == objectiveId)
                {
                    return obj;
                }
            }
            return null;
        }

        /// <summary>
        /// 检查任务是否可以提交
        /// </summary>
        public static bool CanSubmit(this ClientQuestData self)
        {
            foreach (var objRef in self.Objectives)
            {
                ClientQuestObjectiveData obj = objRef;
                if (obj != null && !obj.IsCompleted)
                {
                    return false;
                }
            }
            return true;
        }
    }
}