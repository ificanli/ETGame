using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.Quest)]
    [MessageHandler(SceneType.Map)]
    public class C2M_SubmitQuestHandler : MessageLocationHandler<Unit, C2M_SubmitQuest, M2C_SubmitQuest>
    {
        protected override async ETTask Run(Unit unit, C2M_SubmitQuest request, M2C_SubmitQuest response)
        {
            // 判断NPC是否存在
            Unit npc = unit.GetParent<UnitComponent>().Get(request.NPCId);
            if (npc == null)
            {
                response.Error = TextConstDefine.Quest_NotFoundNPC;
                return;
            }
            
            // NPC距离
            if (math.distance(unit.Position, npc.Position) > 10f)
            {
                response.Error = TextConstDefine.Quest_NPCTooFar;
                return;
            }
            
            // 获取QuestComponent
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                response.Error = TextConstDefine.Quest_ComponentNotFound;
                return;
            }
            
            // 检查任务是否存在
            if (!questComponent.ActiveQuests.TryGetValue(request.QuestId, out EntityRef<Quest> questRef))
            {
                response.Error = TextConstDefine.Quest_NotFound;
                return;
            }
            
            Quest quest = questRef;
            if (quest == null)
            {
                response.Error = TextConstDefine.Quest_NotFound;
                return;
            }

            // 检查任务是否可以提交
            if (quest.Status != QuestStatus.CanSubmit)
            {
                response.Error = TextConstDefine.Quest_NotFinish;
                return;
            }
            
            // 完成任务
            FinishQuestForPlayer(unit, request.QuestId);
            
            Log.Debug($"Player {unit.Id} submitted quest {request.QuestId}");
            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 完成玩家任务
        /// </summary>
        private static void FinishQuestForPlayer(Unit unit, int questId)
        {
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();
            
            // 从活跃任务中移除
            if (questComponent.ActiveQuests.TryGetValue(questId, out EntityRef<Quest> questRef))
            {
                Quest quest = questRef;
                if (quest != null)
                {
                    quest.Status = QuestStatus.Finished;
                    quest.Dispose(); // 释放Quest实体
                }
                questComponent.ActiveQuests.Remove(questId);
            }
            
            // 添加到已完成任务列表
            questComponent.FinishedQuests.Add(questId);
            
            Log.Debug($"Quest {questId} finished for player {unit.Id}");
        }
    }
}