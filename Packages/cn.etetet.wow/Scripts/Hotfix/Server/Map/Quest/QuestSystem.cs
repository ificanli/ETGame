namespace ET.Server
{
    [EntitySystemOf(typeof(Quest))]
    public static partial class QuestSystem
    {
        [EntitySystem]
        private static void Awake(this Quest self, int configId)
        {
            QuestConfig questConfig = QuestConfigCategory.Instance.Get(configId);
        }
        
    }
}