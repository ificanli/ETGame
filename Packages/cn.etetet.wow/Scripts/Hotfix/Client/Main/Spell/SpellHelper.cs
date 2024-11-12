namespace ET.Client
{
    public static class SpellHelper
    {
        public static async ETTask Cast(Unit unit, C2M_SpellCast c2MSpellCast)
        {
            ETCancellationToken cancellationToken = await ETTaskHelper.GetContextAsync<ETCancellationToken>();
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            SpellConfig spellConfig = SpellConfigCategory.Instance.Get(c2MSpellCast.SpellConfigId);
            Spell spell = spellComponent.CreateSpell(spellConfig);
            spell.Caster = unit;
            spellComponent.Current = spell;
            
            // Client Spell Add Effect分发， 表现层可以播放前摇动作
            EffectHelper.RunSpellEffects(spell, EffectTimeType.ClientSpellAdd);
            
            unit.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);

            ObjectWait objectWait = unit.GetComponent<ObjectWait>();
            // 等待spelladd
            Wait_M2C_SpellAdd waitM2CSpellAdd = await objectWait.Wait<Wait_M2C_SpellAdd>().TimeoutAsync(10000);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            
            
            // 等待spellhit
            Wait_M2C_SpellHit waitM2CSpellHit = await objectWait.Wait<Wait_M2C_SpellHit>().TimeoutAsync(10000);
            if (cancellationToken.IsCancel())
            {
                return;
            }
            
            
            // 等待spellremove
            Wait_M2C_SpellRemove waitM2CSpellRemove = await objectWait.Wait<Wait_M2C_SpellRemove>().TimeoutAsync(10000);
            if (cancellationToken.IsCancel())
            {
                return;
            }
        }
    }
}