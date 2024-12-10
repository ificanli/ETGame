using System;

namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
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
                using BTEnv env = BTEnv.Create(buff.Scene());
                env.AddEntity(BTEvnKey.Buff, buff);
                env.AddEntity(BTEvnKey.Caster, buff.GetCaster());
                env.AddEntity(BTEvnKey.Owner, buff.GetOwner());
                int ret = BTDispatcher.Instance.Handle(spellConfig.TargetSelector, env);
                if (ret != 0)
                {
                    ErrorHelper.MapError(unit, ret);
                    return ret;
                }
            }

            {
                // 检查消耗的东西是否足够
                int costCheckRet = CostDispatcher.Instance.Handle(unit, spellConfig);
                if (costCheckRet != 0)
                {
                    ErrorHelper.MapError(unit, costCheckRet);
                    return costCheckRet;
                }
                
                // 消耗东西
                using BTEnv env = BTEnv.Create(buff.Scene());
                env.AddEntity(BTEvnKey.Buff, buff);
                env.AddEntity(BTEvnKey.Caster, buff.GetCaster());
                foreach (CostNode costNode in spellConfig.Cost)
                {
                    int ret = BTDispatcher.Instance.Handle(costNode, env);
                    if (ret == 0)
                    {
                        continue;
                    }
                    ErrorHelper.MapError(unit, ret);
                    return ret;
                }
            }
            // 主技能更新CD
            if (parent == null)
            {
                spellComponent.UpdateCD(spellConfig.Id);
            }
            
            
            BuffHelper.InitBuff(buff, parent);
            
            if (buff.ExpireTime < TimeInfo.Instance.ServerNow())
            {
                BuffHelper.RemoveBuff(buff, BuffFlags.NoDurationRemove);
            }
            
            return 0;
        }
    }
}