namespace ET.Server
{
    [Module(ModuleName.Quest)]
    public class QuestObjectiveAttribute: BaseAttribute
    {
        public QuestObjectiveType QuestObjectiveType;
        
        public QuestObjectiveAttribute(QuestObjectiveType questObjectiveType)
        {
            this.QuestObjectiveType = questObjectiveType;
        }
    }
}