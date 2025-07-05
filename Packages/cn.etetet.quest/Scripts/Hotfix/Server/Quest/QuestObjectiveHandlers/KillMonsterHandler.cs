namespace ET.Server
{
    [Module(ModuleName.Quest)]
    [QuestObjective(QuestObjectiveType.KillMonster)]
    public class KillMonsterHandler : IQuestObjectiveHandler
    {
        public void Init(QuestObjective questObjective)
        {
            QuestObjectiveConfig config = questObjective.GetConfig();
            questObjective.TargetCount = config.NeedCount;
            questObjective.Progress = 0;
            questObjective.IsCompleted = false;
            questObjective.ObjectiveType = QuestObjectiveType.KillMonster;
            
            // 从配置表获取目标怪物ID等参数
            if (config.Id > 0)
            {
                questObjective.Params.Add(config.Id); // 假设配置表中Id字段就是目标怪物ID
            }
        }

        public bool IsFinished()
        {
            // 这个方法需要通过QuestObjective实例调用
            return false; // 占位实现
        }

        public void Process()
        {
            // 击杀怪物的具体处理逻辑
            // 实际的击杀计数应该在怪物死亡事件中触发
        }
    }
}