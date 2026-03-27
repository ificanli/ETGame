namespace ET.Server
{
    [NumericWatcher(SceneType.Map, NumericType.HP)]
    public class NumericChange_PlayerHp_RefreshMonsterDisplayLevel : INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player)
            {
                return;
            }

            bool wasAlive = args.Old > 0;
            bool isAlive = args.New > 0;
            if (wasAlive == isAlive)
            {
                return;
            }

            RogueUnitDisplayLevelHelper.RefreshMonsterDisplayLevels(unit.Scene(), true);
        }
    }
}
