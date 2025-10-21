namespace ET.Server
{
    public interface IQuestObjectiveHandler
    {
        void Process(QuestObjective questObjective, int value);
    }
}