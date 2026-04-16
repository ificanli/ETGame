using Unity.Mathematics;

namespace ET.Server
{
    public class BTSetSpellTargetPositionFromUnitHandler: ABTHandler<BTSetSpellTargetPositionFromUnit>
    {
        protected override int Run(BTSetSpellTargetPositionFromUnit node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit unit = env.GetEntity<Unit>(node.Unit);
            if (buff == null || buff.IsDisposed || unit == null || unit.IsDisposed)
            {
                return 1;
            }

            float3 position = unit.Position;
            buff.GetOrAddSpellTargetComponent().Position = position;
            return 0;
        }
    }
}
