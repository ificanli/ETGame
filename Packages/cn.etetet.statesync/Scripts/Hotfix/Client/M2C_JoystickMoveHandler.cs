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
            Scene currentScene = root.CurrentScene();
            if (currentScene == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            Unit unit = unitComponent?.Get(message.UnitId);
            if (unit == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            float3 newPosition = new float3(message.PosX, message.PosY, message.PosZ);

            if (!unit.IsMyUnit())
            {
                unit.Rotation = new quaternion(message.RotX, message.RotY, message.RotZ, message.RotW);
            }

            // 设置权威位置（触发 ChangePosition 事件 → ChangePosition_SyncGameObjectPos
            // 在 HotfixView 层为远程单位启用预测插值）
            unit.Position = newPosition;

            await ETTask.CompletedTask;
        }
    }
}
