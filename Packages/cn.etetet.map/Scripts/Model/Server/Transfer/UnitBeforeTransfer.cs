namespace ET.Server
{
    public struct UnitBeforeTransfer
    {
        public EntityRef<Unit> Unit;
        public string TargetMapName;
        public bool ChangeScene;
    }
}
