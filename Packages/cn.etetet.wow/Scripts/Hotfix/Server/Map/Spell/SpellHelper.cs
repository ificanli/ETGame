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
                EffectDispatcher.Instance.Run(new Effect(effectConfig, spell, EffectTimeType.SpellHit));
            }
        }
        
        public static void InterruptSpell(Unit unit)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.CancellationToken?.Cancel();
        }
    }
}