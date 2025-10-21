namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_QueryAvailableQuestsHandler : MessageLocationHandler<Unit, C2M_QueryAvailableQuests, M2C_QueryAvailableQuests>
    {
        protected override async ETTask Run(Unit unit, C2M_QueryAvailableQuests request, M2C_QueryAvailableQuests response)
        {
            // 如果指定了NPC ID，检查NPC是否存在
            if (request.NPCId == 0)
            {
                return;
            }
            
            Unit npc = unit.GetParent<UnitComponent>().Get(request.NPCId);
            if (npc == null)
            {
                response.Error = TextConstDefine.Quest_NotFoundNPC;
                return;
            }
            
            // 获取QuestComponent
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();

            await ETTask.CompletedTask;
        }
    }
}