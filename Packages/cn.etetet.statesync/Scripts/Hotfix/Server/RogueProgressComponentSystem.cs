namespace ET.Server
{
    [EntitySystemOf(typeof(RogueProgressComponent))]
    public static partial class RogueProgressComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueProgressComponent self)
        {
            self.Level = 1;
            self.CurrentExp = 0;
            self.NeedExp = int.MaxValue;
            self.ChoiceSerial = 0;
            self.ChoicePending = false;
            self.PendingOptionIds.Clear();
            self.PendingChoiceLevels.Clear();

            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory == null)
            {
                Log.Warning("[Rogue] RogueRuntimeConfigCategory is null on awake.");
                return;
            }

            if (!configCategory.TryGetStartLevel(out int startLevel, out RogueLevelConfig levelConfig))
            {
                Log.Warning("[Rogue] start level config missing.");
                return;
            }

            self.Level = startLevel;
            self.NeedExp = NormalizeNeedExp(levelConfig.NeedExp);
        }

        public static int NormalizeNeedExp(int needExp)
        {
            return needExp > 0 ? needExp : int.MaxValue;
        }
    }
}
