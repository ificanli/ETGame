namespace ET.Server
{
    [Module(ModuleName.Quest)]
    [EntitySystemOf(typeof(QuestObjective))]
    public static partial class QuestObjectiveSystem
    {
        [EntitySystem]
        private static void Awake(this QuestObjective self, int configId)
        {
            self.ConfigId = configId;

            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();

            // 根据不同的类型添加不同的组件来记录任务数据
            IQuestObjectiveHandler questObjectiveHandler = QuestObjectiveDispatcher.Instance.Get(questObjectiveConfig.Type);
            questObjectiveHandler.Init(self);

            self.Scene<QuestComponent>().QuestObjectives.Add(questObjectiveConfig.Type, self);
        }
        
        [EntitySystem]
        private static void Destroy(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            self.Scene<QuestComponent>()?.QuestObjectives.Remove(questObjectiveConfig.Type, self);
        }

        public static void Process(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            IQuestObjectiveHandler questObjectiveHandler = QuestObjectiveDispatcher.Instance.Get(questObjectiveConfig.Type);
            questObjectiveHandler.Process();
        }

        public static QuestObjectiveConfig GetConfig(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = QuestObjectiveConfigCategory.Instance.Get(self.ConfigId);
            return questObjectiveConfig;
        }

        public static bool IsFinished(this QuestObjective self)
        {
            QuestObjectiveConfig questObjectiveConfig = self.GetConfig();
            IQuestObjectiveHandler questObjectiveHandler = QuestObjectiveDispatcher.Instance.Get(questObjectiveConfig.Type);
            return questObjectiveHandler.IsFinished();
        }

        /// <summary>
        /// 处理击杀怪物目标事件
        /// </summary>
        public static void OnMonsterKilled(this QuestObjective self, int monsterId)
        {
            QuestObjectiveConfig config = self.GetConfig();
            if (config.Type != QuestObjectiveType.KillMonster)
            {
                return;
            }
            if (self.Params.Count > 0 && self.Params[0] != monsterId)
            {
                return;
            }
            self.Progress++;
            if (self.Progress >= config.NeedCount)
            {
                self.IsCompleted = true;
            }
        }

        /// <summary>
        /// 处理采集物品目标事件
        /// </summary>
        public static void OnItemCollected(this QuestObjective self, int itemId)
        {
            QuestObjectiveConfig config = self.GetConfig();
            if (config.Type != QuestObjectiveType.Collectltem)
            {
                return;
            }
            if (self.Params.Count > 0 && self.Params[0] != itemId)
            {
                return;
            }
            self.Progress++;
            if (self.Progress >= config.NeedCount)
            {
                self.IsCompleted = true;
            }
        }

        /// <summary>
        /// 处理对话目标事件
        /// </summary>
        public static void OnTalkToNpc(this QuestObjective self, int npcId)
        {
            QuestObjectiveConfig config = self.GetConfig();
            // TODO: 需要添加专门的对话任务类型，暂时使用Level类型代替
            if (config.Type != QuestObjectiveType.Level)
            {
                return;
            }
            if (self.Params.Count > 0 && self.Params[0] != npcId)
            {
                return;
            }
            self.Progress = 1;
            self.IsCompleted = true;
        }

        /// <summary>
        /// 处理进入地图目标事件
        /// </summary>
        public static void OnEnterMap(this QuestObjective self, int mapId)
        {
            QuestObjectiveConfig config = self.GetConfig();
            if (config.Type != QuestObjectiveType.EnterMap)
            {
                return;
            }
            if (self.Params.Count > 0 && self.Params[0] != mapId)
            {
                return;
            }
            self.Progress = 1;
            self.IsCompleted = true;
        }
    }
}