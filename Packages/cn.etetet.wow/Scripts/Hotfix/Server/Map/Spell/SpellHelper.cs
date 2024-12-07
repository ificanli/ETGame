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

                // 检查消耗的东西是否足够
                int costCheckRet = CostDispatcher.Instance.Handle(unit, spellConfig);
                if (costCheckRet != 0)
                {
                    ErrorHelper.MapError(unit, costCheckRet);
                    return costCheckRet;
                }
            }
#endregion
            
            // add
            Buff buff = BuffHelper.CreateBuffWithoutInit(unit, unit.Id, IdGenerater.Instance.GenerateId(), spellConfig.BuffId);
            int castRet = SpellCasting(unit, buff, spellConfig, parent);
            if (castRet != 0)
            {
                BuffHelper.RemoveBuff(buff, BuffFlags.NotFoundTargetRemove);
            }

            return castRet;
        }

        private static int SpellCasting(Unit unit, Buff buff, SpellConfig spellConfig, Buff parent)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            buff.Caster = unit.Id;

            if (parent == null)
            {
                spellComponent.UpdateCD(spellConfig.Id);

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
                int ret = BTDispatcher.Instance.Handle(spellConfig.TargetSelector, env);
                if (ret != 0)
                {
                    ErrorHelper.MapError(unit, ret);
                    return ret;
                }
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