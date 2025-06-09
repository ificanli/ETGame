namespace ET.Server
{
    [EntitySystemOf(typeof(QuestComponent))]
    public static partial class QuestComponentSystem
    {
        [EntitySystem]
        private static void Destroy(this QuestComponent self)
        {

        }
        [EntitySystem]
        private static void Awake(this QuestComponent self)
        {
            
        }
        
        public static void AddQuest(this QuestComponent self, int configId)
        {
            self.AddChild<Quest, int>(configId);
            
        }
    }
}