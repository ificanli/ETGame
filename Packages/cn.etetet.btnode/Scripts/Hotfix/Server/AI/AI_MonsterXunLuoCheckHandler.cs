namespace ET.Server
{
    public class AI_MonsterXunLuoCheckHandler: ABTHandler<AI_MonsterXunLuoCheck>
    {
        protected override int Run(AI_MonsterXunLuoCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff.GetOwner();
            ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
            if (threatComponent.GetCount() > 0)
            {
                return 1;
            }
            return 0;
        }
    }
}
