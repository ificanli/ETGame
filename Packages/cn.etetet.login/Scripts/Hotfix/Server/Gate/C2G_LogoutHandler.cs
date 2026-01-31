namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_LogoutHandler: MessageSessionHandler<C2G_Logout, G2C_Logout>
    {
        protected override async ETTask Run(Session session, C2G_Logout request, G2C_Logout response)
        {
            EntityRef<Session> sessionRef = session;
            SessionPlayerComponent sessionPlayerComponent = session.GetComponent<SessionPlayerComponent>();
            await LogoutHelper.Logout(sessionPlayerComponent.Player);
            
            response.Error = ErrorCode.ERR_Success;
            response.Message = "Logout successful!";

            session = sessionRef;
            WaitRemoveSession(session).Coroutine();
        }

        private static async ETTask WaitRemoveSession(Session session)
        {
            EntityRef<Session> sessionRef = session;
            await session.Root().TimerComponent.WaitAsync(500);
            session = sessionRef;
            session.Dispose();
        }
    }
}