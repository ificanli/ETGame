using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 收取被动产出
    /// </summary>
    public static class HomeCollectHelper
    {
        // 默认被动产出: 每小时产出的物品 (configId, count)
        // TODO: 后续从 BuildingLevelConfig 配置表读取
        public const int DefaultOutputItemId = 1001;
        public const int DefaultOutputRate = 10; // 每小时产出数量
        public const int MinCollectInterval = 60 * 1000; // 最少收取间隔1分钟(ms)

        public static HomeCollectResult Collect(Unit unit, long buildingId)
        {
            PlayerHomeComponent homeComp = unit.GetComponent<PlayerHomeComponent>();
            if (homeComp == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNotInHomeScene };
            }

            HomeBuilding building = homeComp.GetChild<HomeBuilding>(buildingId);
            if (building == null)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeBuildingNotFound };
            }

            long now = TimeInfo.Instance.ServerNow();
            long elapsed = now - building.LastCollectTime;

            // 时间间隔太短，无产出
            if (elapsed < MinCollectInterval)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNothingToCollect };
            }

            // 按小时计算产出 (线性插值)
            int outputCount = (int)(elapsed * DefaultOutputRate / 3600000);
            if (outputCount <= 0)
            {
                return new HomeCollectResult { ErrorCode = ErrorCode.ERR_HomeNothingToCollect };
            }

            // 更新收取时间
            building.LastCollectTime = now;

            var result = new HomeCollectResult
            {
                ErrorCode = ErrorCode.ERR_Success,
                ItemConfigIds = new List<int> { DefaultOutputItemId },
                ItemCounts = new List<int> { outputCount }
            };

            Log.Debug($"HomeCollectHelper: collected {outputCount}x item {DefaultOutputItemId} from building {buildingId}");
            return result;
        }
    }

}
