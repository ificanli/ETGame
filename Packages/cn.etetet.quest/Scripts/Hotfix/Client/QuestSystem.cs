namespace ET.Client
{
    [EntitySystemOf(typeof(Quest))]
    public static partial class QuestSystem
    {
        [EntitySystem]
        private static void Awake(this Quest self)
        {
            self.Objectives.Clear();
        }

        [EntitySystem]
        private static void Destroy(this Quest self)
        {
            // 清理所有目标Entity
            foreach (var objRef in self.Objectives)
            {
                QuestObjective obj = objRef;
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
        public static void AddObjective(this Quest self, long objectiveId, int count, int needCount)
        {
            QuestObjective objective = self.AddChildWithId<QuestObjective>(objectiveId);
            objective.Count = count;
            objective.NeedCount = needCount;
            
            self.Objectives.Add(objective);
        }

        /// <summary>
        /// 更新任务目标进度
        /// </summary>
        public static void UpdateObjective(this Quest self, long objectiveId, int count)
        {
            foreach (var objRef in self.Objectives)
            {
                QuestObjective obj = objRef;
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
        public static QuestObjective GetObjective(this Quest self, long objectiveId)
        {
            foreach (var objRef in self.Objectives)
            {
                QuestObjective obj = objRef;
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
        public static bool CanSubmit(this Quest self)
        {
            foreach (var objRef in self.Objectives)
            {
                QuestObjective obj = objRef;
                if (!obj.IsFinished())
                {
                    return false;
                }
            }
            return true;
        }
    }
}