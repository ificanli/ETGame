namespace ET.Client
{
    [Event(SceneType.WOW)]
    public class EnterMapFinish_CloseLoadingPanel : AEvent<Scene, EnterMapFinish>
    {
        protected override async ETTask Run(Scene scene, EnterMapFinish a)
        {
            await scene.Root().GetComponent<TimerComponent>().WaitAsync(2000);
            YIUIMgrComponent.Inst.ClosePanel<LoadingPanelComponent>();
        }
    }
}