namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class MatchRobotComponent : Entity, IAwake, ITransfer
    {
        public int MatchRobotConfigId;
        public int HeroConfigId;
        public int MainWeaponConfigId;
        public int AIBuffConfigId;
        public int AutoChooseDelayMinMs;
        public int AutoChooseDelayMaxMs;
        public long AutoChooseScheduledSerial;
        public long AutoChooseCompletedSerial;
    }
}
