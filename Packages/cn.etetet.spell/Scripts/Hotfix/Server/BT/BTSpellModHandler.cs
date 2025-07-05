namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTSpellModHandler: ABTHandler<BTSpellMod>
    {
        protected override int Run(BTSpellMod node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
            
            spellComponent.AddMod(node.SpellId, node.SpellModType, node.Value);
            
            Buff buff = env.GetEntity<Buff>(node.Buff);
            
            // buff删除的时候会还原数值
            BuffSpellModRecordComponent buffSpellModRecordComponent = 
                    buff.GetComponent<BuffSpellModRecordComponent>() ??
                    buff.AddComponent<BuffSpellModRecordComponent>();
            buffSpellModRecordComponent.Add(node.SpellId, node.SpellModType, node.Value);
            
            return 0;
        }
    }
}