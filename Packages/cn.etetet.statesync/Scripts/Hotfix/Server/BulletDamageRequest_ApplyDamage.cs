namespace ET.Server
{
    /// <summary>
    /// 服务端处理子弹伤害请求，走 DamageContextHelper 统一管线。
    /// </summary>
    [Event(SceneType.Map)]
    public class BulletDamageRequest_ApplyDamage : AEvent<Scene, BulletDamageRequest>
    {
        protected override async ETTask Run(Scene scene, BulletDamageRequest a)
        {
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            Unit attacker = unitComponent.Get(a.CasterUnitId);
            Unit target = unitComponent.Get(a.TargetUnitId);
            if (target == null || target.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            DamageContextHelper.ApplyDamage(
                scene,
                attacker,
                target,
                a.BaseDamage,
                weaponId: a.WeaponId,
                isBullet: true);

            await ETTask.CompletedTask;
        }
    }
}
