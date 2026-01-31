namespace ET.Server
{
    public class AI_MonsterZhuiJiCheckHandler: ABTHandler<AI_MonsterZhuiJiCheck>
    {
        protected override int Run(AI_MonsterZhuiJiCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            if (threatComponent == null)
            {
                return 1;
            }
            if (threatComponent.GetCount() == 0)
            {
                return 1;
            }

            return 0;
        }
    }
}
