namespace ET.Client
{
    [EntitySystemOf(typeof(SettlementClientComponent))]
    public static partial class SettlementClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SettlementClientComponent self)
        {
            self.ResetRuntime();
        }

        [EntitySystem]
        private static void Destroy(this SettlementClientComponent self)
        {
            self.ResetRuntime();
        }

        public static void ResetRuntime(this SettlementClientComponent self)
        {
            self.HasSettlement = false;
            self.PendingOpen = false;
            self.IsSuccess = false;
            self.TotalWealth = 0;
            self.KillNum = 0;
        }

        public static void SetSettlement(this SettlementClientComponent self, bool isSuccess, long totalWealth, int killNum)
        {
            self.HasSettlement = true;
            self.PendingOpen = true;
            self.IsSuccess = isSuccess;
            self.TotalWealth = totalWealth < 0 ? 0 : totalWealth;
            self.KillNum = killNum < 0 ? 0 : killNum;
        }

        public static void MarkShown(this SettlementClientComponent self)
        {
            self.PendingOpen = false;
        }
    }
}
