namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitDieEvent_RogueMissionTask : AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie a)
        {
            Unit target = a.Target;
            if ((target == null || target.IsDisposed) && a.TargetId > 0)
            {
                target = scene.GetComponent<UnitComponent>()?.Get(a.TargetId);
            }

            RogueMissionTaskMonsterComponent monsterComponent = target?.GetComponent<RogueMissionTaskMonsterComponent>();
            if (monsterComponent == null || monsterComponent.IsDisposed || monsterComponent.TaskPointUnitId <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            long taskPointUnitId = monsterComponent.TaskPointUnitId;
            await RogueMissionTaskPointHelper.OnTaskMonsterKilledAsync(scene, taskPointUnitId);
        }
    }
}
