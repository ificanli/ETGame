namespace ET.Client
{
    [FriendOf(typeof(LoadingPanelComponent))]
    [Event(SceneType.WOW)]
    public class ChangeSceneProgress_ShowProgress : AEvent<Scene, ChangeSceneProgress>
    {
        protected override async ETTask Run(Scene scene, ChangeSceneProgress a)
        {
            YIUIMgrComponent.Inst.GetPanel<LoadingPanelComponent>()?.u_DataProgress.SetValue(a.Progress);
            await ETTask.CompletedTask;
        }
    }
}