namespace ET.Server
{
    [NumericWatcher(SceneType.Map, NumericType.HP)]
    public class NumericChange_DeadRemoveMonster : INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            if (unit.UnitType != UnitType.Monster)
            {
                return;
            }

            if (args.New > 0 || args.Old <= 0)
            {
                return;
            }

            Log.Info($"[MonsterDeath] remove monster: unitId={unit.Id}, configId={unit.ConfigId}, oldHp={args.Old}, newHp={args.New}");

            foreach (AOIEntity viewerAoi in unit.GetBeSeePlayers().Values)
            {
                Unit viewer = viewerAoi?.Unit;
                if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
                {
                    continue;
                }

                MapMessageHelper.NoticeUnitRemove(viewer, unit);
            }

            unit.Scene()?.GetComponent<UnitComponent>()?.Remove(unit.Id);
        }
    }
}
