namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class PetComponent: Entity, IAwake
    {
        public long OwnerId { get; set; }
    }
}