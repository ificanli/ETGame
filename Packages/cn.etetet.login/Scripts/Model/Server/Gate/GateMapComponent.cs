namespace ET.Server
{
    [ComponentOf(typeof(Player))]
    public class GateMapComponent: Entity, IAwake, IDestroy
    {
        public Fiber Fiber { get; set; }
    }
}