namespace ET.Server
{
    [Event(SceneType.Map)]
    public class ChangePosition_RogueTrapMaster : AEvent<Scene, ChangePosition>
    {
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            RogueTrapMasterStateComponent trapMasterState = unit?.GetComponent<RogueTrapMasterStateComponent>();
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player || trapMasterState == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            trapMasterState.OnMoved();
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Map)]
    public class UnitWeaponFired_RogueTrapMaster : AEvent<Scene, UnitWeaponFired>
    {
        protected override async ETTask Run(Scene scene, UnitWeaponFired args)
        {
            Unit caster = args.Caster;
            Unit target = args.Target;
            RogueTrapMasterStateComponent trapMasterState = caster?.GetComponent<RogueTrapMasterStateComponent>();
            if (caster == null || caster.IsDisposed || caster.UnitType != UnitType.Player || target == null || target.IsDisposed || trapMasterState == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (!trapMasterState.CanTrigger())
            {
                await ETTask.CompletedTask;
                return;
            }

            if (args.WeaponId <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.Get(args.WeaponId);
            if (weaponConfig == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            float damage = args.Damage;
            if (damage <= 0f)
            {
                WeaponComponent weaponComponent = caster.GetComponent<WeaponComponent>();
                if (weaponComponent != null)
                {
                    damage = weaponComponent.GetEffectiveDamage(args.SlotIndex);
                }
            }

            if (damage <= 0f)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (scene.GetComponent<BulletTickComponent>() == null)
            {
                scene.AddComponent<BulletTickComponent>();
            }

            int bulletCount = trapMasterState.EffectiveBulletCount;
            if (bulletCount <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            BulletHelper.CreateScatterBullets(
                scene,
                caster,
                target,
                damage,
                (FireLockType)weaponConfig.FireLockTypeId,
                bulletCount,
                weaponConfig.SpreadAngle,
                args.WeaponId);
            trapMasterState.MarkTriggered();

            await ETTask.CompletedTask;
        }
    }
}
