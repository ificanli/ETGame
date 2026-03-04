using Unity.Mathematics;

namespace ET.Server
{
    public class BTWeaponNotMovingHandler : ABTHandler<BTWeaponNotMoving>
    {
        protected override int Run(BTWeaponNotMoving node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            if (caster == null)
            {
                return 1;
            }

            // 检查是否有 JoystickMoveComponent（如果有说明正在移动）
            JoystickMoveComponent moveComp = caster.GetComponent<JoystickMoveComponent>();
            if (moveComp != null)
            {
                // 检查移动方向是否为零
                float3 direction = moveComp.Direction;
                if (math.lengthsq(direction) > 0.01f)
                {
                    return 1; // 正在移动
                }
            }

            return 0; // 静止
        }
    }
}
