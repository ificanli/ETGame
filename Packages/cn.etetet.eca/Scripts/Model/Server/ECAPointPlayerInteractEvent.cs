namespace ET.Server
{
    public struct ECAPointPlayerInteractEvent
    {
        public EntityRef<ECAPointComponent> Point;
        public EntityRef<Unit> Player;
    }
}
