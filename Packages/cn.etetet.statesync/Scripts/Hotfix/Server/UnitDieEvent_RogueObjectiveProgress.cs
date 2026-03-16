namespace ET.Server
{
    /// <summary>
    /// 击杀事件驱动任务进度推进。
    /// 当玩家击杀怪物时，检查是否有 RogueObjective 需要推进。
    /// </summary>
    [Event(SceneType.Map)]
    public class UnitDieEvent_RogueObjectiveProgress : AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie a)
        {
            Unit killer = a.Unit;
            if (killer == null || killer.IsDisposed || killer.UnitType != UnitType.Player)
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueObjectiveComponent objectiveComponent = killer.GetComponent<RogueObjectiveComponent>();
            if (objectiveComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            int targetUnitType = a.TargetUnitType;
            RogueObjectiveHelper.OnKill(killer, objectiveComponent, targetUnitType);

            await ETTask.CompletedTask;
        }
    }
}
