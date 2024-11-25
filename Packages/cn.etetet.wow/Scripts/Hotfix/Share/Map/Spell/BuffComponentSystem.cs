using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(BuffComponent))]
    public static partial class BuffComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BuffComponent self)
        {

        }
        
        [EntitySystem]
        private static void Deserialize(this BuffComponent self)
        {
            foreach (Buff buff in self.Children.Values)
            {
                foreach (BuffFlags flag in buff.GetConfig().Flags)
                {
                    self.flagBuffs.Add((int)flag, buff);
                }
            }
        }

        public static Buff CreateBuff(this BuffComponent self, int buffConfigId)
        {
            Buff buff = self.AddChild<Buff, int>(buffConfigId);
            return buff;
        }

        public static void RemoveBuff(this BuffComponent self, long buffId)
        {
            Buff buff = self.GetChild<Buff>(buffId);
            foreach (BuffFlags flag in buff.GetConfig().Flags)
            {
                self.flagBuffs.Remove((int)flag, buff);
            }
            self.RemoveChild(buffId);
        }

        public static List<EntityRef<Buff>> GetByFlag(this BuffComponent self, BuffFlags flag)
        {
            self.flagBuffs.TryGetValue((int)flag, out var list);
            return list;
        }
    }
}