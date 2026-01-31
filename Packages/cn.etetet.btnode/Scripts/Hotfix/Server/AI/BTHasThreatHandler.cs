namespace ET.Server
{
    public class BTHasThreatHandler: ABTHandler<BTHasThreat>
    {
        protected override int Run(BTHasThreat node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            if (threatComponent == null)
            {
                return 1;
            }
            if (threatComponent.GetCount() > 0)
            {
                return 0;
            }
            return 1;
        }
    }
}
