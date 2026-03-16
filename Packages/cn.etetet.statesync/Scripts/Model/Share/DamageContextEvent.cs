namespace ET
{
    /// <summary>
    /// 伤害应用前事件。订阅者可修改 DamageContext（增伤/减伤/设置 CanDie）。
    /// </summary>
    public struct RogueBeforeDamageApply
    {
        public DamageContext Context;
    }

    /// <summary>
    /// 伤害应用后事件。用于吸血、击杀奖励、任务进度等后置逻辑。
    /// </summary>
    public struct RogueAfterDamageApply
    {
        public DamageContext Context;
    }
}
