namespace ET.Server
{
    [ChildOf(typeof(RogueEffectRuntimeComponent))]
    public class RogueEffectRuntime : Entity, IAwake
    {
        public int OptionId { get; set; }
        public int EffectGroupId { get; set; }
        public int ExecuteType { get; set; }
        public int RefId { get; set; }
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int EffectBuffConfigId { get; set; }
        public long AppliedBuffId { get; set; }

        /// <summary>ScaleModifier 实际应用的 HP 增量，移除时精确回退</summary>
        public long AppliedHpDelta { get; set; }

        /// <summary>OnKillStackAttack 当前叠加层数</summary>
        public int StackCount { get; set; }
    }

    [ComponentOf(typeof(Unit))]
    public class RogueEffectRuntimeComponent : Entity, IAwake
    {
    }
}
