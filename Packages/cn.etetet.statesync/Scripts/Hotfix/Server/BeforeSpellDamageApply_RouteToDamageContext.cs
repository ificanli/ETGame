namespace ET.Server
{
    /// <summary>
    /// 拦截 Spell 伤害，路由到统一 DamageContext 管线。
    /// 这样 Spell 伤害也能享受致命免死、战斗状态、击杀奖励等肉鸽机制。
    /// </summary>
    [Event(SceneType.Map)]
    public class BeforeSpellDamageApply_RouteToDamageContext : AEvent<Scene, BeforeSpellDamageApply>
    {
        protected override async ETTask Run(Scene scene, BeforeSpellDamageApply a)
        {
            a.Info.Intercepted = true;

            Unit attacker = a.Attacker;
            Unit target = a.Target;
            if (target == null || target.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            DamageContextHelper.ApplyDamage(
                scene,
                attacker,
                target,
                a.FinalDamage,
                weaponId: 0,
                isBullet: false,
                isCritical: false);

            await ETTask.CompletedTask;
        }
    }
}
