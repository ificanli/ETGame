namespace ET.Server
{
    [ChildOf(typeof(RogueObjectiveComponent))]
    public class RogueObjective : Entity, IAwake
    {
        public int OptionId { get; set; }
        public int EffectGroupId { get; set; }
        public int ObjectiveId { get; set; }
        public int GoalValue { get; set; }
        public int Progress { get; set; }
        public bool Completed { get; set; }
        public bool RewardClaimed { get; set; }
    }

    [ComponentOf(typeof(Unit))]
    public class RogueObjectiveComponent : Entity, IAwake
    {
    }
}
