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
                Log.Warning($"[JoystickTrace][ClientRecv] current scene is null, unitId={message.UnitId}");
                await ETTask.CompletedTask;
                return;
            }

            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            Unit unit = unitComponent?.Get(message.UnitId);
            if (unit == null)
            {
                Log.Warning($"[JoystickTrace][ClientRecv] unit not found, scene={currentScene.Name}, unitId={message.UnitId}, pos=({message.PosX:F2},{message.PosY:F2},{message.PosZ:F2})");
                await ETTask.CompletedTask;
                return;
            }

            // 直接纠正位置和旋转（服务端权威）
            unit.Position = new float3(message.PosX, message.PosY, message.PosZ);
            unit.Rotation = new quaternion(message.RotX, message.RotY, message.RotZ, message.RotW);

            await ETTask.CompletedTask;
        }
    }
}
