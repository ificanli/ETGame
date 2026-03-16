namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddMinimapRuntime : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            if (scene.GetComponent<MinimapRuntimeComponent>() == null)
            {
                scene.AddComponent<MinimapRuntimeComponent>();
            }

            await ETTask.CompletedTask;
        }
    }
}
