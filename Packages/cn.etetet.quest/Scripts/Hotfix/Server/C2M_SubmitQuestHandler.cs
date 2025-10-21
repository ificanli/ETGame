using Unity.Mathematics;

namespace ET.Server
{
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
            
            Quest quest = questComponent.GetQuest(request.QuestId);
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
            
            // 添加到已完成任务列表
            questComponent.FinishedQuests.Add(questId);
        }
    }
}