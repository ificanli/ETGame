namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddMinimapCaptureComponent : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            if (scene.GetComponent<MinimapCaptureComponent>() == null)
            {
                scene.AddComponent<MinimapCaptureComponent>();
            }

            await ETTask.CompletedTask;
        }
    }
}
