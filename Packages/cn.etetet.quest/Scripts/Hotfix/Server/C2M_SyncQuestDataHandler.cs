namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_SyncQuestDataHandler : MessageLocationHandler<Unit, C2M_SyncQuestData, M2C_SyncQuestData>
    {
        protected override async ETTask Run(Unit unit, C2M_SyncQuestData request, M2C_SyncQuestData response)
        {
            // 获取QuestComponent
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                questComponent = unit.AddComponent<QuestComponent>();
            }

            // 获取当前玩家的所有任务数据
            response.QuestList = GetPlayerQuestList(questComponent);

            Log.Debug($"Player {unit.Id} synced quest data, found {response.QuestList.Count} active quests");
            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 获取玩家任务列表
        /// </summary>
        private System.Collections.Generic.List<QuestInfo> GetPlayerQuestList(QuestComponent questComponent)
        {
            var questList = new System.Collections.Generic.List<QuestInfo>();

            // 遍历所有活跃任务
            foreach (var kvp in questComponent.ActiveQuests)
            {
                Quest quest = kvp.Value;
                if (quest != null)
                {
                    var questInfo = QuestInfo.Create();
                    questInfo.QuestId = quest.ConfigId;
                    questInfo.Status = (int)quest.Status;
                    questInfo.Objectives = new System.Collections.Generic.List<QuestObjectiveInfo>();
                    questInfo.AcceptTime = TimeInfo.Instance.ServerNow(); // 模拟接取时间
                    questInfo.CompleteTime = 0; // 未完成

                    questList.Add(questInfo);
                }
            }

            return questList;
        }
    }
}