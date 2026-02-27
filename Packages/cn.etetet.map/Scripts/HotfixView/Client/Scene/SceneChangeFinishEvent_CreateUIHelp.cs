namespace ET.Client
{
    [Event(SceneType.Client)]
    public class SceneChangeFinishEvent_CreateUIHelp : AEvent<Scene, SceneChangeFinish>
    {
        protected override async ETTask Run(Scene scene, SceneChangeFinish args)
        {
            var currentScene = scene.GetComponent<CurrentScenesComponent>().Scene;
            string mapName = currentScene.Name.GetSceneConfigName();

            if (mapName == "Home")
            {
                await scene.YIUIRoot().OpenPanelAsync<LobbyPanelComponent>();
            }
            else
            {
                var yiuiRoot = currentScene.GetComponent<YIUIRootComponent>();
                EntityRef<YIUIRootComponent> yiuiRootRef = yiuiRoot;
                await yiuiRoot.OpenPanelAsync<MainPanelComponent>();
                yiuiRoot = yiuiRootRef;
                await yiuiRoot.OpenPanelAsync<HUDPanelComponent>();
            }
        }
    }
}