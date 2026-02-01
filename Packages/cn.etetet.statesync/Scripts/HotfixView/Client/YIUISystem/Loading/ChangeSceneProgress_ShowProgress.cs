namespace ET.Client
{
    [Event(SceneType.Client)]
    public class ChangeSceneProgress_ShowProgress : AEvent<Scene, ChangeSceneProgress>
    {
        protected override async ETTask Run(Scene scene, ChangeSceneProgress a)
        {
            scene.YIUIMgr().GetPanel<LoadingPanelComponent>()?.u_DataProgress.SetValue(a.Progress);
            await ETTask.CompletedTask;
        }
    }
}