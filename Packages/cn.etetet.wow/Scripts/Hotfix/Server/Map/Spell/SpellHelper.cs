using System;
using Unity.Mathematics;

namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
    [Module(ModuleName.Spell)]
    public static class SpellHelper
    {
        public static int Cast(Unit unit, int spellConfigId, Buff parent = null)
        {
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
#region Check
            // check
            {
                // 子技能不检查CD
                if (parent == null)
                {
                    if (!spellComponent.CheckCD(spellConfig))
                    {
                        ErrorHelper.MapError(unit, TextConstDefine.SpellCast_SpellInCD);
                        return TextConstDefine.SpellCast_SpellInCD;
                    }
                }
            }
#endregion
            
            // add
            Buff buff = BuffHelper.CreateBuffWithoutInit(unit, unit.Id, IdGenerater.Instance.GenerateId(), spellConfig.BuffId);
            int castRet = SpellCasting(unit, buff, spellConfig, parent);
            if (castRet != 0)
            {
                unit.GetComponent<BuffComponent>().RemoveBuff(buff);
            }

            return castRet;
        }

        private static int SpellCasting(Unit unit, Buff buff, SpellConfig spellConfig, Buff parent)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            buff.Caster = unit.Id;

            if (parent == null)
            {
                // 这里简单做一下打断当前技能, 设置新的当前技能
                Buff currentBuff = spellComponent.Current;
                if (currentBuff != null)
                {
                    BuffHelper.RemoveBuff(currentBuff, BuffFlags.NewSpellInterrupt);
                }

                spellComponent.Current = buff;
            }

            // 选择目标
            {
                buff.AddComponent<SpellTargetComponent>();
                TargetSelector targetSelector = spellConfig.TargetSelector;
                if (targetSelector != null)
                {
                    using BTEnv env = BTEnv.Create(buff.Scene());
                    env.AddEntity(targetSelector.Buff, buff);
                    env.AddEntity(targetSelector.Caster, buff.GetCaster());
                    env.AddEntity(targetSelector.Owner, buff.GetOwner());
                    int ret = BTDispatcher.Instance.Handle(spellConfig.TargetSelector, env);
                    if (ret != 0)
                    {
                        ErrorHelper.MapError(unit, ret);
                        return ret;
                    }
                }
            }

            CostNode costNode = spellConfig.Cost;
            if (costNode != null)
            {
                // 先检查消耗的东西是否足够
                {
                    using BTEnv env = BTEnv.Create(buff.Scene());
                    env.AddEntity(costNode.Buff, buff);
                    env.AddEntity(costNode.Caster, buff.GetCaster());
                    env.AddStruct(costNode.Check, true);
                    int ret = BTDispatcher.Instance.Handle(costNode, env);
                    if (ret != 0)
                    {
                        ErrorHelper.MapError(unit, ret);
                        return ret;
                    }
                }

                // 消耗东西
                {
                    using BTEnv env = BTEnv.Create(buff.Scene());
                    env.AddEntity(costNode.Buff, buff);
                    env.AddEntity(costNode.Caster, buff.GetCaster());
                    env.AddStruct(costNode.Check, false);
                    int ret = BTDispatcher.Instance.Handle(costNode, env);
                    if (ret != 0)
                    {
                        ErrorHelper.MapError(unit, ret);
                        return ret;
                    }
                }
            }
            // 主技能更新CD
            if (parent == null)
            {
                spellComponent.UpdateCD(spellConfig.Id, TimeInfo.Instance.ServerFrameTime());
                if (unit.UnitType.IsSame(UnitType.Player))
                {
                    M2C_UpdateCD m2CUpdateCd = M2C_UpdateCD.Create();
                    m2CUpdateCd.UnitId = unit.Id;
                    m2CUpdateCd.SpellConfigId = spellConfig.Id;
                    m2CUpdateCd.Time = TimeInfo.Instance.ServerFrameTime();
                    MapMessageHelper.NoticeClient(unit, m2CUpdateCd, NoticeType.Self);
                }
            }
            
            
            BuffHelper.InitBuff(buff, parent);
            
            if (buff.ExpireTime < TimeInfo.Instance.ServerNow())
            {
                BuffHelper.RemoveBuff(buff, BuffFlags.NoDurationRemove);
            }
            
            return 0;
        }

        public static void Stop(Unit unit, BuffFlags buffFlags)
        {
            Buff buff = unit.GetComponent<SpellComponent>().Current;
            if (buff == null)
            {
                return;
            }
            BuffHelper.RemoveBuff(buff, buffFlags);
        }
    }
}