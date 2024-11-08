namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
    public static class SpellHelper
    {
        public static async ETTask Cast(Unit unit, int spellConfigId, Spell parent = null)
        {
            long startTime = TimeInfo.Instance.FrameTime;
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.CancellationToken = cancellationToken;
            
            Spell spell = spellComponent.CreateSpell(spellConfigId);
            if (parent == null)
            {
                // 打断老技能，这里先简单处理，技能打断有一套规则
                Spell preSpell = spellComponent.Current;
                if (preSpell != null)
                {
                    spellComponent.CancellationToken.Cancel();
                }

                spellComponent.Current = spell;
            }
            else
            {
                spell.ParentSpell = parent;
            }
            
            SpellConfig spellConfig = spell.GetConfig();
            
            
            SpellEffectHelper.RunEffects(spell, EffectTimeType.ServerSpellStart);
            
            
            // 发送SpellStart消息
            M2C_SpellAdd m2CSpellAdd = M2C_SpellAdd.Create();
            m2CSpellAdd.UnitId = unit.Id;
            m2CSpellAdd.SpellId = spell.Id;
            MapMessageHelper.Broadcast(unit, m2CSpellAdd);
            
            
            // 等到命中
            TimerComponent timerComponent = unit.Scene().GetComponent<TimerComponent>();
            await timerComponent.WaitTillAsync(startTime + spellConfig.HitTime);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            // 选择目标
            SpellTargetComponent spellTargetComponent = SelectTarget(unit, spell);
            
            // 发送SpellHit消息
            M2C_SpellHit m2CSpellHit = M2C_SpellHit.Create();
            m2CSpellHit.SpellId = spell.Id;
            m2CSpellHit.TargetPosition = spellTargetComponent.Position;
            foreach (Unit target in spellTargetComponent.Units)
            {
                m2CSpellHit.TargetUnitId.Add(target.Id);
            }
            MapMessageHelper.Broadcast(unit, m2CSpellHit);
            
            // 对目标分发hitEffect
            foreach (Unit target in spellTargetComponent.Units)
            {
                if (target == null)
                {
                    continue;
                }

                SpellEffectHelper.RunEffects(spell, EffectTimeType.ServerSpellHit);
                
                // 命中Buff
                for(int i = 0; i < spellConfig.Buffs.Length; ++i)
                {
                    BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
                    Buff buff = buffComponent.CreateBuff(spellConfig.Buffs[i]);
                    buff.Caster = unit;
                }
            }
            
            await timerComponent.WaitTillAsync(startTime + spellConfig.Duration);
            
            // 发送SpellRemove消息
            M2C_SpellRemove m2CSpellRemove = M2C_SpellRemove.Create();
            m2CSpellRemove.SpellId = spell.Id;
            MapMessageHelper.Broadcast(unit, m2CSpellHit);
        }

        private static SpellTargetComponent SelectTarget(Unit unit, Spell spell)
        {
            SpellTargetComponent spellTargetComponent = spell.AddComponent<SpellTargetComponent>();
            SpellConfig spellConfig = spell.GetConfig();
            switch (spellConfig.TargetSelector[0])
            {
                case SpellTargetType.Select:
                {
                    break;
                }
                case SpellTargetType.Caster:
                {
                    spellTargetComponent.Units.Add(unit);
                    break;
                }
            }

            return spellTargetComponent;
        }
        
        public static void Interrupt(Unit unit)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.CancellationToken?.Cancel();
        }
    }
}