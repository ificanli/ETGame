namespace ET.Server
{
    public static class RogueKillRewardHelper
    {
        public static int TryGrantKillGold(Unit killer, int targetUnitType)
        {
            if (killer == null || killer.IsDisposed || killer.UnitType != UnitType.Player)
            {
                return 0;
            }

            RogueProgressComponent progress = killer.GetComponent<RogueProgressComponent>();
            int killGold = RogueGoldHelper.GetKillGoldBonus(killer, targetUnitType);
            if (progress == null || killGold <= 0)
            {
                return 0;
            }

            int finalDelta = RogueGoldHelper.GetGoldDeltaBySource(progress, killGold, RogueGoldSourceType.RogueCard);
            RogueGoldHelper.AddGold(progress, finalDelta);
            RogueProgressHelper.SyncProgress(killer, progress);
            return finalDelta;
        }
    }
}
