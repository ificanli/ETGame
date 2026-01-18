using System;
using System.Collections.Generic;
using System.Linq;

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
                if (buff == null)
                {
                    continue;
                }
                foreach (BuffFlags flag in buff.GetConfig().Flags)
                {
                    self.flagBuffs.Add((int)flag, buff);
                }
            }
        }

        public static Buff CreateBuff(this BuffComponent self, long buffId, int buffConfigId, long casterId)
        {
            try
            {
                Buff buff = self.AddChildWithId<Buff, int>(buffId, buffConfigId);
                buff.Caster = casterId;
                buff.CreateTime = TimeInfo.Instance.ServerNow();
                BuffConfig buffConfig = buff.GetConfig();
                buff.Stack = buffConfig.Stack;
                foreach (BuffFlags buffFlag in buffConfig.Flags)
                {
                    self.flagBuffs.Add((int)buffFlag, buff);
                }

                foreach (EffectNode effectNode in buffConfig.Effects)
                {
                    self.effectBuffs.Add(effectNode.GetType(), buff);
                }
            
                self.configIdBuffs.Add(buffConfigId, buff);
            
                if (buffConfig.TickTime > 0)
                {
                    buff.TickTime = buffConfig.TickTime;
                }
                if (buffConfig.Duration >= 0)
                {
                    buff.ExpireTime = TimeInfo.Instance.ServerNow() + buffConfig.Duration;
                }
                else
                {
                    buff.ExpireTime = long.MaxValue;
                }
                return buff;
            }
            catch (Exception e)
            {
                throw new Exception($"create buff fail: {buffId} {buffConfigId} {casterId}", e);
            }
        }

        public static void RemoveBuffByConfigId(this BuffComponent self, int configId)
        {
            self.configIdBuffs.Remove(configId, out var buffs);

            if (buffs == null)
            {
                return;
            }
            
            foreach (Buff buff in buffs)
            {
                if (buff == null)
                {
                    continue;
                }
                self.RemoveBuff(buff);
            }
        }

        public static void RemoveBuff(this BuffComponent self, Buff buff)
        {
            BuffConfig buffConfig = buff.GetConfig();
            foreach (BuffFlags flag in buffConfig.Flags)
            {
                self.flagBuffs.Remove((int)flag, buff);
            }
            
            foreach (EffectNode effectNode in buffConfig.Effects)
            {
                self.effectBuffs.Remove(effectNode.GetType(), buff);
            }

            self.configIdBuffs.Remove(buffConfig.Id, buff);
            
            Log.Debug($"remove buff: {buff.Id} {buff.ConfigId}");
            // BuffData靠GC回收，方便传递数据
            buff.RemoveComponent<BuffData>(false);
            self.RemoveChild(buff.Id);
        }
        
        public static void RemoveBuff(this BuffComponent self, long buffId)
        {
            Buff buff = self.GetChild<Buff>(buffId);
            self.RemoveBuff(buff);
        }

        public static bool HasBuff(this BuffComponent self, int configId)
        {
            var buffs = self.GetByConfigId(configId);
            if (buffs == null)
            {
                return false;
            }
            return buffs.Count > 0;
        }

        public static HashSet<EntityRef<Buff>> GetByFlag(this BuffComponent self, BuffFlags flag)
        {
            self.flagBuffs.TryGetValue((int)flag, out var set);
            return set;
        }
        
        public static HashSet<EntityRef<Buff>> GetByConfigId(this BuffComponent self, int configId)
        {
            self.configIdBuffs.TryGetValue(configId, out var set);
            return set;
        }
        
        public static Buff GetOneByConfigId(this BuffComponent self, int configId)
        {
            self.configIdBuffs.TryGetValue(configId, out var set);
            return set?.FirstOrDefault();
        }
        
        public static void RemoveBuffFlag(this BuffComponent self, BuffFlags flag)
        {
            if (!self.flagBuffs.Remove((int)flag, out var set))
            {
                return;
            }
            foreach (Buff buff in set)
            {
                self.RemoveBuff(buff);
            }
        }
        
        public static void GetByEffectType<T>(this BuffComponent self, List<Buff> buffs) where T: EffectNode
        {
            if (!self.effectBuffs.TryGetValue(typeof(T), out var set))
            {
                return;
            }
            foreach (var buffRef in set)
            {
                buffs.Add(buffRef);
            }
        }
    }
}