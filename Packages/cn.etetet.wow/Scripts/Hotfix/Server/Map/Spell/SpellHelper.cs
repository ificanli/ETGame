namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
    public static class SpellHelper
    {
        public static async ETTask Cast(Unit caster, int spellConfigId, Spell parent = null)
        {
            long startTime = TimeInfo.Instance.FrameTime;
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            SpellComponent spellComponent = caster.GetComponent<SpellComponent>();
            spellComponent.CancellationToken = cancellationToken;
            
            Spell spell = spellComponent.CreateSpell(spellConfigId);
            if (parent == null)
            {
                spellComponent.Current = spell;
            }
            else
            {
                spell.ParentSpell = parent;
            }
            
            SpellConfig spellConfig = spell.GetConfig();
            
            // 选择目标
            SpellTargetComponent spellTargetComponent = SelectTarget(caster, spell);
            
            
            SpellEffectHelper.RunEffects(spell, EffectTimeType.SpellStart);
            

            // 等到命中
            TimerComponent timerComponent = caster.Scene().GetComponent<TimerComponent>();
            await timerComponent.WaitTillAsync(startTime + spellConfig.HitTime);
            if (cancellationToken.IsCancel())
            {
                return;
            }

            foreach (Unit unit in spellTargetComponent.Units)
            {
                if (unit == null)
                {
                    continue;
                }

                SpellEffectHelper.RunEffects(spell, EffectTimeType.SpellHit);
                
                // 命中Buff
                for(int i = 0; i < spellConfig.Buffs.Length; ++i)
                {
                    BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
                    buffComponent.CreateBuff(spellConfig.Buffs[i]);
                }
            }
            
            await timerComponent.WaitTillAsync(startTime + spellConfig.Duration);
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