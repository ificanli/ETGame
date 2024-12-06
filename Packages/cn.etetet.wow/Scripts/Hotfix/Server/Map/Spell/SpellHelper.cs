using System;

namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
    public static class SpellHelper
    {
        public static bool IsRootSpell(this SpellConfig spellConfig)
        {
            return spellConfig.Id % 10 == 0;
        }
        
        public static async ETTask Cast(Unit unit, int spellConfigId)
        {
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
#region Spell Check
            // check
            {
                if (spellConfig.IsRootSpell())
                {
                    if (!spellComponent.CheckCD(spellConfig))
                    {
                        ErrorHelper.MapError(unit, TextConstDefine.SpellCast_SpellInCD);
                        return;
                    }
                }

                // 检查消耗的东西是否足够
                int costCheckRet = CostDispatcher.Instance.Handle(unit, spellConfig);
                if (costCheckRet != 0)
                {
                    ErrorHelper.MapError(unit, costCheckRet);
                    return;
                }
            }
#endregion
            
#region Spell Add
            // add
            Spell spell = spellComponent.CreateSpell(IdGenerater.Instance.GenerateId(), spellConfig.Id);
            spell.SpellStatus = SpellStatus.Create;
            spell.Caster = unit.Id;
            spell.CancellationToken = cancellationToken;

            // 子技能没有CD,子技能不设置Current
            if (spellConfig.IsRootSpell())
            {
                spellComponent.UpdateCD(spellConfig.Id);

                // 这里简单做一下打断当前技能, 设置新的当前技能
                Spell currentSpell = spellComponent.Current;
                if (currentSpell != null)
                {
                    Interrupt(unit, currentSpell);
                }
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
                    spell.SpellStatus = SpellStatus.Casting;
                    if (spellConfig.HitTime > 0)
                    {
                        
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
                    // 如果技能在吟唱或者读条，等待结束
                    if (spell.SpellStatus == SpellStatus.Channeling)
                    {
                        ObjectWait objectWait = spell.GetComponent<ObjectWait>() ?? spell.AddComponent<ObjectWait>();
                        await objectWait.Wait<WaitSpellChanneling>();
                        if (cancellationToken.IsCancel())
                        {
                            return;
                        }
                    }

                    spell.SpellStatus = SpellStatus.Finished;
                }
#endregion
            }
            finally
            {
                Remove(unit, spell.Id);
            }
        }
       
        public static void Interrupt(Unit unit, Spell spell)
        {
            spell.CancellationToken.Cancel();
            
            M2C_SpellRemove m2CSpellRemove = M2C_SpellRemove.Create();
            m2CSpellRemove.UnitId = unit.Id;
            m2CSpellRemove.SpellId = spell.Id;
            m2CSpellRemove.RemoveType = (int)SpellFlags.NewSpellInterrupt;
            MapMessageHelper.NoticeClient(unit, m2CSpellRemove, spell.GetConfig().NoticeType);
        }

        public static void Remove(Unit unit, long spellId)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
            spellComponent.RemoveSpell(spellId);
        }
    }
}