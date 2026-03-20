namespace ET.Server
{
    /// <summary>
    /// 从起装商店直接购买到当前携带态。
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_LoadoutBuyFromShopHandler : MessageSessionHandler<C2G_LoadoutBuyFromShop, G2C_LoadoutBuyFromShop>
    {
        protected override async ETTask Run(Session session, C2G_LoadoutBuyFromShop request, G2C_LoadoutBuyFromShop response)
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

                response.Error = LoadoutOperationHelper.BuyFromShop(loadout, storage, request, out string message);
                response.Message = response.Error == ErrorCode.ERR_Success ? "success" : message;

                if (response.Error == ErrorCode.ERR_Success)
                {
                    LoadoutOperationHelper.PushStateChanged(player, loadout, storage);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
