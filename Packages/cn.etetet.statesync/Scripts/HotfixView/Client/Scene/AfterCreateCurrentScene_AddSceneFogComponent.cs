namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddSceneFogComponent : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            if (scene.GetComponent<SceneFogComponent>() == null)
            {
                scene.AddComponent<SceneFogComponent>();
            }

            await ETTask.CompletedTask;
        }
    }
}
