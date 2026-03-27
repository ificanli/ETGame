namespace ET
{
    [EntitySystemOf(typeof(UnitDisplayLevelComponent))]
    public static partial class UnitDisplayLevelComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UnitDisplayLevelComponent self, int level)
        {
            self.Level = level > 0 ? level : 1;
        }

        public static void SetLevel(this UnitDisplayLevelComponent self, int level)
        {
            self.Level = level > 0 ? level : 1;
        }
    }
}
