namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class RogueMissionTaskMonsterComponent : Entity, IAwake
    {
        public long TaskPointUnitId { get; set; }
        public long OwnerPlayerId { get; set; }
    }
}
