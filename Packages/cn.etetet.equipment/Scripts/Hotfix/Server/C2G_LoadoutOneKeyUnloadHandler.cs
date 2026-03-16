namespace ET.Server
{
    /// <summary>
    /// 一键卸下当前携带态中的正式内容。
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_LoadoutOneKeyUnloadHandler : MessageSessionHandler<C2G_LoadoutOneKeyUnload, G2C_LoadoutOneKeyUnload>
    {
        protected override async ETTask Run(Session session, C2G_LoadoutOneKeyUnload request, G2C_LoadoutOneKeyUnload response)
        {
            SessionPlayerComponent sessionPlayer = session.GetComponent<SessionPlayerComponent>();
            if (sessionPlayer?.Player == null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                return;
            }

            Player player = sessionPlayer.Player;
            EntityRef<Session> sessionRef = session;
            EntityRef<Player> playerRef = player;

            using (await session.Root().CoroutineLockComponent.Wait(CoroutineLockType.Loadout, player.Id))
            {
                session = sessionRef;
                player = playerRef;
                if (session == null || player == null)
                {
                    response.Error = ErrorCode.ERR_ConnectGateKeyError;
                    return;
                }

                LoadoutComponent loadout = player.GetComponent<LoadoutComponent>() ?? player.AddComponent<LoadoutComponent>();
                PlayerStorageComponent storage = player.GetComponent<PlayerStorageComponent>() ?? player.AddComponent<PlayerStorageComponent>();

                LoadoutOperationHelper.OneKeyUnload(loadout, storage);
                response.Error = ErrorCode.ERR_Success;
                response.Message = "success";
                LoadoutOperationHelper.PushStateChanged(player, loadout, storage);
            }

            await ETTask.CompletedTask;
        }
    }
}
