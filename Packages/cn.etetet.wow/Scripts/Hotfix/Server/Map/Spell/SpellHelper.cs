using System;

namespace ET.Server
{
    [FriendOf(typeof(SpellComponent))]
    public static class SpellHelper
    {
        public static async ETTask Cast(Unit unit, int spellConfigId, long parentSpellId = 0)
        {
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);

            // 检查技能是否能施放
            int checkRet = Check(unit, spellConfig);
            if (checkRet != 0)
            {
                ErrorHelper.MapError(unit, checkRet);
                return;
            }
            
            // 发送SpellStart消息
            long startTime = TimeInfo.Instance.FrameTime;

            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
            Spell spell = spellComponent.CreateSpell(spellConfigId, parentSpellId);

            // 子技能没有CD
            if (parentSpellId == 0)
            {
                spellComponent.UpdateCD(spellConfigId);
            }

            if (parentSpellId == 0)
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
                spell.ParentSpell = parentSpellId;
            }
            
            
            spellComponent.CancellationToken = cancellationToken;
            
            M2C_SpellAdd m2CSpellAdd = M2C_SpellAdd.Create();
            m2CSpellAdd.UnitId = unit.Id;
            m2CSpellAdd.SpellId = spell.Id;
            m2CSpellAdd.SpellConfigId = spellConfigId;
            MapMessageHelper.NoticeClient(unit, m2CSpellAdd, spellConfig.NoticeType);
            
            EffectHelper.RunBT<EffectServerBuffAdd>(spell);

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
            EffectHelper.RunBT<EffectServerSpellHit>(spell);

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

        private static int Check(Unit unit, SpellConfig spellConfig)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            if (!spellComponent.CheckCD(spellConfig))
            {
                return TextConstDefine.SpellCast_SpellInCD;
            }

            int costCheckRet = CostDispatcher.Instance.Handle(unit, spellConfig);
            if (costCheckRet != 0)
            {
                return costCheckRet;
            }
            
            return 0;
        }

        private static SpellTargetComponent SelectTarget(Unit unit, Spell spell)
        {
            SpellTargetComponent spellTargetComponent = spell.AddComponent<SpellTargetComponent>();
            SpellConfig spellConfig = spell.GetConfig();
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
            
            MapMessageHelper.NoticeClient(unit, spellRemove, spell.GetConfig().NoticeType);
            
            spellComponent.RemoveSpell(spellId);
        }
    }
}