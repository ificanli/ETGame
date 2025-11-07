namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class UnitGateInfoComponent: Entity, IAwake, ITransfer
    {
        public ActorId ActorId;
    }
}