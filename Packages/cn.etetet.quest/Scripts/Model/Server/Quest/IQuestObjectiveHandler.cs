namespace ET.Server
{
    [Module(ModuleName.Quest)]
    public interface IQuestObjectiveHandler
    {
        void Init(QuestObjective questObjective);
        bool IsFinished();
        void Process();
    }
}