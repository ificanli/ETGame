using System.Collections.Generic;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_SyncQuestDataHandler : MessageLocationHandler<Unit, C2M_SyncQuestData, M2C_SyncQuestData>
    {
        protected override async ETTask Run(Unit unit, C2M_SyncQuestData request, M2C_SyncQuestData response)
        {
            // 获取QuestComponent
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();

            foreach (KeyValuePair<long, Entity> kv in questComponent.Children)
            {
                if (kv.Value is not Quest quest)
                {
                    continue;
                }
                QuestInfo questInfo = QuestInfo.Create();
                questInfo.QuestId = quest.Id;
                foreach (var kv2 in quest.Children)
                {
                    if (kv2.Value is not QuestObjective questObjective)
                    {
                        continue;
                    }
                    QuestObjectiveInfo questObjectiveInfo = QuestObjectiveInfo.Create();
                    questObjectiveInfo.Count = questObjective.Count;
                    questObjectiveInfo.NeedCount = questObjective.GetConfig().NeedCount;
                    questObjectiveInfo.QuestObjectiveId = (int)questObjective.Id;
                    questInfo.Objectives.Add(questObjectiveInfo);
                }
                
                response.QuestList.Add(questInfo);
            }
            
            await ETTask.CompletedTask;
        }
    }
}