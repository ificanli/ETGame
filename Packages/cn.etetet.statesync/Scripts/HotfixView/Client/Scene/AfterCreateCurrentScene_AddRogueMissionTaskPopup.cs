namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddRogueMissionTaskPopup : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            if (scene.GetComponent<RogueMissionTaskPopupComponent>() == null)
            {
                scene.AddComponent<RogueMissionTaskPopupComponent>();
            }

            await ETTask.CompletedTask;
        }
    }
}
