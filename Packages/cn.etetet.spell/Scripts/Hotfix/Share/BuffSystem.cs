namespace ET
{
    [EntitySystemOf(typeof(Buff))]
    public static partial class BuffSystem
    {
        [EntitySystem]
        private static void Destroy(this Buff self)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }
            TimerComponent timerComponent = root.GetComponent<TimerComponent>();
            timerComponent.Remove(ref self.TimeoutTimer);
        }
        
        [EntitySystem]
        private static void Awake(this Buff self, int configId)
        {
            self.ConfigId = configId;
        }

        public static BuffConfig GetConfig(this Buff self)
        {
            return BuffConfigCategory.Instance.Get(self.ConfigId);
        }

        public static SpellConfig GetSpellConfig(this Buff self)
        {
            return SpellConfigCategory.Instance.Get(self.ConfigId / 10 * 10 - 100000);
        }

        public static int GetSpellConfigId(this Buff self)
        {
            return self.ConfigId / 10 * 10 - 100000;
        }

        public static Unit GetCaster(this Buff self)
        {
            return self.Scene().GetComponent<UnitComponent>().Get(self.Caster);
        }

        public static Unit GetOwner(this Buff self)
        {
            return self.Parent.GetParent<Unit>();
        }

        public static bool IsExpired(this Buff self)
        {
            return self.ExpireTime <= TimeInfo.Instance.ServerNow() + 1;
        }
    }
}