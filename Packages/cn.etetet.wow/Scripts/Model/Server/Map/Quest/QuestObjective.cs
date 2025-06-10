namespace ET.Server
{
    [ChildOf(typeof(Quest))]
    public class QuestObjective: Entity, IAwake<int>, IDestroy
    {
        public int ConfigId;
    }
}