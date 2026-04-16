namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddMapPoiRuntime : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            if (scene.GetComponent<MapPoiRuntimeComponent>() == null)
            {
                scene.AddComponent<MapPoiRuntimeComponent>();
            }

            await ETTask.CompletedTask;
        }
    }
}
