namespace ET.Server
{
    public struct UnitBeforeTransfer
    {
        public EntityRef<Unit> Unit;
        public string TargetMapName;
        public bool ChangeScene;
    }

    /// <summary>
    /// 玩家进入地图并完成玩法运行时初始化后的事件。
    /// </summary>
    public struct PlayerEnterMap
    {
        public EntityRef<Unit> Unit;
        public string MapName;
    }
}
