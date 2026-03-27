namespace ET.Server
{
    /// <summary>
    /// 处理客户端摇杆输入消息，更新服务端Unit的移动方向。
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_JoystickInputHandler : MessageLocationHandler<Unit, C2M_JoystickInput>
    {
        protected override async ETTask Run(Unit unit, C2M_JoystickInput message)
        {
            // 确保JoystickMoveComponent存在
            JoystickMoveComponent joystickMove = unit.GetComponent<JoystickMoveComponent>();
            if (joystickMove == null)
            {
                joystickMove = unit.AddComponent<JoystickMoveComponent>();
            }

            long now = TimeInfo.Instance.ServerNow();
            bool isStop = message.DirX == 0f && message.DirZ == 0f;
            uint lastSequenceBefore = joystickMove.LastClientInputSequence;
            if (message.InputSequence > 0)
            {
                if (message.InputSequence <= joystickMove.LastClientInputSequence)
                {
                    if (now - joystickMove.LastStaleInputLogTime >= 250)
                    {
                        joystickMove.LastStaleInputLogTime = now;
                        Log.Info(
                            $"[NavMove][ServerInputDrop] unitId={unit.Id}, seq={message.InputSequence}, lastSeq={joystickMove.LastClientInputSequence}, " +
                            $"dir=({message.DirX:F3},{message.DirZ:F3}), stop={isStop}");
                    }

                    await ETTask.CompletedTask;
                    return;
                }

                joystickMove.LastClientInputSequence = message.InputSequence;
            }

            joystickMove.LastInputProcessTime = now;
            Log.Info(
                $"[NavMove][TraceServerInputRecv] unitId={unit.Id}, serverNow={now}, seq={message.InputSequence}, dirX={message.DirX:F6}, dirZ={message.DirZ:F6}, stop={isStop}, lastSeqBefore={lastSequenceBefore}, lastSeqAfter={joystickMove.LastClientInputSequence}");
            joystickMove.SetDirection(message.DirX, message.DirZ);
            if (isStop)
            {
                Log.Info($"[NavMove][ServerStop] unitId={unit.Id}, seq={message.InputSequence}, serverNow={now}");
            }

            await ETTask.CompletedTask;
        }
    }
}
