using System;

namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class C2M_RobotCase_PrepareData_003_Handler : MessageLocationHandler<Unit, C2M_RobotCase_PrepareData_003_Request, M2C_RobotCase_PrepareData_003_Response>
	{
		protected override async ETTask Run(Unit unit, C2M_RobotCase_PrepareData_003_Request request, M2C_RobotCase_PrepareData_003_Response response)
		{
			// 创建EntityRef以便在await后安全使用
			EntityRef<Unit> unitRef = unit;
			
			try
			{
				Log.Debug("Preparing Quest test data for RobotCase 003");
				
				// 确保QuestConfigCategory有测试数据
				await CreateTestQuestConfigs();
				
				// 在await后重新获取unit
				unit = unitRef;
				if (unit == null) 
				{
					response.Error = ErrorCore.ERR_WithException;
					response.Message = "Unit is null after await";
					return;
				}
				
				// 获取场景的UnitComponent
				Scene scene = unit.Scene();
				UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
				
				// 为玩家添加QuestComponent（如果没有的话）
				if (unit.GetComponent<QuestComponent>() == null)
				{
					unit.AddComponent<QuestComponent>();
					Log.Debug($"Added QuestComponent to player unit {unit.Id}");
				}
				
				// 创建测试NPC（如果不存在）
				long testNPCId = 20001;
				Unit testNPC = unitComponent.Get(testNPCId);
				if (testNPC == null)
				{
					// 使用UnitFactory创建NPC
					testNPC = UnitFactory.Create(scene, testNPCId, 1001); // 假设1001是NPC配置ID
					testNPC.Position = new Unity.Mathematics.float3(unit.Position.x + 5, unit.Position.y, unit.Position.z);
					
					Log.Debug($"Created test NPC {testNPCId} at position {testNPC.Position}");
				}
				
				// 初始化一些测试任务数据
				await InitializeTestQuestData(unit);
				
				Log.Debug("Quest test data preparation completed successfully");
			}
			catch (System.Exception e)
			{
				Log.Error($"Failed to prepare Quest test data: {e.Message}");
				response.Error = ErrorCore.ERR_WithException;
				response.Message = e.Message;
			}
			
			await ETTask.CompletedTask;
		}
		
		/// <summary>
		/// 创建测试用的Quest配置数据
		/// </summary>
		private async ETTask CreateTestQuestConfigs()
		{
			try
			{
				// 检查QuestConfigCategory是否已经有数据
				QuestConfigCategory questConfigCategory = QuestConfigCategory.Instance;
				if (questConfigCategory.GetOrDefault(1001) != null)
				{
					Log.Debug("Quest config 1001 already exists");
					return;
				}
				
				Log.Debug("Skipping quest config creation - using direct handler data");
			}
			catch (System.Exception e)
			{
				Log.Error($"Failed to create test quest configs: {e.Message}");
			}
			
			await ETTask.CompletedTask;
		}
		
		/// <summary>
		/// 初始化测试任务数据
		/// </summary>
		private async ETTask InitializeTestQuestData(Unit unit)
		{
			try
			{
				QuestComponent questComponent = unit.GetComponent<QuestComponent>();
				if (questComponent == null) return;
				
				// 添加一些可接取的任务到可用列表
				int testQuestId = 1001;
				if (!questComponent.AvailableQuests.Contains(testQuestId))
				{
					questComponent.AvailableQuests.Add(testQuestId);
					Log.Debug($"Added quest {testQuestId} to available quests");
				}
				
				// 可以在这里添加更多测试任务
				int secondQuestId = 1002;
				if (!questComponent.AvailableQuests.Contains(secondQuestId))
				{
					questComponent.AvailableQuests.Add(secondQuestId);
					Log.Debug($"Added quest {secondQuestId} to available quests");
				}
				
				Log.Debug($"Initialized test quest data with {questComponent.AvailableQuests.Count} available quests");
			}
			catch (System.Exception e)
			{
				Log.Error($"Failed to initialize test quest data: {e.Message}");
			}
			
			await ETTask.CompletedTask;
		}
		
	}
}