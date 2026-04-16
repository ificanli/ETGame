using Unity.Mathematics;

namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_MoveSyncHandler : MessageHandler<Scene, M2C_MoveSync>
    {
        protected override async ETTask Run(Scene root, M2C_MoveSync message)
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

            // 这是其他玩家的位置同步（自己不会收到这个消息）
            // 序列号去重
            JoystickMoveSyncStateComponent syncState = unit.GetComponent<JoystickMoveSyncStateComponent>();
            if (syncState == null)
            {
                syncState = unit.AddComponent<JoystickMoveSyncStateComponent>();
            }

            if (message.Sequence <= syncState.LastAppliedMoveSequence)
            {
                await ETTask.CompletedTask;
                return;
            }

            syncState.LastAppliedMoveSequence = message.Sequence;

            // 更新 authority state 用于外推
            JoystickMoveAuthorityStateComponent authorityState = unit.GetComponent<JoystickMoveAuthorityStateComponent>();
            if (authorityState == null)
            {
                authorityState = unit.AddComponent<JoystickMoveAuthorityStateComponent>();
            }

            authorityState.LastSpeed = message.Speed;
            authorityState.LastDirection = new float3(message.DirX, 0f, message.DirZ);
            authorityState.LastSyncTime = TimeInfo.Instance.ClientNow();

            // 从方向推算旋转
            if (message.Speed > 0.01f && (message.DirX != 0f || message.DirZ != 0f))
            {
                float3 dir = new float3(message.DirX, 0f, message.DirZ);
                unit.Rotation = quaternion.LookRotationSafe(dir, math.up());
            }

            float3 newPosition = new float3(message.PosX, message.PosY, message.PosZ);
            unit.Position = newPosition;

            await ETTask.CompletedTask;
        }
    }
}
