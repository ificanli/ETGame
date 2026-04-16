namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddHomeSceneRuntime : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            if (scene.Name.GetSceneConfigName() == "Home" && scene.GetComponent<HomeSceneRuntimeComponent>() == null)
            {
                scene.AddComponent<HomeSceneRuntimeComponent>();
            }

            await ETTask.CompletedTask;
        }
    }
}
