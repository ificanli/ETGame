namespace ET.Client
{
    public struct EventMain_ShowHPView
    {
        public EntityRef<HPViewComponent> HPView;
    }

    /// <summary>摇杆输入事件，DirX/DirZ 为 0 表示停止</summary>
    public struct EventMain_JoystickInput
    {
        public long SceneInstanceId;
        public float DirX;
        public float DirZ;
    }
}