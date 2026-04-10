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
            RogueTrapMasterStateComponent trapMasterState = caster?.GetComponent<RogueTrapMasterStateComponent>();
            if (caster == null || caster.IsDisposed || caster.UnitType != UnitType.Player || trapMasterState == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (!trapMasterState.CanAccumulateTrapShots())
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

            if (!trapMasterState.RegisterShotAndTryTriggerTrap())
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueTrapHelper.CreateTrap(scene, caster, trapMasterState, args.WeaponId, damage);

            await ETTask.CompletedTask;
        }
    }

    public static class RogueTrapHelper
    {
        public static void CreateTrap(Scene scene, Unit owner, RogueTrapMasterStateComponent trapMasterState, int weaponId, float damage)
        {
            if (scene == null ||
                scene.IsDisposed ||
                owner == null ||
                owner.IsDisposed ||
                trapMasterState == null ||
                trapMasterState.IsDisposed ||
                damage <= 0f)
            {
                return;
            }

            long trapDamage = (long)damage * trapMasterState.EffectiveTrapDamagePermille / 1000;
            if (trapDamage <= 0)
            {
                trapDamage = 1;
            }

            RogueTrapEntity trapEntity = scene.AddChild<RogueTrapEntity>();
            trapEntity.Initialize(
                owner,
                owner.Position,
                trapDamage,
                weaponId,
                trapMasterState.EffectiveTrapRadius,
                trapMasterState.EffectiveTrapTickIntervalMs,
                trapMasterState.EffectiveTrapLifetimeMs);
        }
    }
}
