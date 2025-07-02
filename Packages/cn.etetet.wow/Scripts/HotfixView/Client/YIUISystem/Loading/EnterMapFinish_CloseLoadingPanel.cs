namespace ET.Client
{
    [Event(SceneType.Client)]
    public class EnterMapFinish_CloseLoadingPanel : AEvent<Scene, EnterMapFinish>
    {
        protected override async ETTask Run(Scene scene, EnterMapFinish a)
        {
            EntityRef<Scene> sceneRef = scene;
            await scene.Root().GetComponent<TimerComponent>().WaitAsync(2000);
            scene = sceneRef;
            scene.YIUIMgr().ClosePanel<LoadingPanelComponent>();
        }
    }
}