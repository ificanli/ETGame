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
            TimerComponent timerComponent = root.TimerComponent;
            timerComponent.Remove(ref self.TimeoutTimer);
        }
        
        [EntitySystem]
        private static void Awake(this Buff self, int configId)
        {
            self.ConfigId = configId;
        }
        
        public static Buff GetParentBuff(this Buff self)
        {
            BuffData parentData = self.GetBuffData().ParentData;
            if (parentData == null)
            {
                return null;
            }
            return parentData.GetParent<Buff>();
        }
        
        public static BuffData GetBuffData(this Buff buff)
        {
            BuffData buffData = buff.BuffData;
            return buffData ?? buff.AddComponent<BuffData>();
        }

        public static SpellTargetComponent GetOrAddSpellTargetComponent(this Buff buff)
        {
            BuffData buffData = buff.GetBuffData();
            return buffData.GetComponent<SpellTargetComponent>() ?? buffData.AddComponent<SpellTargetComponent>();
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
            if (self == null || self.IsDisposed || self.Caster == 0)
            {
                return null;
            }

            Scene scene = self.Scene();
            if (scene == null)
            {
                return null;
            }

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return null;
            }

            return unitComponent.Get(self.Caster);
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
