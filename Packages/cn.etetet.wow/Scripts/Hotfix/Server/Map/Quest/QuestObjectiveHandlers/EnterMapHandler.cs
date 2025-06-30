namespace ET.Server
{
    [QuestObjective(QuestObjectiveType.EnterMap)]
    public class EnterMapHandler : IQuestObjectiveHandler
    {
        public void Init(QuestObjective questObjective)
        {
            QuestObjectiveConfig config = questObjective.GetConfig();
            questObjective.TargetCount = config.NeedCount; // 通常为1，表示进入一次即可
            questObjective.Progress = 0;
            questObjective.IsCompleted = false;
            questObjective.ObjectiveType = QuestObjectiveType.EnterMap;
            
            // 从配置表获取目标地图ID
            if (config.Id > 0)
            {
                questObjective.Params.Add(config.Id); // 假设配置表中Id字段就是目标地图ID
            }
        }

        public bool IsFinished()
        {
            // 这个方法需要通过QuestObjective实例调用
            return false; // 占位实现
        }

        public void Process()
        {
            // 进入地图任务通常是一次性的，不需要持续处理
            // 实际的触发应该在玩家进入地图时通过事件系统调用
        }
    }
}