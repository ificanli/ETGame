using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ET.Server
{
    public static class BuffHelper
    {
        [Invoke(TimerInvokeType.BuffTimeoutTimer)]
        public class BuffTimeoutTimer: ATimer<Buff>
        {
            protected override void Run(Buff self)
            {
                BuffHelper.RemoveBuff(self, BuffFlags.TimeoutRemove);
            }
        }

        public static Buff CreateBuffWithoutInit(Unit unit, long casterId, long buffId, int buffConfigId, Buff parent)
        {
            Buff buff = unit.GetComponent<BuffComponent>().CreateBuff(buffId, buffConfigId, casterId);
            BuffConfig buffConfig = buff.GetConfig();
            
            if (parent != null)
            {
                buff.GetBuffData().ParentData = parent.GetBuffData();
                
                if (buffConfig.Flags.Contains(BuffFlags.ParentRemoveRemove))
                {
                    BuffChildrenComponent buffChildrenComponent =
                            parent.GetComponent<BuffChildrenComponent>() ?? parent.AddComponent<BuffChildrenComponent>();
                    buffChildrenComponent.Buffs.Add(buff);
                }
            }
            
            Log.Debug($"Buff Create: {buffConfigId}");
            return buff;
        }

        public static Buff CreateBuff(Unit unit, long casterId, long buffId, int buffConfigId, Buff parent)
        {
            Buff buff = unit.GetComponent<BuffComponent>().CreateBuff(buffId, buffConfigId, casterId);

            if (parent != null)
            {
                buff.GetBuffData().ParentData = parent.GetBuffData();
                BuffConfig buffConfig = buff.GetConfig();
                if (buffConfig.Flags.Contains(BuffFlags.ParentRemoveRemove))
                {
                    BuffChildrenComponent buffChildrenComponent =
                            parent.GetComponent<BuffChildrenComponent>() ?? parent.AddComponent<BuffChildrenComponent>();
                    buffChildrenComponent.Buffs.Add(buff);
                }
            }

            Log.Debug($"Buff Create: {buffConfigId}");
            return InitBuff(buff);
        }
        
        public static Buff InitBuff(Buff buff)
        {
            Unit unit = buff.Parent.GetParent<Unit>();
            BuffComponent buffComponent = buff.GetParent<BuffComponent>();
            BuffConfig buffConfig = buff.GetConfig();

            // 处理叠加规则
            if (buffConfig.OverLayRuleType != OverLayRuleType.None)
            {
                var oldBuffs = buffComponent.GetByConfigId(buffConfig.Id);
                if (oldBuffs != null && oldBuffs.Count > 0)
                {
                    switch (buffConfig.OverLayRuleType)
                    {
                        case OverLayRuleType.AddStack:
                        {
                            Buff oldBuff = oldBuffs.First();
                            UpdateStack(oldBuff, oldBuff.Stack + buffConfig.Stack);
                            return oldBuff;
                        }
                        case OverLayRuleType.AddTime:
                        {
                            Buff oldBuff = oldBuffs.First();
                            long expireTime = TimeInfo.Instance.ServerNow() + buffConfig.Duration;
                            if (expireTime < oldBuff.ExpireTime)
                            {
                                return oldBuff;
                            }
                            UpdateExpireTime(oldBuff, expireTime);
                            return oldBuff;
                        }
                        case OverLayRuleType.Replace:
                        {
                            foreach (Buff oldBuff in oldBuffs.ToArray())
                            {
                                if (oldBuff.Id != buff.Id)
                                {
                                    RemoveBuff(oldBuff, BuffFlags.SameConfigIdReplaceRemove);
                                }
                            }
                            break;
                        }
                        case OverLayRuleType.None:
                        {
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            
            TimerComponent timerComponent = buff.Root().GetComponent<TimerComponent>();

            RefreshTickComponent(buff);
            
            if (buff.ExpireTime > 0 && buff.ExpireTime < long.MaxValue)
            {
                if (buff.TickTime > 0)
                {
                    buff.ExpireTime += 1;
                }
                buff.TimeoutTimer = timerComponent.NewOnceTimer(buff.ExpireTime, TimerInvokeType.BuffTimeoutTimer, buff);
            }
            
            EffectServerBuffAdd effect = buff.GetConfig().GetEffect<EffectServerBuffAdd>();
            if (effect != null)
            {
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, buff.Parent.GetParent<Unit>());
                env.AddEntity(effect.Caster, buff.GetCaster());
                BTHelper.RunTree(effect, env);
            }
            
            
            M2C_BuffAdd m2CBuffAdd = M2C_BuffAdd.Create();
            m2CBuffAdd.BuffId = buff.Id;
            m2CBuffAdd.BuffConfigId = buffConfig.Id;
            m2CBuffAdd.UnitId = unit.Id;
            m2CBuffAdd.CreateTime = buff.CreateTime;
            m2CBuffAdd.TickTime = buff.TickTime;
            m2CBuffAdd.ExpireTime = buff.ExpireTime;
            m2CBuffAdd.Stack = buff.Stack;
            m2CBuffAdd.CasterId = buff.Caster;
            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
            if (spellTargetComponent != null)
            {
                m2CBuffAdd.SpellTarget = SpellTarget.Create();
                foreach (long targetId in spellTargetComponent.Units)
                {
                    m2CBuffAdd.SpellTarget.TargetUnitId.Add(targetId);
                }
                m2CBuffAdd.SpellTarget.TargetPosition = spellTargetComponent.Position;
            }
            
            MapMessageHelper.NoticeClient(unit, m2CBuffAdd, buffConfig.NoticeType);

            return buff;
        }

        private static void RefreshTickComponent(Buff buff)
        {
            buff.RemoveComponent<BuffTickComponent>();

            if (buff.TickTime > 0)
            {
                buff.AddComponent<BuffTickComponent>();
            }
        }
        
        public static void UpdateExpireTime(Buff buff, long expireTime)
        {
            buff.ExpireTime = expireTime;
            
            if (buff.ExpireTime < TimeInfo.Instance.ServerNow())
            {
                BuffHelper.RemoveBuff(buff, BuffFlags.TimeoutRemove);
                return;
            }
            
            
            long timeoutTimer = buff.TimeoutTimer;
            TimerComponent timerComponent = buff.Root().GetComponent<TimerComponent>();
            timerComponent.Remove(ref timeoutTimer);
            
            if (buff.ExpireTime > 0)
            {
                buff.TimeoutTimer = timerComponent.NewOnceTimer(buff.ExpireTime, TimerInvokeType.BuffTimeoutTimer, buff);
            }

            Unit unit = buff.Parent.GetParent<Unit>();
            M2C_BuffUpdate m2CBuffUpdate = M2C_BuffUpdate.Create();
            m2CBuffUpdate.BuffId = buff.Id;
            m2CBuffUpdate.UnitId = unit.Id;
            m2CBuffUpdate.Stack = -1;
            m2CBuffUpdate.ExpireTime = expireTime;
            m2CBuffUpdate.TickTime = -1;
            MapMessageHelper.NoticeClient(unit, m2CBuffUpdate, buff.GetConfig().NoticeType);
        }
        
        public static void UpdateTickTime(Buff buff, int tickTime)
        {
            int oldTickTime = buff.TickTime;
            buff.TickTime = tickTime;

            if (oldTickTime <= 0 && tickTime > 0 && buff.ExpireTime > 0 && buff.ExpireTime < long.MaxValue)
            {
                UpdateExpireTime(buff, buff.ExpireTime + 1);
            }

            RefreshTickComponent(buff);
             
            Unit unit = buff.Parent.GetParent<Unit>();
            M2C_BuffUpdate m2CBuffUpdate = M2C_BuffUpdate.Create();
            m2CBuffUpdate.BuffId = buff.Id;
            m2CBuffUpdate.UnitId = unit.Id;
            m2CBuffUpdate.Stack = -1;
            m2CBuffUpdate.ExpireTime = -1;
            m2CBuffUpdate.TickTime = tickTime;
            MapMessageHelper.NoticeClient(unit, m2CBuffUpdate, buff.GetConfig().NoticeType);
        }
        
        public static void UpdateStack(Buff buff, int stack)
        {
            if (buff.Stack == stack)
            {
                return;
            }

            BuffConfig buffConfig = buff.GetConfig();
            int maxStack = buffConfig.MaxStack;
            if (stack > maxStack)
            {
                stack = maxStack;
            }
            
            if (stack < 0)
            {
                stack = 0;
            }
            
            buff.Stack = stack;
            
            if (buff.Stack == 0)
            {
                RemoveBuff(buff, BuffFlags.StackRemove);
                return;
            }
            Unit unit = buff.Parent.GetParent<Unit>();
            M2C_BuffUpdate m2CBuffUpdate = M2C_BuffUpdate.Create();
            m2CBuffUpdate.BuffId = buff.Id;
            m2CBuffUpdate.UnitId = unit.Id;
            m2CBuffUpdate.Stack = buff.Stack;
            m2CBuffUpdate.ExpireTime = -1;
            m2CBuffUpdate.TickTime = -1;
            MapMessageHelper.NoticeClient(unit, m2CBuffUpdate, buffConfig.NoticeType);
        }

        public static void RemoveBuff(Buff buff, BuffFlags removeType)
        {
            Unit unit = buff.Parent?.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }
            
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            
            M2C_BuffRemove m2CBuffRemove = M2C_BuffRemove.Create();
            m2CBuffRemove.UnitId = unit.Id;
            m2CBuffRemove.BuffId = buff.Id;
            m2CBuffRemove.BuffConfigId = buff.ConfigId;
            m2CBuffRemove.RemoveType = (int)removeType;

            BuffRemoveTypeComponent buffRemoveTypeComponent = buff.GetComponent<BuffRemoveTypeComponent>() ?? buff.AddComponent<BuffRemoveTypeComponent>();
            buffRemoveTypeComponent.BuffRemoveType = removeType;
            
            EffectServerBuffRemove effect = buff.GetConfig().GetEffect<EffectServerBuffRemove>();
            if (effect != null)
            {
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, unit);
                env.AddEntity(effect.Caster, buff.GetCaster());
                BTHelper.RunTree(effect, env);
            }

            buffComponent.RemoveBuff(buff);
            
            MapMessageHelper.NoticeClient(unit, m2CBuffRemove, buff.GetConfig().NoticeType);
        }
        
        public static void RemoveBuff(Unit unit, long id, BuffFlags removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            Buff buff = buffComponent.GetChild<Buff>(id);
            
            if (buff == null)
            {
                return;
            }
            
            RemoveBuff(buff, removeType);
        }

        public static void RemoveBuffByConfigId(Unit unit, int configId, BuffFlags removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            HashSet<EntityRef<Buff>> buffs = buffComponent.GetByConfigId(configId);
            if (buffs == null)
            {
                return;
            }
            foreach (EntityRef<Buff> buff in buffs.ToArray())
            {
                RemoveBuff(buff, removeType);
            }
        }
        
        public static void RemoveBuffFlag(Unit unit, BuffFlags flag)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                return;
            }
            HashSet<EntityRef<Buff>> buffs = buffComponent.GetByFlag(flag);
            if (buffs == null)
            {
                return;
            }
            foreach (EntityRef<Buff> buff in buffs.ToArray())
            {
                RemoveBuff(buff, flag);
            }
        }

        public static void RemoveBuffsByFlag(Unit unit, BuffFlags flag, BuffFlags removeType)
        {
            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                return;
            }
            HashSet<EntityRef<Buff>> buffs = buffComponent.GetByFlag(flag);
            if (buffs == null)
            {
                return;
            }
            foreach (EntityRef<Buff> buff in buffs.ToArray())
            {
                RemoveBuff(buff, removeType);
            }
        }
    }
}
