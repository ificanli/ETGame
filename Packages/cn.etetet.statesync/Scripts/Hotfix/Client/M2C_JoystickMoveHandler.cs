using Unity.Mathematics;

namespace ET.Client
{
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

            // MoveSequence 去重（底线设施，防止乱序/重复包）
            JoystickMoveSyncStateComponent syncState = unit.GetComponent<JoystickMoveSyncStateComponent>();
            if (syncState == null)
            {
                syncState = unit.AddComponent<JoystickMoveSyncStateComponent>();
            }

            if (message.MoveSequence <= syncState.LastAppliedMoveSequence)
            {
                await ETTask.CompletedTask;
                return;
            }

            syncState.LastAppliedMoveSequence = message.MoveSequence;

            // 保留 authority state 用于日志对账和动画判断
            JoystickMoveAuthorityStateComponent authorityState = unit.GetComponent<JoystickMoveAuthorityStateComponent>();
            if (authorityState == null)
            {
                authorityState = unit.AddComponent<JoystickMoveAuthorityStateComponent>();
            }

            authorityState.LastSpeed = message.Speed;
            authorityState.LastDirection = new float3(message.DirX, 0f, message.DirZ);
            authorityState.LastSyncTime = TimeInfo.Instance.ClientNow();
            authorityState.LastProcessedInputSequence = message.LastProcessedInputSequence;

            float3 newPosition = new float3(message.PosX, message.PosY, message.PosZ);

            if (unit.IsMyUnit())
            {
                // 本机：设置 unit.Position 会触发 ChangePosition 事件
                // ChangePosition_SyncGameObjectPos 会调用 ApplyAuthoritativePosition
                unit.Position = newPosition;
                await ETTask.CompletedTask;
                return;
            }

            unit.Rotation = new quaternion(message.RotX, message.RotY, message.RotZ, message.RotW);
            unit.Position = newPosition;

            await ETTask.CompletedTask;
        }
    }
}
