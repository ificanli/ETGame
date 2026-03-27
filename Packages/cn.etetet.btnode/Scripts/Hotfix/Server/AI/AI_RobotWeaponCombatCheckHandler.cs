namespace ET.Server
{
    public class AI_RobotWeaponCombatCheckHandler : ABTHandler<AI_RobotWeaponCombatCheck>
    {
        protected override int Run(AI_RobotWeaponCombatCheck node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return 1;
            }

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            if (targetComponent == null)
            {
                return 1;
            }

            float maxRange = node.MaxRange > 0f ? node.MaxRange : 18f;
            Unit currentTarget = targetComponent.Unit;
            if (TargetSelectorHelper.IsValidTarget(unit, currentTarget, maxRange))
            {
                return 0;
            }

            targetComponent.Unit = null;

            TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>() ?? unit.AddComponent<TargetSelectorComponent>();
            if (node.SelectIntervalMs > 0)
            {
                selector.SelectIntervalMs = node.SelectIntervalMs;
            }

            Unit target = selector.SelectTarget(maxRange);
            if (target == null || target.IsDisposed)
            {
                ThreatComponent threatComponent = unit.GetComponent<ThreatComponent>();
                ThreatInfo threatInfo = threatComponent?.GetMaxThreat();
                target = threatInfo?.Unit;
                if (!TargetSelectorHelper.IsValidTarget(unit, target, maxRange))
                {
                    target = null;
                }
            }

            if (target == null)
            {
                return 1;
            }

            targetComponent.Unit = target;
            return 0;
        }
    }
}
