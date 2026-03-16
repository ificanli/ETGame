namespace ET.Server
{
    [Event(SceneType.Map)]
    public class BulletKillEvent_PublishUnitDie : AEvent<Scene, BulletKillEvent>
    {
        protected override async ETTask Run(Scene scene, BulletKillEvent args)
        {
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            Unit killer = unitComponent.Get(args.CasterUnitId);
            Unit target = unitComponent.Get(args.TargetUnitId);
            if (killer == null || killer.IsDisposed)
            {
                Log.Info($"[RogueExp] skip UnitDie publish from bullet kill: killer invalid, killerId={args.CasterUnitId}, targetId={args.TargetUnitId}");
                await ETTask.CompletedTask;
                return;
            }

            EventSystem.Instance.Publish(scene, new UnitDie
            {
                Unit = killer,
                Target = target,
                TargetId = args.TargetUnitId,
                TargetUnitType = args.TargetUnitType,
            });

            Log.Info($"[RogueExp] bullet kill converted to UnitDie, killerId={killer.Id}, killerType={killer.UnitType}, targetId={args.TargetUnitId}, targetType={(UnitType)args.TargetUnitType}, weaponId={args.WeaponId}");
            await ETTask.CompletedTask;
        }
    }
}
