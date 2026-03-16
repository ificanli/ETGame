namespace ET.Server
{
    /// <summary>
    /// 进入Home时的加载逻辑
    /// </summary>
    public static class HomeEnterHelper
    {
        /// <summary>
        /// 玩家进入Home时调用，加载或创建基地数据
        /// </summary>
        public static void OnEnterHome(Unit unit)
        {
            // 如果已有PlayerHomeComponent则跳过（不应发生，因为不走ITransfer）
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp != null)
            {
                return;
            }

            // 创建默认基地数据（首次进入）
            homeComp = unit.AddComponent<PlayerHomeComponent>();
            homeComp.HomeVersion = 1;
            homeComp.LastSettleTime = TimeInfo.Instance.ServerNow();

            Log.Debug($"HomeEnterHelper: created default PlayerHomeComponent for unit {unit.Id}");
        }
    }
}
