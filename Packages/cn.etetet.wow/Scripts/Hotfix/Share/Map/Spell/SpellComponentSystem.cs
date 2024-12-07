namespace ET
{
    [EntitySystemOf(typeof(SpellComponent))]
    public static partial class SpellComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpellComponent self)
        {

        }

        public static bool CheckCD(this SpellComponent self, SpellConfig spellConfig)
        {
            long timeNow = TimeInfo.Instance.FrameTime;
            if (self.CDTime + 2000 > timeNow)
            {
                return false;
            }

            if (self.SpellCD.TryGetValue(spellConfig.Id, out long cdTime))
            {
                if (cdTime + spellConfig.CD > timeNow)
                {
                    return false;
                }
            }
            return true;
        }

        public static void UpdateCD(this SpellComponent self, int spellConfigId)
        {
            long timeNow = TimeInfo.Instance.FrameTime;
            self.CDTime = timeNow;
            self.SpellCD[spellConfigId] = timeNow;
        }
    }
}