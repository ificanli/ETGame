namespace ET.Server
{
    /// <summary>
    /// 地图加载完成后初始化战术视野驱动。
    /// </summary>
    [Event(SceneType.Map)]
    public class MapLoadFinishEvent_InitTacticalVision : AEvent<Scene, MapLoadFinishEvent>
    {
        protected override ETTask Run(Scene scene, MapLoadFinishEvent args)
        {
            Scene mapScene = args.Scene;
            if (mapScene == null || mapScene.IsDisposed)
            {
                return ETTask.CompletedTask;
            }

            if (mapScene.GetComponent<TacticalVisionComponent>() == null)
            {
                mapScene.AddComponent<TacticalVisionComponent>();
            }

            return ETTask.CompletedTask;
        }
    }
}
