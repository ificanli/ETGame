using Unity.Mathematics;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_AcceptQuestHandler : MessageLocationHandler<Unit, C2M_AcceptQuest, M2C_AcceptQuest>
    {
        protected override async ETTask Run(Unit unit, C2M_AcceptQuest request, M2C_AcceptQuest response)
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
            // 判断NPC是否可接该任务
            if (questComponent.IsPreQuestFinished(request.QuestId))
            {
                return;
            }
            
            QuestHelper.AddQuest(unit, request.QuestId);
            await ETTask.CompletedTask;
        }
    }
}