namespace ET.Client
{
    public static class SpellHelper
    {
        public static async ETTask Cast(Unit unit, C2M_SpellCast c2MSpellCast)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            Spell spell = spellComponent.CreateSpell(c2MSpellCast.SpellConfigId);
            spellComponent.Current = spell;
            
            // Start Effect分发， 表现层可以播放前摇动作
            SpellEffectHelper.RunEffects(spell, EffectTimeType.SpellStart);
            
            unit.Root().GetComponent<ClientSenderComponent>().Send(c2MSpellCast);
            await ETTask.CompletedTask;
        }
    }
}