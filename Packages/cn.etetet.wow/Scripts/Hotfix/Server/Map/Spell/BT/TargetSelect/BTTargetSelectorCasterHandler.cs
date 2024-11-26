namespace ET.Server
{
    public class BTTargetSelectorCasterHandler: ABTHandler<TargetSelectorCaster>
    {
        protected override int Run(TargetSelectorCaster node, BTEnv env)
        {
            Spell spell = env.GetEntity<Spell>(node.Spell);

            Unit unit = spell.GetCaster();
            
            spell.GetComponent<SpellTargetComponent>().Units.Add(unit);
            
            return 0;
        }
    }
}