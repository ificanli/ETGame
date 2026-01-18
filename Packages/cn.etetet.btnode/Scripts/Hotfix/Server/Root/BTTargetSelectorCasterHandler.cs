namespace ET.Server
{
    public class BTTargetSelectorCasterHandler: ABTHandler<TargetSelectorCaster>
    {
        protected override int Run(TargetSelectorCaster node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            Unit unit = buff.GetCaster();
            
            buff.GetBuffData().GetComponent<SpellTargetComponent>().Units.Add(unit.Id);
            
            return 0;
        }
    }
}