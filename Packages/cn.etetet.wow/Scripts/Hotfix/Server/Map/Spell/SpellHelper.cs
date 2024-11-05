namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
    public static class SpellHelper
    {
        public static async ETTask Cast(Unit unit, int spellConfigId, long parentId = 0)
        {
            long startTime = TimeInfo.Instance.FrameTime;
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.CancellationToken = cancellationToken;
            
            Spell spell = spellComponent.CreateSpell(spellConfigId);
            if (parentId == 0)
            {
                spellComponent.Current = spell;
            }
            
            // 选择目标
            SelectTarget(unit, spell);

            // 执行Effect
            TimerComponent timerComponent = unit.Root().GetComponent<TimerComponent>();
            SpellConfig spellConfig = spell.GetConfig();
            
            for(int i = 0; i < spellConfig.Effects.Length; i +=2)
            {
                await timerComponent.WaitTillAsync(startTime + spellConfig.Effects[i]);
                if (cancellationToken.IsCancel())
                {
                    return;
                }

                EffectConfig effectConfig = EffectConfigCategory.Instance.Get(spellConfig.Effects[i + 1]);
                // 分发效果
                EventSystem.Instance.Invoke(effectConfig.Type, new Effect(effectConfig, spell, EffectTimeType.SpellHit));
            }
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