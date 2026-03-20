using System.Globalization;

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

            if (TryGetOwnerPlayerId(point, out long ownerPlayerId) && ownerPlayerId > 0 && ownerPlayerId != player.Id)
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
            if (rewardOnce)
            {
                ConsumeRewardPoint(point);
            }

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

        private static bool TryGetOwnerPlayerId(ECAPointComponent point, out long ownerPlayerId)
        {
            ownerPlayerId = 0;
            if (!FlowParamHelper.TryGetStringParam(point?.Params, RogueECAPointParamKey.OwnerPlayerId, out string rawOwnerPlayerId))
            {
                return false;
            }

            return long.TryParse(rawOwnerPlayerId, NumberStyles.Integer, CultureInfo.InvariantCulture, out ownerPlayerId);
        }

        private static void ConsumeRewardPoint(ECAPointComponent point)
        {
            if (point == null || point.IsDisposed)
            {
                return;
            }

            point.IsActive = false;
            HideInteractHints(point);

            bool destroyAfterReward = FlowParamHelper.TryGetBoolParam(
                point.Params,
                RogueECAPointParamKey.DestroyAfterReward,
                out bool configuredDestroyAfterReward) && configuredDestroyAfterReward;
            if (!destroyAfterReward)
            {
                return;
            }

            Unit pointUnit = point.GetParent<Unit>();
            pointUnit?.Dispose();
        }

        private static void HideInteractHints(ECAPointComponent point)
        {
            Scene scene = point?.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            point.CleanupInvalidPlayersInRange(unitComponent);
            foreach (long playerId in point.PlayersInRange)
            {
                Unit player = unitComponent.Get(playerId);
                if (player == null || player.IsDisposed)
                {
                    continue;
                }

                ContainerRuntimeHelper.SendInteractHint(point, player, false);
            }
        }
    }
}
