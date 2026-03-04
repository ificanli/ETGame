namespace ET.Client
{
    /// <summary>
    /// 客户端摇杆移动辅助，供UI层调用。
    /// 发送摇杆方向给服务端，由服务端驱动Unit移动。
    /// </summary>
    public static class JoystickMoveHelper
    {
        /// <summary>
        /// 发送摇杆输入（由UI摇杆回调调用）
        /// </summary>
        /// <param name="root">客户端Scene</param>
        /// <param name="dirX">摇杆X方向 (-1 to 1)</param>
        /// <param name="dirZ">摇杆Z方向 (-1 to 1)</param>
        public static void SendJoystickInput(Scene root, float dirX, float dirZ)
        {
            ClientSenderComponent sender = root.GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                Log.Warning($"[JoystickTrace][ClientSend] sender missing, scene={root?.Name}, dir=({dirX:F3},{dirZ:F3})");
                return;
            }

            C2M_JoystickInput msg = C2M_JoystickInput.Create();
            msg.DirX = dirX;
            msg.DirZ = dirZ;
            sender.Send(msg);

            if (dirX == 0f && dirZ == 0f)
            {
                Log.Info($"[JoystickTrace][ClientSend] send C2M_JoystickInput scene={root?.Name}, dir=({dirX:F3},{dirZ:F3})");
            }
        }

        /// <summary>
        /// 停止移动（摇杆释放时调用）
        /// </summary>
        public static void StopJoystickMove(Scene root)
        {
            SendJoystickInput(root, 0f, 0f);
        }
    }
}
