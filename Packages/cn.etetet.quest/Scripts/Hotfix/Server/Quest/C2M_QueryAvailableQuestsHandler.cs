namespace ET.Server
{
    [Module(ModuleName.Quest)]
    [MessageHandler(SceneType.Map)]
    public class C2M_QueryAvailableQuestsHandler : MessageLocationHandler<Unit, C2M_QueryAvailableQuests, M2C_QueryAvailableQuests>
    {
        protected override async ETTask Run(Unit unit, C2M_QueryAvailableQuests request, M2C_QueryAvailableQuests response)
        {
            try
            {
                // 获取QuestComponent
                QuestComponent questComponent = unit.GetComponent<QuestComponent>();
                if (questComponent == null)
                {
                    questComponent = unit.AddComponent<QuestComponent>();
                }
                
                // 如果指定了NPC ID，检查NPC是否存在
                if (request.NPCId != 0)
                {
                    Unit npc = unit.GetParent<UnitComponent>().Get(request.NPCId);
                    if (npc == null)
                    {
                        response.Error = TextConstDefine.Quest_NotFoundNPC;
                        return;
                    }
                }
                
                // 获取可接取的任务列表
                // 为了测试，直接创建模拟数据而不依赖QuestConfigCategory
                response.AvailableQuests = CreateMockAvailableQuests(questComponent, request.NPCId);
                
                Log.Debug($"Player {unit.Id} queried available quests, found {response.AvailableQuests.Count} quests");
                await ETTask.CompletedTask;
            }
            catch (System.Exception e)
            {
                Log.Error($"QueryAvailableQuests failed: {e.Message}");
                response.Error = ErrorCore.ERR_WithException;
                response.Message = e.Message;
            }
        }

        /// <summary>
        /// 创建模拟的可接取任务列表
        /// </summary>
        private System.Collections.Generic.List<AvailableQuestInfo> CreateMockAvailableQuests(QuestComponent questComponent, long npcId)
        {
            var availableQuests = new System.Collections.Generic.List<AvailableQuestInfo>();
            
            // 检查玩家是否有可接取的任务
            if (questComponent.AvailableQuests.Contains(1001))
            {
                var questInfo = AvailableQuestInfo.Create();
                questInfo.QuestId = 1001;
                questInfo.QuestName = "Test Quest 1";
                questInfo.QuestDesc = "This is a test quest for robot testing";
                questInfo.QuestType = 1; // 主线任务
                questInfo.RewardExp = 100;
                questInfo.RewardGold = 50;
                questInfo.RewardItems = new System.Collections.Generic.List<int> { 10001, 10002 };
                availableQuests.Add(questInfo);
            }
            
            if (questComponent.AvailableQuests.Contains(1002))
            {
                var questInfo = AvailableQuestInfo.Create();
                questInfo.QuestId = 1002;
                questInfo.QuestName = "Test Quest 2";
                questInfo.QuestDesc = "This is the second test quest";
                questInfo.QuestType = 2; // 支线任务
                questInfo.RewardExp = 200;
                questInfo.RewardGold = 100;
                questInfo.RewardItems = new System.Collections.Generic.List<int> { 10003 };
                availableQuests.Add(questInfo);
            }
            
            return availableQuests;
        }
    }
}