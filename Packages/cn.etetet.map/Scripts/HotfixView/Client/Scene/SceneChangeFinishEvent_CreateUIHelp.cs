namespace ET.Client
{
    [Event(SceneType.Client)]
    public class SceneChangeFinishEvent_CreateUIHelp : AEvent<Scene, SceneChangeFinish>
    {
        protected override async ETTask Run(Scene scene, SceneChangeFinish args)
        {
            EntityRef<Scene> sceneRef = scene;
            await scene.Root().TimerComponent.WaitAsync(2000);
            scene = sceneRef;
            if (scene == null || scene.IsDisposed)
            {
                return;
            }

            var currentScene = scene.GetComponent<CurrentScenesComponent>().Scene;
            if (currentScene == null || currentScene.IsDisposed)
            {
                return;
            }

            string mapName = currentScene.Name.GetSceneConfigName();

            if (mapName == "Home")
            {
                if (scene.GetComponent<LoadoutComponent>() == null)
                {
                    scene.AddComponent<LoadoutComponent>();
                }

                bool showSettlement = scene.GetComponent<SettlementClientComponent>() is { HasSettlement: true, PendingOpen: true };
                YIUIRootComponent yiuiRoot = scene.YIUIRoot();
                if (yiuiRoot == null)
                {
                    return;
                }

                EntityRef<YIUIRootComponent> yiuiRootRef = yiuiRoot;
                await yiuiRoot.OpenPanelAsync<LobbyPanelComponent>();

                scene = sceneRef;
                yiuiRoot = yiuiRootRef;
                if (scene == null || scene.IsDisposed)
                {
                    return;
                }

                if (showSettlement)
                {
                    await SettlementPanelHelper.TryOpenOrRefresh(scene);
                    scene = sceneRef;
                    if (scene == null || scene.IsDisposed)
                    {
                        return;
                    }
                }

                scene.YIUIMgr()?.ClosePanel<LoadingPanelComponent>();
            }
            else
            {
                var yiuiRoot = currentScene.GetComponent<YIUIRootComponent>();
                if (yiuiRoot == null)
                {
                    return;
                }

                EntityRef<YIUIRootComponent> yiuiRootRef = yiuiRoot;
                await yiuiRoot.OpenPanelAsync<MainPanelComponent>();
                yiuiRoot = yiuiRootRef;
                if (yiuiRoot == null || yiuiRoot.IsDisposed)
                {
                    return;
                }

                await yiuiRoot.OpenPanelAsync<HUDPanelComponent>();
                scene = sceneRef;
                if (scene == null || scene.IsDisposed)
                {
                    return;
                }

                scene.YIUIMgr()?.ClosePanel<LoadingPanelComponent>();
            }
        }
    }
}
