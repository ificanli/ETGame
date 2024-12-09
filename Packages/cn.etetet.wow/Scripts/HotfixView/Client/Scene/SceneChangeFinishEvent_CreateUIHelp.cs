namespace ET.Client
{
    [Event(SceneType.WOW)]
    public class SceneChangeFinishEvent_CreateUIHelp : AEvent<Scene, SceneChangeFinish>
    {
        protected override async ETTask Run(Scene scene, SceneChangeFinish args)
        {
            var currentScene = scene.GetComponent<CurrentScenesComponent>().Scene;
            var yiuiRoot     = currentScene.GetComponent<YIUIRootComponent>();
            await yiuiRoot.OpenPanelAsync<MainPanelComponent>();
            await yiuiRoot.OpenPanelAsync<HUDPanelComponent>();
        }
    }
}