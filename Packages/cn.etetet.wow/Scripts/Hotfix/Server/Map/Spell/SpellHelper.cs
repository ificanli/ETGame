using System;

namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
    public static class SpellHelper
    {
        public static async ETTask Cast(Unit unit, int spellConfigId)
        {
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();

#region Spell Check
            // check
            {
                if (!spellComponent.CheckCD(spellConfig))
                {
                    ErrorHelper.MapError(unit, TextConstDefine.SpellCast_SpellInCD);
                    return;
                }

                // 检查消耗的东西是否足够
                int costCheckRet = CostDispatcher.Instance.Handle(unit, spellConfig);
                if (costCheckRet != 0)
                {
                    ErrorHelper.MapError(unit, costCheckRet);
                    return ;
                }
            }
#endregion
            
#region Spell Add

            // add
            Spell spell = spellComponent.CreateSpell(IdGenerater.Instance.GenerateId(), spellConfig.Id);
            spell.Caster = unit.Id;

            // 子技能没有CD,子技能不设置Current
            if (!spell.IsSubSpell())
            {
                spellComponent.UpdateCD(spellConfig.Id);

                // 设置当前技能
                spellComponent.Current = spell;
            }

            M2C_SpellAdd m2CSpellAdd = M2C_SpellAdd.Create();
            m2CSpellAdd.UnitId = unit.Id;
            m2CSpellAdd.SpellId = spell.Id;
            m2CSpellAdd.SpellConfigId = spellConfig.Id;
            MapMessageHelper.NoticeClient(unit, m2CSpellAdd, spellConfig.NoticeType);

            EffectHelper.RunBT<EffectServerBuffAdd>(spell);
            
#endregion

            try
            {
#region Spell Target Select
                // 选择目标
                {
                    spell.AddComponent<SpellTargetComponent>();
                    using BTEnv env = BTEnv.Create();
                    env.AddEntity(BTEvnKey.Spell, spell);
                    int ret = BTDispatcher.Instance.Handle(spell.GetConfig().TargetSelector, env);
                    if (ret != 0)
                    {
                        ErrorHelper.MapError(unit, ret);
                        return;
                    }
                }
#endregion
            
#region Spell Hit
                // hit
                {
                    if (spellConfig.HitTime > 0)
                    {
                        ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
                        spellComponent.CancellationToken = cancellationToken;
                        TimerComponent timerComponent = spell.Scene().GetComponent<TimerComponent>();
                        // 等到命中
                        await timerComponent.WaitTillAsync(spell.CreateTime + spellConfig.HitTime);
                        if (cancellationToken.IsCancel())
                        {
                            return;
                        }
                    }

                    SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
            
                    // 发送SpellHit消息, 命中，暴击等等在这里已经计算完成，SpellHit消息中应该带有命中，暴击等信息
                    M2C_SpellHit m2CSpellHit = M2C_SpellHit.Create();
                    m2CSpellHit.UnitId = unit.Id;
                    m2CSpellHit.SpellId = spell.Id;
                    m2CSpellHit.TargetPosition = spellTargetComponent.Position;
                    foreach (Unit target in spellTargetComponent.Units)
                    {
                        m2CSpellHit.TargetUnitId.Add(target.Id);
                    }
                    MapMessageHelper.NoticeClient(unit, m2CSpellHit, spellConfig.NoticeType);
    
                    // 对目标分发hitEffect
                    EffectHelper.RunBT<EffectServerSpellHit>(spell);
                }
#endregion  
                
#region Spell Finish
                // finish
                {
                    if (spell.ExpireTime > 0)
                    {
                        TimerComponent timerComponent = spell.Scene().GetComponent<TimerComponent>();
                        ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
                        spellComponent.CancellationToken = cancellationToken;
                
                        await timerComponent.WaitTillAsync(spell.CreateTime + spellConfig.Duration);
                        if (cancellationToken.IsCancel())
                        {
                            return;
                        }
                    }
                }
#endregion
            }
            finally
            {
                // 发送SpellRemove消息
                Remove(unit, spell.Id);
            }
        }
       

        public static bool IsSubSpell(this Spell spell)
        {
            return spell.ConfigId % 10 == 0;
        }
        
        public static void Interrupt(Unit unit, long spellId)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.CancellationToken?.Cancel();
            spellComponent.CancellationToken = default;
            
            Remove(unit, spellId);
        }

        public static void Remove(Unit unit, long spellId)
        {
            M2C_SpellRemove spellRemove = M2C_SpellRemove.Create();
            spellRemove.UnitId = unit.Id;
            spellRemove.SpellId = spellId;
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            Spell spell = spellComponent.GetSpell(spellId);
            spellComponent.CancellationToken = null;
            
            MapMessageHelper.NoticeClient(unit, spellRemove, spell.GetConfig().NoticeType);
            
            spellComponent.RemoveSpell(spellId);
        }
    }
}