namespace ET.Server
{
    [EntitySystemOf(typeof(RogueChoiceQualityComponent))]
    public static partial class RogueChoiceQualityComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueChoiceQualityComponent self)
        {
            self.ChoiceQualities.Clear();
        }
    }
}
