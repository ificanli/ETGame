namespace ET.Server
{
    public interface IQuestObjectiveHandler
    {
        void Init(QuestObjective questObjective);
        bool IsFinished();
        void Process();
    }
}