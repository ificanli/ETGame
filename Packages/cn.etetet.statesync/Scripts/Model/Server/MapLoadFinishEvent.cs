namespace ET.Server
{
    /// <summary>
    /// 地图加载完成事件，用于触发刷怪等初始化逻辑
    /// </summary>
    public struct MapLoadFinishEvent
    {
        public EntityRef<Scene> Scene;
    }
}
