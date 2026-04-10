namespace ET.Server
{
    public class BTWeaponHasTargetHandler : ABTHandler<BTWeaponHasTarget>
    {
        protected override int Run(BTWeaponHasTarget node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);

            TargetSelectorComponent selector = caster.GetComponent<TargetSelectorComponent>();
            if (selector == null)
            {
                Log.Warning($"[WeaponFireTrace][HasTarget] missing selector, caster={caster.Id}");
                Log.Warning($"BTWeaponHasTarget: unit {caster.Id} has no TargetSelectorComponent");
                return 1;
            }

            Unit target = caster.GetComponent<TargetComponent>()?.Unit;
            if (TargetSelectorHelper.IsValidTarget(caster, target, float.MaxValue))
            {
                selector.CurrentTargetId = target.Id;
            }
            else
            {
                target = selector.SelectTarget();
            }

            if (target == null)
            {
                int seeUnitCount = caster.GetComponent<AOIEntity>()?.GetSeeUnits().Count ?? 0;
                Log.Warning(
                    $"[WeaponFireTrace][HasTarget] no target, caster={caster.Id}, unitType={caster.UnitType}, configId={caster.ConfigId}, pos=({caster.Position.x:F2},{caster.Position.y:F2},{caster.Position.z:F2}), maxRange={selector.MaxRange}, currentTargetId={selector.CurrentTargetId}, seeUnits={seeUnitCount}");
                Log.Warning($"BTWeaponHasTarget: unit {caster.Id} found no target (MaxRange={selector.MaxRange})");
                return 1;
            }

            Log.Info(
                $"[WeaponFireTrace][HasTarget] target selected, caster={caster.Id}, unitType={caster.UnitType}, configId={caster.ConfigId}, target={target.Id}, targetType={target.UnitType}, targetConfigId={target.ConfigId}, maxRange={selector.MaxRange}");

            // 将目标写入env供后续节点使用
            if (!string.IsNullOrEmpty(node.Target))
            {
                env.AddEntity(node.Target, target);
            }

            return 0;
        }
    }
}
