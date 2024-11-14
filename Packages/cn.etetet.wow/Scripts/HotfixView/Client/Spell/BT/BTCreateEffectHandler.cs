using UnityEngine;

namespace ET.Client
{
    public class BTCreateEffectHandler: ABTHandler<BTCreateEffect>
    {
        protected override bool Run(Effect effect, BTCreateEffect node)
        {
            switch (effect.EffectTimeType)
            {
                case EffectTimeType.ClientSpellAdd:
                {
                    Unit unit = effect.Parent.Parent.GetParent<Unit>();
                    EffectUnitHelper.Create(unit, node.BindPoint, node.Effect, false);
                    break;
                }
                case EffectTimeType.ClientSpellHit:
                {
                    Spell spell = effect.GetParent<Spell>();
                    SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
                    foreach (Unit unit in spellTargetComponent.Units)
                    {
                        EffectUnitHelper.Create(unit, node.BindPoint, node.Effect, false);
                    }
                    break;
                }
            }

            return true;
        }
    }
}