namespace ET.Server
{
    public class BTTargetSelectorCasterHandler: ABTHandler<TargetSelectorCaster>
    {
        protected override int Run(TargetSelectorCaster node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            Unit unit = buff.GetCaster();
            
            buff.GetComponent<SpellTargetComponent>().Units.Add(unit);
            
            return 0;
        }
    }
}