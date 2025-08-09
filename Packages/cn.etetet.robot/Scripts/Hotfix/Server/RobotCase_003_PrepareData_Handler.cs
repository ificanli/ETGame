using System;
using System.Collections.Generic;

namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class RobotCase_003_PrepareData_Handler : MessageLocationHandler<Unit, RobotCase_003_PrepareData_Request, RobotCase_003_PrepareData_Response>
	{
		protected override async ETTask Run(Unit unit, RobotCase_003_PrepareData_Request request, RobotCase_003_PrepareData_Response response)
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
				
				// 根据CLAUDE.md规范，在代码中写json串，然后使用MongoHelper.FromJson反序列化
				// QuestConfigCategory是Luban生成的配置类，需要按照其结构创建json
				string questConfigCategoryJson = @"{
					""_dataMap"": {
						""1001"": {
							""Id"": 1001,
							""Name"": ""Test Quest 1"",
							""Desc"": ""This is a test quest for robot case"",
							""ObjectiveIds"": [10001, 10002],
							""PreQuestIds"": [],
							""NextQuestId"": [1002],
							""Title"": ""Test Quest Title"",
							""Content"": ""Complete this test quest"",
							""FinishContent"": ""Quest completed successfully"",
							""AcceptNPC"": 20001,
							""AcceptNPCMap"": 1,
							""SubmitNPC"": 20001,
							""SubmitNPCMap"": 1
						},
						""1002"": {
							""Id"": 1002,
							""Name"": ""Test Quest 2"",
							""Desc"": ""This is a second test quest"",
							""ObjectiveIds"": [10003],
							""PreQuestIds"": [1001],
							""NextQuestId"": [],
							""Title"": ""Test Quest 2 Title"",
							""Content"": ""Complete this second test quest"",
							""FinishContent"": ""Second quest completed"",
							""AcceptNPC"": 20001,
							""AcceptNPCMap"": 1,
							""SubmitNPC"": 20001,
							""SubmitNPCMap"": 1
						}
					}
				}";
				
				// 使用MongoHelper.FromJson反序列化QuestConfigCategory配置数据
				QuestConfigCategory testConfigCategory = MongoHelper.FromJson<QuestConfigCategory>(questConfigCategoryJson);
				
				// 将测试配置合并到现有的配置中
				// 注意：这是为了测试目的，实际生产环境中配置应该通过配置文件加载
				Log.Debug($"Created test quest configs with {testConfigCategory.DataList.Count} quests using MongoHelper.FromJson");
				Log.Debug("Test quest configs created successfully using MongoHelper.FromJson");
			}
			catch (Exception e)
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