using System;

namespace ET.Client
{
    public static class SpellHelper
    {
        public static void Cast(Unit unit, C2M_SpellCast c2MSpellCast)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(c2MSpellCast.SpellConfigId);
            
            // 临时技能
            Spell spell = spellComponent.CreateSpell(spellConfig, IdGenerater.Instance.GenerateId());
            try
            {
                spell.Caster = unit;

                // Client Spell Add Effect分发， 表现层可以播放前摇动作
                EffectHelper.RunSpellEffects(spell, EffectTimeType.ClientSpellAdd);

                unit.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);
            }
            finally
            {
                spellComponent.RemoveSpell(spell.Id);
            }
        }

        public static async ETTask Start(Unit unit, long spellId, int spellConfigId)
        {
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);
            
            using Spell spell = spellComponent.CreateSpell(spellConfig, spellId);
            ObjectWait objectWait = spell.AddComponent<ObjectWait>();
            // 等待spellhit
            Wait_M2C_SpellHit waitM2CSpellHit = await objectWait.Wait<Wait_M2C_SpellHit>().TimeoutAsync(10000);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            UnitComponent unitComponent = unit.GetParent<UnitComponent>();
            SpellTargetComponent spellTargetComponent = spell.AddComponent<SpellTargetComponent>();
            foreach (long targetId in waitM2CSpellHit.Message.TargetUnitId)
            {
                spellTargetComponent.Units.Add(unitComponent.Get(targetId));
            }
            spellTargetComponent.Position = waitM2CSpellHit.Message.TargetPosition;
            
            EffectHelper.RunSpellEffects(spell, EffectTimeType.ClientSpellHit);
            
            
            
            
            // 等待spellremove
            Wait_M2C_SpellRemove waitM2CSpellRemove = await objectWait.Wait<Wait_M2C_SpellRemove>().TimeoutAsync(10000);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            EffectHelper.RunSpellEffects(spell, EffectTimeType.ClientSpellRemove);
        }
    }
}