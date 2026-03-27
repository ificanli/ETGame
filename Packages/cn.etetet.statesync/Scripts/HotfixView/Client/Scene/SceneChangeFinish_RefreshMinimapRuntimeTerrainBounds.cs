namespace ET.Client
{
    [Event(SceneType.Client)]
    public class SceneChangeFinish_RefreshMinimapRuntimeTerrainBounds : AEvent<Scene, SceneChangeFinish>
    {
        protected override async ETTask Run(Scene root, SceneChangeFinish args)
        {
            Scene currentScene = root.GetComponent<CurrentScenesComponent>()?.Scene;
            MinimapRuntimeComponent runtime = currentScene?.GetComponent<MinimapRuntimeComponent>();
            runtime?.RefreshWorldBoundsFromTerrain();
            await ETTask.CompletedTask;
        }
    }
}
