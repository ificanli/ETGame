using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.Quest)]
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
            if (questComponent == null)
            {
                questComponent = unit.AddComponent<QuestComponent>();
            }
            
            // 检查是否可以接取这个任务
            if (!questComponent.AvailableQuests.Contains(request.QuestId))
            {
                response.Error = TextConstDefine.Quest_NotAvailable;
                return;
            }
            
            // 检查是否已经接取过这个任务
            if (questComponent.ActiveQuests.ContainsKey(request.QuestId))
            {
                response.Error = TextConstDefine.Quest_AlreadyAccepted;
                return;
            }
            
            // 接取任务 - 创建Quest实体
            AddQuestToPlayer(unit, request.QuestId);
            
            Log.Debug($"Player {unit.Id} accepted quest {request.QuestId}");
            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 为玩家添加任务
        /// </summary>
        private static void AddQuestToPlayer(Unit unit, int questId)
        {
            QuestComponent questComponent = unit.GetComponent<QuestComponent>();
            
            // 创建Quest实体
            Quest quest = questComponent.AddChild<Quest, int>(questId);
            quest.ConfigId = questId;
            quest.Status = QuestStatus.InProgress;
            
            // 添加到活跃任务列表
            EntityRef<Quest> questRef = quest;
            questComponent.ActiveQuests[questId] = questRef;
            
            // 从可接取列表中移除
            questComponent.AvailableQuests.Remove(questId);
            
            // 模拟任务直接完成（为了测试）
            quest.Status = QuestStatus.CanSubmit;
            
            Log.Debug($"Quest {questId} added to player and marked as can submit for testing");
        }
    }
}