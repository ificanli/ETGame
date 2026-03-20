namespace ET.Server
{
    [Event(SceneType.Gate)]
    public class PlayerLoadoutConfirmed_ApplyCurrentUnitLoadout : AEvent<Scene, PlayerLoadoutConfirmed>
    {
        protected override async ETTask Run(Scene scene, PlayerLoadoutConfirmed args)
        {
            if (scene == null || scene.IsDisposed || args.PlayerId <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            MessageLocationSenderComponent senderComponent = scene.GetComponent<MessageLocationSenderComponent>();
            if (senderComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            EntityRef<Scene> sceneRef = scene;
            try
            {
                A2Map_ApplyLoadoutRequest request = A2Map_ApplyLoadoutRequest.Create();
                request.HeroConfigId = args.HeroConfigId;
                request.MainWeaponConfigId = args.MainWeaponConfigId;
                request.SubWeaponConfigId = args.SubWeaponConfigId;
                request.ArmorConfigId = args.ArmorConfigId;

                A2Map_ApplyLoadoutResponse response =
                        await senderComponent.Get(LocationType.Unit).Call(args.PlayerId, request) as A2Map_ApplyLoadoutResponse;
                scene = sceneRef;
                if (scene == null || scene.IsDisposed)
                {
                    return;
                }

                if (response == null)
                {
                    Log.Warning($"[LoadoutRuntimeSync] empty response, player={args.PlayerId}");
                    return;
                }

                if (response.Error != ErrorCode.ERR_Success)
                {
                    Log.Warning($"[LoadoutRuntimeSync] apply failed, player={args.PlayerId}, error={response.Error}, message={response.Message}");
                    return;
                }

                Log.Info($"[LoadoutRuntimeSync] applied to live unit, player={args.PlayerId}, hero={args.HeroConfigId}, main={args.MainWeaponConfigId}, sub={args.SubWeaponConfigId}, armor={args.ArmorConfigId}");
            }
            catch (RpcException e)
            {
                Log.Info($"[LoadoutRuntimeSync] skip live unit apply, player={args.PlayerId}, rpcError={e.Error}, message={e.Message}");
            }
        }
    }
}
