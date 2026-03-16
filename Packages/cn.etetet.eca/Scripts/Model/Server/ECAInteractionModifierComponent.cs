namespace ET.Server
{
    /// <summary>
    /// 玩家交互范围修正组件（米）。
    /// 由上层玩法（如肉鸽 Buff）写入，ECA 范围判定统一读取。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class ECAInteractionModifierComponent : Entity, IAwake, IDestroy
    {
        public float InteractRangeBonus;
        public int SilentSearchRefCount;
    }
}
