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

            JoystickMoveSyncStateComponent syncState = unit.GetComponent<JoystickMoveSyncStateComponent>();
            if (syncState == null)
            {
                syncState = unit.AddComponent<JoystickMoveSyncStateComponent>();
            }

            if (message.MoveSequence <= syncState.LastAppliedMoveSequence)
            {
                if (unit.IsMyUnit())
                {
                    Log.Info(
                        $"[NavMove][AuthRecvDrop] unitId={unit.Id}, incomingSeq={message.MoveSequence}, lastAppliedSeq={syncState.LastAppliedMoveSequence}, pos=({message.PosX:F3},{message.PosY:F3},{message.PosZ:F3}), dir=({message.DirX:F3},{message.DirZ:F3}), speed={message.Speed:F3}");
                }
                await ETTask.CompletedTask;
                return;
            }

            syncState.LastAppliedMoveSequence = message.MoveSequence;

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
                long clientNow = TimeInfo.Instance.ClientNow();
                JoystickMoveAuthorityStateComponent currentAuthorityState = authorityState;
                Log.Info(
                    $"[NavMove][TraceAuthRecv] unitId={unit.Id}, clientNow={clientNow}, moveSeq={message.MoveSequence}, ackInputSeq={message.LastProcessedInputSequence}, posX={message.PosX:F6}, posY={message.PosY:F6}, posZ={message.PosZ:F6}, dirX={message.DirX:F6}, dirZ={message.DirZ:F6}, speed={message.Speed:F3}");
                Log.Info(
                    $"[NavMove][AuthRecv] unitId={unit.Id}, seq={message.MoveSequence}, ackInputSeq={message.LastProcessedInputSequence}, pos={newPosition}, dir=({message.DirX:F3},{message.DirZ:F3}), speed={message.Speed:F3}, currentUnitPos={unit.Position}, authorityLastSpeed={currentAuthorityState.LastSpeed:F3}");
            }
            if (unit.IsMyUnit())
            {
                unit.Position = newPosition;
                await ETTask.CompletedTask;
                return;
            }

            unit.Rotation = new quaternion(message.RotX, message.RotY, message.RotZ, message.RotW);

            // 设置权威位置（触发 ChangePosition 事件 → ChangePosition_SyncGameObjectPos
            // 在 HotfixView 层为远程单位启用预测插值）
            unit.Position = newPosition;

            await ETTask.CompletedTask;
        }
    }
}
