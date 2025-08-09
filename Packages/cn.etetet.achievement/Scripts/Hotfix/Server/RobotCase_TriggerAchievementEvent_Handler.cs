namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class RobotCase_TriggerAchievementEvent_Handler : MessageLocationHandler<Unit, RobotCase_TriggerAchievementEvent_Request, RobotCase_TriggerAchievementEvent_Response>
    {
        protected override async ETTask Run(Unit unit, RobotCase_TriggerAchievementEvent_Request request, RobotCase_TriggerAchievementEvent_Response response)
        {
            try
            {
                Log.Debug($"Triggering achievement event: Type={request.EventType}, ParamId={request.ParamId}, Count={request.Count}");

                // 根据事件类型触发对应事件
                switch (request.EventType)
                {
                    case 1: // 击杀怪物
                        AchievementHelper.TriggerKillMonster(unit, request.ParamId, request.Count);
                        Log.Debug($"Triggered kill monster event: MonsterId={request.ParamId}, Count={request.Count}");
                        break;
                    
                    case 2: // 等级提升
                        AchievementHelper.TriggerLevelUp(unit, request.ParamId);
                        Log.Debug($"Triggered level up event: Level={request.ParamId}");
                        break;
                    
                    case 3: // 任务完成
                        AchievementHelper.TriggerQuestComplete(unit, request.ParamId);
                        Log.Debug($"Triggered quest complete event: QuestId={request.ParamId}");
                        break;
                    
                    case 4: // 道具收集
                        AchievementHelper.TriggerItemCollect(unit, request.ParamId, request.Count);
                        Log.Debug($"Triggered item collect event: ItemId={request.ParamId}, Count={request.Count}");
                        break;
                    
                    case 5: // 地图探索
                        AchievementHelper.TriggerMapExplore(unit, request.ParamId);
                        Log.Debug($"Triggered map explore event: MapId={request.ParamId}");
                        break;
                    
                    default:
                        Log.Error($"Unknown achievement event type: {request.EventType}");
                        response.Error = ErrorCode.ERR_Cancel;
                        response.Message = $"Unknown event type: {request.EventType}";
                        return;
                }

                // 等待一帧，让事件系统处理完毕
                await ETTask.CompletedTask;

                Log.Debug("Achievement event triggered successfully");
                
                response.Error = ErrorCode.ERR_Success;
                response.Message = "Achievement event triggered";
            }
            catch (System.Exception e)
            {
                Log.Error($"Failed to trigger achievement event: {e}");
                response.Error = ErrorCode.ERR_Cancel;
                response.Message = $"Failed to trigger event: {e.Message}";
            }
        }
    }
}