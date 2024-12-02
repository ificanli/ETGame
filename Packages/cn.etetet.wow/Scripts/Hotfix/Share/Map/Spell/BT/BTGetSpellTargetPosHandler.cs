namespace ET
{
    public class BTGetSpellTargetPosHandler: ABTHandler<BTGetSpellTargetPos>
    {
        protected override int Run(BTGetSpellTargetPos node, BTEnv env)
        {
            Spell spell = env.GetEntity<Spell>(node.Spell);
            SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
            env.AddStruct(node.Pos, spellTargetComponent.Position);
            return 0;
        }
    }
}