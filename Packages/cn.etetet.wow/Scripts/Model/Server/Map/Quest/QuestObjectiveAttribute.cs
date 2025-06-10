namespace ET.Server
{
    public class QuestObjectiveAttribute: BaseAttribute
    {
        public QuestObjectiveType QuestObjectiveType;
        
        public QuestObjectiveAttribute(QuestObjectiveType questObjectiveType)
        {
            this.QuestObjectiveType = questObjectiveType;
        }
    }
}