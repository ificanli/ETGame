namespace ET.Client
{
    /// <summary>
    /// 客户端结算运行时组件，挂在根 Scene 上，承载撤离/死亡结算展示数据。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class SettlementClientComponent : Entity, IAwake, IDestroy
    {
        public bool HasSettlement { get; set; }

        public bool PendingOpen { get; set; }

        public bool IsSuccess { get; set; }

        public long TotalWealth { get; set; }

        public int KillNum { get; set; }
    }
}
