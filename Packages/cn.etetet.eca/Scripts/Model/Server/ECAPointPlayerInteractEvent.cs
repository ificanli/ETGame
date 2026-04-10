namespace ET.Server
{
    public struct ECAPointPlayerEnterEvent
    {
        public EntityRef<ECAPointComponent> Point;
        public EntityRef<Unit> Player;
    }

    public struct ECAPointPlayerInteractEvent
    {
        public EntityRef<ECAPointComponent> Point;
        public EntityRef<Unit> Player;
    }
}
