namespace ET.Server
{
    public static class PhaseHelper
    {
        public static bool IsPhase(this Unit self, PhaseType phaseType)
        {
            AOIEntity aoiEntity = self.GetComponent<AOIEntity>();
            return (aoiEntity.Phase & phaseType) == phaseType;
        }

        public static void SetPhase(this Unit self, PhaseType phaseType)
        {
            AOIEntity aoiEntity = self.GetComponent<AOIEntity>();
            aoiEntity.Phase = phaseType;
            self.NumericComponent?.Set(NumericType.Phase, (long)aoiEntity.Phase);
        }

        public static void AddPhase(this Unit self, PhaseType phaseType)
        {
            AOIEntity aoiEntity = self.GetComponent<AOIEntity>();
            aoiEntity.Phase |= phaseType;
            self.NumericComponent?.Set(NumericType.Phase, (long)aoiEntity.Phase);
        }

        public static void RemovePhase(this Unit self, PhaseType phaseType)
        {
            AOIEntity aoiEntity = self.GetComponent<AOIEntity>();
            aoiEntity.Phase &= ~phaseType;
            self.NumericComponent?.Set(NumericType.Phase, (long)aoiEntity.Phase);
        }
    }
}