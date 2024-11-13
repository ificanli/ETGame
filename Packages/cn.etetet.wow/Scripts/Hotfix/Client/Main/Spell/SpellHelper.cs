using System;

namespace ET.Client
{
    public static class SpellHelper
    {
        public static void Cast(Unit unit, C2M_SpellCast c2MSpellCast)
        {
            unit.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);
        }

        public static async ETTask Start(Unit unit, long spellId, int spellConfigId)
        {
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            spellComponent.CancellationToken = cancellationToken;
            
            if (unit.IsMyUnit())
            {
                spellComponent.UpdateCD(spellConfigId);
            }

            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(spellConfigId);
            
            Spell spell = spellComponent.CreateSpell(spellConfig, spellId);
            spell.Caster = unit;
            EffectHelper.RunSpellEffects(spell, EffectTimeType.ClientSpellAdd);
            
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