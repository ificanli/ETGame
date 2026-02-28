namespace ET.Server
{
    [EntitySystemOf(typeof(PlayerStorageComponent))]
    public static partial class PlayerStorageComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerStorageComponent self)
        {
            self.LastEvacuationItems.Clear();
            self.LastEvacuationWealth = 0;
            self.TotalWealth = 0;
        }

        [EntitySystem]
        private static void Destroy(this PlayerStorageComponent self)
        {
            self.LastEvacuationItems.Clear();
        }

        /// <summary>
        /// 写入撤离结算数据
        /// </summary>
        public static void WriteEvacuationResult(this PlayerStorageComponent self, M2C_EvacuationSettlement settlement)
        {
            self.LastEvacuationItems.Clear();
            self.LastEvacuationWealth = settlement.TotalWealth;
            self.TotalWealth += settlement.TotalWealth;

            foreach (ItemData item in settlement.Items)
            {
                if (self.LastEvacuationItems.TryGetValue(item.ConfigId, out int existing))
                {
                    self.LastEvacuationItems[item.ConfigId] = existing + item.Count;
                }
                else
                {
                    self.LastEvacuationItems[item.ConfigId] = item.Count;
                }
            }

            Log.Info($"[PlayerStorage] wrote evacuation result: {self.LastEvacuationItems.Count} item types, wealth={self.LastEvacuationWealth}");
        }
    }
}
