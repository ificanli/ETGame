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

            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);
            Spell spell = spellComponent.CreateSpell(spellConfig);
            if (parent == null)
            {
                // 打断老技能，这里先简单处理，技能打断有一套规则
                Spell preSpell = spellComponent.Current;
                if (preSpell != null)
                {
                    spellComponent.CancellationToken?.Cancel();
                    spellComponent.CancellationToken = null;
                }

                spellComponent.Current = spell;
            }
            else
            {
                spell.ParentSpell = parent;
            }
            
            
            // 发送SpellStart消息
            M2C_SpellAdd m2CSpellAdd = M2C_SpellAdd.Create();
            m2CSpellAdd.UnitId = unit.Id;
            m2CSpellAdd.SpellId = spell.Id;
            MapMessageHelper.Broadcast(unit, m2CSpellAdd);
            
            
            
            EffectHelper.RunSpellEffects(spell, EffectTimeType.ServerSpellAdd);
            
            
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
            EffectHelper.RunSpellEffects(spell, EffectTimeType.ServerSpellHit);
            
            await timerComponent.WaitTillAsync(startTime + spellConfig.Duration);
            
            // 发送SpellRemove消息
            M2C_SpellRemove m2CSpellRemove = M2C_SpellRemove.Create();
            m2CSpellRemove.SpellId = spell.Id;
            MapMessageHelper.Broadcast(unit, m2CSpellHit);
        }

        private static SpellTargetComponent SelectTarget(Unit unit, Spell spell)
        {
            SpellTargetComponent spellTargetComponent = spell.AddComponent<SpellTargetComponent>();
            SpellConfig spellConfig = spell.Config;
            switch (spellConfig.TargetSelector)
            {
                case TargetSelectorSingle:
                {
                    TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
                    spellTargetComponent.Units.Add(targetComponent.Unit);
                    break;
                }
                case TargetSelectorCaster:
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