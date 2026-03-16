namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitDieEvent_RogueExpGain : AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie a)
        {
            Unit killer = a.Unit;
            Unit target = a.Target;
            int targetUnitType = target != null && !target.IsDisposed ? (int)target.UnitType : a.TargetUnitType;
            if (target != null && !target.IsDisposed)
            {
                RogueProgressHelper.AddKillExp(killer, target);
            }
            else
            {
                RogueProgressHelper.AddKillExp(killer, targetUnitType);
            }

            if (killer != null && !killer.IsDisposed && killer.UnitType == UnitType.Player)
            {
                RogueKillRewardHelper.TryGrantKillGold(killer, targetUnitType);
            }

            await ETTask.CompletedTask;
        }
    }
}
