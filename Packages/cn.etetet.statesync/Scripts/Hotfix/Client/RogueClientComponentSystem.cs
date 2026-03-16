namespace ET.Client
{
    [EntitySystemOf(typeof(RogueClientComponent))]
    public static partial class RogueClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueClientComponent self)
        {
            self.ResetRuntime();
        }

        public static void ResetRuntime(this RogueClientComponent self)
        {
            self.Level = 1;
            self.CurrentExp = 0;
            self.NeedExp = 0;
            self.CurrentGold = 0;
            self.ChoiceSerial = 0;
            self.ChoicePopupPending = false;
            self.ChoicePopupOpening = false;
            self.ChoiceOptions.Clear();
        }

        public static void SetPopupShown(this RogueClientComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.ChoicePopupPending = false;
            self.ChoicePopupOpening = false;
        }

        public static void SetPopupOpening(this RogueClientComponent self, bool opening)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.ChoicePopupOpening = opening;
        }
    }
}
