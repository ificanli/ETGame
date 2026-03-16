namespace ET.Client
{
    /// <summary>
    /// 摇杆移动同步事件（客户端）。
    /// 用于在视图层同步移动动画参数，避免 Hotfix 直接依赖视图组件。
    /// </summary>
    public struct EventJoystickMoveSynced
    {
        public EntityRef<Scene> Scene;
        public long UnitId;
        public float MoveSpeed;
    }
}
