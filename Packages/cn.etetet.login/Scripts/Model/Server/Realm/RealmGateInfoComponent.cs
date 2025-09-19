namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public class RealmGateInfoComponent: Entity, IAwake, IDestroy
    {
        public MultiMapSet<int, string> ZoneGates = new();
    }
}