namespace ET.Server
{
    public enum QuestStatus
    {
        InProgress,
        Finished,
    }
    
    [ChildOf(typeof(QuestComponent))]
    public class Quest: Entity, IAwake<int>
    {
        public int ConfigId;
        
        public QuestStatus Status;
    }
}