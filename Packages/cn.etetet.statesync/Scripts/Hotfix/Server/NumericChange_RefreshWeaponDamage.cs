namespace ET.Server
{
    /// <summary>
    /// 攻击力变化时自动刷新武器有效伤害。
    /// </summary>
    [Event(SceneType.Map)]
    public class NumericChange_RefreshWeaponDamage : AEvent<Scene, NumbericChange>
    {
        protected override async ETTask Run(Scene scene, NumbericChange a)
        {
            if (a.NumericType != NumericType.Attack)
            {
                await ETTask.CompletedTask;
                return;
            }

            Unit unit = a.Unit;
            if (unit == null || unit.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            WeaponComponent weaponComp = unit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit);
            await ETTask.CompletedTask;
        }
    }
}
