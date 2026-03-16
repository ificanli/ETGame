namespace ET.Server
{
    /// <summary>
    /// Gate 侧接收 Map 侧的跑局结果，将其回写为新的当前携带态。
    /// </summary>
    [MessageHandler(SceneType.Gate)]
    public class Map2G_LoadoutCarryResultHandler : MessageHandler<Player, Map2G_LoadoutCarryResult>
    {
        protected override async ETTask Run(Player player, Map2G_LoadoutCarryResult message)
        {
            EntityRef<Player> playerRef = player;

            using (await player.Root().CoroutineLockComponent.Wait(CoroutineLockType.Loadout, player.Id))
            {
                player = playerRef;
                if (player == null)
                {
                    return;
                }

                LoadoutComponent loadout = player.GetComponent<LoadoutComponent>() ?? player.AddComponent<LoadoutComponent>();
                PlayerStorageComponent storage = player.GetComponent<PlayerStorageComponent>() ?? player.AddComponent<PlayerStorageComponent>();

                if (message.ResultType == (int)LoadoutCarryResultType.Evacuated)
                {
                    LoadoutStateHelper.ApplyCarryResult(loadout, message);
                    storage.RecordEvacuationSummary(LoadoutStateHelper.BuildEvacuationSummary(message), message.TotalWealthDelta);
                    LoadoutOperationHelper.PushStateChanged(player, loadout, storage);
                    Log.Info($"[Map2G_LoadoutCarryResult] player {player.Id} evacuated, wealth={message.TotalWealthDelta}");
                    return;
                }

                if (message.ResultType == (int)LoadoutCarryResultType.Dead)
                {
                    LoadoutStateHelper.ResetToSecureOnly(loadout);
                    LoadoutOperationHelper.PushStateChanged(player, loadout, storage);
                    Log.Info($"[Map2G_LoadoutCarryResult] player {player.Id} dead, reset to secure only");
                }
            }
        }
    }
}
