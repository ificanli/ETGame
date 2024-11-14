using System;

namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
    public static class SpellHelper
    {
        public static async ETTask Cast(Unit unit, int spellConfigId, Spell parent = null)
        {
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);

            // 检查技能是否能施放
            if (!Check(unit, spellConfig))
            {
                return;
            }
            
            // 发送SpellStart消息
            long spellId = IdGenerater.Instance.GenerateId();
            
            long startTime = TimeInfo.Instance.FrameTime;

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
            Spell spell = spellComponent.CreateSpell(spellConfig, spellId);

            // 子技能没有CD
            if (parent == null)
            {
                spellComponent.UpdateCD(spellConfigId);
            }

            if (parent == null)
            {
                // 打断老技能，这里先简单处理，技能打断有一套规则
                Spell preSpell = spellComponent.Current;
                if (preSpell != null)
                {
                    Interrupt(unit, preSpell.Id);
                }

                spellComponent.Current = spell;
            }
            else
            {
                spell.ParentSpell = parent;
            }
            
            
            spellComponent.CancellationToken = cancellationToken;
            
            M2C_SpellAdd m2CSpellAdd = M2C_SpellAdd.Create();
            m2CSpellAdd.UnitId = unit.Id;
            m2CSpellAdd.SpellId = spellId;
            m2CSpellAdd.SpellConfigId = spellConfigId;
            MapMessageHelper.NoticeClient(unit, m2CSpellAdd, spellConfig.NoticeType);
            
            EffectHelper.RunSpellEffects(spell, EffectTimeType.ServerSpellAdd);

#region SpellHit

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
            m2CSpellHit.UnitId = unit.Id;
            m2CSpellHit.SpellId = spell.Id;
            m2CSpellHit.TargetPosition = spellTargetComponent.Position;
            foreach (Unit target in spellTargetComponent.Units)
            {
                m2CSpellHit.TargetUnitId.Add(target.Id);
            }
            MapMessageHelper.NoticeClient(unit, m2CSpellHit, spellConfig.NoticeType);

            // 对目标分发hitEffect
            EffectHelper.RunSpellEffects(spell, EffectTimeType.ServerSpellHit);

#endregion
            


#region SpellRemove

            await timerComponent.WaitTillAsync(startTime + spellConfig.Duration);
            if (cancellationToken.IsCancel())
            {
                return;
            }

            // 发送SpellRemove消息
            Remove(unit, spell.Id);
            
#endregion
        }

        private static bool Check(Unit unit, SpellConfig spellConfig)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            if (!spellComponent.CheckCD(spellConfig.Id))
            {
                ErrorHelper.MapError(unit, TextConstDefine.SpellCast_SpellInCD);
                return false;
            }
            return true;
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
            
            MapMessageHelper.NoticeClient(unit, spellRemove, spell.Config.NoticeType);
            
            spellComponent.RemoveSpell(spellId);
        }
    }
}