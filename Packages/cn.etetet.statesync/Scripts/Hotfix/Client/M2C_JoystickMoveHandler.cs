using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 处理服务端广播的摇杆移动同步消息，在客户端更新单位位置。
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_JoystickMoveHandler : MessageHandler<Scene, M2C_JoystickMove>
    {
        protected override async ETTask Run(Scene root, M2C_JoystickMove message)
        {
            UnitComponent unitComponent = root.CurrentScene().GetComponent<UnitComponent>();
            Unit unit = unitComponent?.Get(message.UnitId);
            if (unit == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 直接纠正位置（服务端权威）
            unit.Position = message.Position;

            // 朝向移动方向
            float3 velocity = message.Velocity;
            if (math.lengthsq(velocity) > 0.0001f)
            {
                unit.Rotation = quaternion.LookRotation(math.normalize(velocity), math.up());
            }

            await ETTask.CompletedTask;
        }
    }
}
