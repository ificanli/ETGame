namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class UnitPetComponent: Entity, IAwake
    {
        public long PetId { get; set; }
    }
}