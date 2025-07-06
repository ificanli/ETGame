namespace ET.Server
{
    [Module(ModuleName.Quest)]
    [QuestObjective(QuestObjectiveType.Level)]
    public class LevelHandler : IQuestObjectiveHandler
    {
        public void Init(QuestObjective questObjective)
        {
            QuestObjectiveConfig config = questObjective.GetConfig();
            questObjective.TargetCount = config.NeedCount; // 目标等级
            questObjective.Progress = 0;
            questObjective.IsCompleted = false;
            questObjective.ObjectiveType = QuestObjectiveType.Level;
        }

        public bool IsFinished()
        {
            // 这个方法需要通过QuestObjective实例调用
            return false; // 占位实现
        }

        public void Process()
        {
            // 等级任务的处理逻辑
            // 实际的等级检查应该在升级事件中触发
        }
    }
}