namespace ET.Server
{
    public static class SpellHelper
    {
        public static void Cast(Unit unit, int spellConfigId)
        {
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            Spell spell = spellComponent.CreateSpell(spellConfigId);
            
            //spell.AddComponent<SpellStateComponent>();
            //spell.AddComponent<SpellEffectComponent>();
            //spell.AddComponent<SpellTargetComponent>();
            //spell.AddComponent<SpellTargetPositionComponent>();
            //spell.AddComponent<SpellTargetUnitComponent>();
        }
    }
}