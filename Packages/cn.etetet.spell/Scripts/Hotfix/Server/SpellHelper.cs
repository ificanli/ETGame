using System;
using Unity.Mathematics;

namespace ET.Server
{
    public static class SpellHelper
    {
        public static int Cast(Unit unit, int spellConfigId, Buff parent = null)
        {
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
#region Check
            // check
            {
                // 通用流程，子技能不检查CD
                if (SpellConfigHelper.IsMainSpell(spellConfigId))
                {
                    bool ret = spellComponent.CheckCD(spellConfig);
                    if (!ret)
                    {
                        return TextConstDefine.SpellCast_SpellInCD;
                    }
                }
            }
#endregion
            
            // add
            Buff buff = BuffHelper.CreateBuffWithoutInit(unit, unit.Id, IdGenerater.Instance.GenerateId(), spellConfig.BuffId, parent);
            int castRet = SpellCasting(unit, buff, spellConfig, parent);
            if (castRet != 0)
            {
                unit.GetComponent<BuffComponent>().RemoveBuff(buff);
            }

            return castRet;
        }
        
        /// <summary>
        /// 指定目标Unit施放技能
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="spellConfigId"></param>
        /// <param name="target"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static int Cast(Unit unit, int spellConfigId, Unit target, Buff parent = null)
        {
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
            #region Check
            // check
            {
                if (SpellConfigHelper.IsMainSpell(spellConfigId))
                {
                    bool ret = spellComponent.CheckCD(spellConfig);
                    if (!ret)
                    {
                        return TextConstDefine.SpellCast_SpellInCD;
                    }
                }
            }
            #endregion
            
            // add
            Buff buff = BuffHelper.CreateBuffWithoutInit(unit, unit.Id, IdGenerater.Instance.GenerateId(), spellConfig.BuffId, parent);
            buff.GetBuffData().AddComponent<SpellTargetComponent>().Units.Add(target.Id);
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
                SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
                // 不为null表示已经指定了目标
                if (spellTargetComponent == null)
                {
                    buff.GetBuffData().AddComponent<SpellTargetComponent>();
                    TargetSelector targetSelector = spellConfig.TargetSelector;
                    if (targetSelector != null)
                    {
                        using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                        env.AddEntity(targetSelector.Buff, buff);
                        env.AddEntity(targetSelector.Caster, buff.GetCaster());
                        env.AddEntity(targetSelector.Owner, buff.GetOwner());
                        int ret = BTHelper.RunTree(spellConfig.TargetSelector, env);
                        if (ret != 0)
                        {
                            return ret;
                        }
                    }
                }
            }

            CostNode costNode = spellConfig.Cost;
            if (costNode != null)
            {
                // 先检查消耗的东西是否足够
                {
                    using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                    env.AddEntity(costNode.Buff, buff);
                    env.AddEntity(costNode.Unit, unit);
                    env.AddStruct(costNode.Check, true);
                    int ret = BTHelper.RunTree(costNode, env);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }

                // 消耗东西
                {
                    using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                    env.AddEntity(costNode.Buff, buff);
                    env.AddEntity(costNode.Unit, unit);
                    env.AddStruct(costNode.Check, false);
                    int ret = BTHelper.RunTree(costNode, env);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }
            }
            
            // 这里不管是否检查，都会更新cd
            if (spellConfig.CD > 0)
            {
                UpdateCD(unit, spellConfig.Id, TimeInfo.Instance.ServerNow());
            }
            
            BuffHelper.InitBuff(buff);
            
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

        public static void UpdateCD(Unit unit, int spellConfigId, long time)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.UpdateCD(spellConfigId, time);

            if (unit.UnitType == UnitType.Player)
            {
                M2C_UpdateCD m2CUpdateCd = M2C_UpdateCD.Create();
                m2CUpdateCd.UnitId = unit.Id;
                m2CUpdateCd.SpellConfigId = spellConfigId;
                m2CUpdateCd.Time = time;
                MapMessageHelper.NoticeClient(unit, m2CUpdateCd, NoticeType.Self);
            }
        }
    }
}