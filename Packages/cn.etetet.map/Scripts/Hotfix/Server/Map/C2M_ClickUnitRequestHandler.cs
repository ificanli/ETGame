using System.Collections.Generic;

namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class C2M_ClickUnitRequestHandler : MessageLocationHandler<Unit, C2M_ClickUnitRequest, M2C_ClickUnitResponse>
	{
		protected override async ETTask Run(Unit unit, C2M_ClickUnitRequest request, M2C_ClickUnitResponse response)
		{
			await ETTask.CompletedTask;
			
			Unit target = unit.GetParent<UnitComponent>().Get(request.UnitId);
			if (target == null)
			{
				return;
			}

			unit.GetComponent<TargetComponent>().Unit = target;
			
			// 有可接任务则发送任务信息
			QuestComponent questComponent = unit.GetComponent<QuestComponent>();
			
			HashSet<int> allIds = QuestConfigCategory.Instance.GetAllQuestsById(target.Id);
			HashSet<int> questIds = QuestConfigCategory.Instance.GetAcceptQuestsById(target.Id);
			HashSet<int> submitQuestIds = QuestConfigCategory.Instance.GetSubmitQuestsById(target.Id);
			
			foreach (int questId in allIds)
			{
				Quest acceptedQuest = questComponent.GetQuest(questId);
				if (acceptedQuest != null) // 已接任务
				{
					Show_QuestInfo questInfo = Show_QuestInfo.Create();
					questInfo.QuestId = questId;
					// 已接已完成
					if (submitQuestIds.Contains(questId))
					{
						questInfo.Status = (int)QuestStatus.Submited;
					}
					else
					{
						questInfo.Status = (int)QuestStatus.InProgress;	
					}
					response.questInfo.Add(questInfo);
				}
				else
				{
					if (!questIds.Contains(questId))
					{
						continue;
					}
					
					// 可接
					Show_QuestInfo questInfo = Show_QuestInfo.Create();
					questInfo.QuestId = questId;
					questInfo.Status = (int)QuestStatus.Available;

					response.questInfo.Add(questInfo);
				}
			}
		}
	}
}