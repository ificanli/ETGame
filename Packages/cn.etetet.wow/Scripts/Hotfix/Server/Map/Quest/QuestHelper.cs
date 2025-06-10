namespace ET.Server
{
    public static class QuestHelper
    {
        public static void AddQuest(Unit self, int configId)
        {
            Quest quest = self.GetComponent<QuestComponent>().AddQuest(configId);
            
            // 通知任务
            M2C_CreateQuest createQuest = M2C_CreateQuest.Create();
            foreach (var kv in quest.Children)
            {
                QuestObjective objective = (QuestObjective)kv.Value;
                QuestObjectiveConfig objectiveConfig = objective.GetConfig();
                QuestObjectiveInfo questObjectiveInfo = QuestObjectiveInfo.Create();
                questObjectiveInfo.QuestObjectiveId = objectiveConfig.Id;
                questObjectiveInfo.NeedCount = objectiveConfig.NeedCount;
                createQuest.QuestObjective.Add(questObjectiveInfo);
            }
            
            MapMessageHelper.NoticeClient(self, createQuest, NoticeType.Self);
        }
        
        public static void FinishQuest(Unit self, int questId)
        {
            QuestComponent questComponent = self.GetComponent<QuestComponent>();
            if (!questComponent.TryFinishQuest(questId))
            {
                return;
            }
            
            // 通知任务完成
            M2C_UpdateQuest updateQuest = M2C_UpdateQuest.Create();
            updateQuest.QuestId = questId;
            updateQuest.State = (int)QuestStatus.Finished;
            
            MapMessageHelper.NoticeClient(self, updateQuest, NoticeType.Self);
        }
    }
}