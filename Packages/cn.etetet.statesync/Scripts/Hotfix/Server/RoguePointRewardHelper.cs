namespace ET.Server
{
    public static class RoguePointRewardHelper
    {
        public static int TryGrantInteractGold(ECAPointComponent point, Unit player)
        {
            if (point == null || point.IsDisposed || player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return 0;
            }

            if (!FlowParamHelper.TryGetIntParam(point.Params, RogueECAPointParamKey.InteractGold, out int rewardGold) || rewardGold <= 0)
            {
                return 0;
            }

            RogueProgressComponent progress = player.GetComponent<RogueProgressComponent>();
            if (progress == null)
            {
                return 0;
            }

            bool rewardOnce = !FlowParamHelper.TryGetBoolParam(point.Params, RogueECAPointParamKey.RewardOnce, out bool configuredRewardOnce) || configuredRewardOnce;
            string rewardKey = BuildPointRewardKey(point);
            if (rewardOnce && HasClaimedReward(progress, rewardKey))
            {
                return 0;
            }

            int finalDelta = RogueGoldHelper.GetGoldDeltaBySource(progress, rewardGold, RogueGoldSourceType.RogueCard);
            RogueGoldHelper.AddGold(progress, finalDelta);
            if (rewardOnce)
            {
                progress.ClaimedPointRewardIds.Add(rewardKey);
            }

            RogueProgressHelper.SyncProgress(player, progress);
            return finalDelta;
        }

        private static bool HasClaimedReward(RogueProgressComponent progress, string rewardKey)
        {
            if (progress == null || progress.IsDisposed || string.IsNullOrWhiteSpace(rewardKey) || progress.ClaimedPointRewardIds == null)
            {
                return false;
            }

            foreach (string claimedRewardId in progress.ClaimedPointRewardIds)
            {
                if (string.Equals(claimedRewardId, rewardKey))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildPointRewardKey(ECAPointComponent point)
        {
            return string.IsNullOrWhiteSpace(point?.PointId) ? string.Empty : point.PointId;
        }
    }
}
