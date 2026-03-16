namespace ET.Server
{
    public class BTTargetSelectorCasterHandler: ABTHandler<TargetSelectorCaster>
    {
        protected override int Run(TargetSelectorCaster node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (buff == null || buff.IsDisposed)
            {
                return 1;
            }

            Unit unit = buff.GetCaster();
            if (unit == null || unit.IsDisposed)
            {
                return 1;
            }
            
            buff.GetOrAddSpellTargetComponent().Units.Add(unit.Id);
            
            return 0;
        }
    }
}
