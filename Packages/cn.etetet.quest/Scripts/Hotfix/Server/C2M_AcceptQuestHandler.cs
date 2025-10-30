using Unity.Mathematics;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_AcceptQuestHandler : MessageLocationHandler<Unit, C2M_AcceptQuest, M2C_AcceptQuest>
    {
        protected override async ETTask Run(Unit unit, C2M_AcceptQuest request, M2C_AcceptQuest response)
        {
            QuestConfig questConfig = QuestConfigCategory.Instance.Get((int)request.QuestId);
            if (questConfig == null)
            {
                Log.Warning($"not found request: {request.QuestId}");
                return;
            }
            
            // 判断NPC是否存在
            Unit npc = unit.GetParent<UnitComponent>().Get(questConfig.AcceptNPC);
            if (npc == null)
            {
                Log.Warning($"not found npc: {questConfig.AcceptNPC}");
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
            
            // 检查是否已经接取过这个任务
            if (questComponent.GetQuest(request.QuestId) != null)
            {
                response.Error = TextConstDefine.Quest_AlreadyAccepted;
                return;
            }

            // 检查任务是否可以接取，主要防止外挂
            if (!QuestHelper.CanAcceptQuest(unit, request.QuestId))
            {
                Log.Warning($"can not accept quest: {unit.Id} {request.QuestId}");
                return;
            }
            
            // 接取任务 - 创建Quest实体
            QuestHelper.AddQuest(unit, request.QuestId);
            await ETTask.CompletedTask;
        }
    }
}