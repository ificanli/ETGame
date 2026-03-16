namespace ET.Server
{
    [Event(SceneType.Map)]
    public class BulletHitEvent_Broadcast : AEvent<Scene, BulletHitEvent>
    {
        protected override async ETTask Run(Scene scene, BulletHitEvent args)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            Unit caster = unitComponent.Get(args.CasterUnitId);
            Unit target = unitComponent.Get(args.TargetUnitId);
            Unit noticer = target ?? caster;
            if (noticer == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            M2C_WeaponHit hitMsg = M2C_WeaponHit.Create();
            hitMsg.CasterUnitId = args.CasterUnitId;
            hitMsg.TargetUnitId = args.TargetUnitId;
            hitMsg.WeaponId = args.WeaponId;
            MapMessageHelper.NoticeClient(noticer, hitMsg, NoticeType.Broadcast);

            await ETTask.CompletedTask;
        }
    }
}
