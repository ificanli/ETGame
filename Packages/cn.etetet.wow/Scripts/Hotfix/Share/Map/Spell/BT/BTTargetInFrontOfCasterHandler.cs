using Unity.Mathematics;

namespace ET
{
    public class BTTargetInFrontOfCasterHandler: ABTHandler<BTTargetInFrontOfCaster>
    {
        protected override int Run(BTTargetInFrontOfCaster node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            float v = math.dot(caster.Forward, target.Position - caster.Position);
            if (v < 0)
            {
                return TextConstDefine.SpellCast_TargetNotInFrontOfCaster;
            }
            return 0;
        }
    }
}