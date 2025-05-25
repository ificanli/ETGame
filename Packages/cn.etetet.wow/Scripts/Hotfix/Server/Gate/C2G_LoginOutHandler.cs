namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_LoginOutHandler: MessageSessionHandler<C2G_LoginOut, G2C_LoginOut>
    {
        protected override async ETTask Run(Session session, C2G_LoginOut request, G2C_LoginOut response)
        {
            // 这里可以做一些清理工作，比如从GateSessionKeyComponent中移除key

            // 断开连接
            Scene root = session.Root();
            SessionPlayerComponent sessionPlayerComponent = session.GetComponent<SessionPlayerComponent>();
            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            Player player = playerComponent.GetByAccount(sessionPlayerComponent.Player.Account);
            playerComponent.Remove(player);
            
            response.Error = ErrorCode.ERR_Success;
            response.Message = "Logout successful!";

            WaitRemoveSession(session).NoContext();
            
            await ETTask.CompletedTask;
        }

        private static async ETTask WaitRemoveSession(Session session)
        {
            await session.Root().GetComponent<TimerComponent>().WaitAsync(500); 
            
            session.Dispose();
        }
    }
}