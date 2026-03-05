namespace ET.Client
{
    [EntitySystemOf(typeof(RogueClientComponent))]
    public static partial class RogueClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueClientComponent self)
        {
            self.Level = 1;
            self.CurrentExp = 0;
            self.NeedExp = 0;
            self.ChoiceSerial = 0;
            self.ChoiceOptions.Clear();
        }
    }
}
