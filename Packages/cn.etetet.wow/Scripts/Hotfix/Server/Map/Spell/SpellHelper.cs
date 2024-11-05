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
            SelectTarget(caster, spell);

            // 等到命中
            TimerComponent timerComponent = caster.Scene().GetComponent<TimerComponent>();
            await timerComponent.WaitTillAsync(startTime + spellConfig.HitTime);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            // 命中效果
            for(int i = 0; i < spellConfig.Effects.Length; ++i)
            {
                EffectConfig effectConfig = EffectConfigCategory.Instance.Get(spellConfig.Effects[i]);
                // 分发效果
                EventSystem.Instance.Invoke(effectConfig.Type, new Effect(effectConfig, spell, EffectTimeType.SpellHit));
            }
            // 命中Buff
            for(int i = 0; i < spellConfig.Buffs.Length; ++i)
            {
                // 加上Buff
            }
            
            await timerComponent.WaitTillAsync(startTime + spellConfig.LastTime);
        }

        private static void SelectTarget(Unit unit, Spell spell)
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
        }
        
        public static void Interrupt(Unit unit)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.CancellationToken?.Cancel();
        }
    }
}