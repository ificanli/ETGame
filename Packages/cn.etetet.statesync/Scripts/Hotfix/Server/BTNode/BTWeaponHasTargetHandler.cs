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
                Log.Warning($"BTWeaponHasTarget: unit {caster.Id} has no TargetSelectorComponent");
                return 1;
            }

            Unit target = selector.SelectTarget();
            if (target == null)
            {
                Log.Warning($"BTWeaponHasTarget: unit {caster.Id} found no target (MaxRange={selector.MaxRange})");
                return 1;
            }

            // 将目标写入env供后续节点使用
            if (!string.IsNullOrEmpty(node.Target))
            {
                env.AddEntity(node.Target, target);
            }

            return 0;
        }
    }
}
