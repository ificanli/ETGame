using UnityEngine;

namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponFireHandler : MessageHandler<Scene, M2C_WeaponFire>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponFire message)
        {
            global::ET.WeaponConfig weaponConfig = global::ET.WeaponConfigCategory.Instance.GetOrDefault(message.WeaponId);
            if (weaponConfig == null)
            {
                Log.Warning($"[WeaponFireTrace][Client] missing config, weaponId={message.WeaponId}, caster={message.CasterUnitId}, target={message.TargetUnitId}");
                await ETTask.CompletedTask;
                return;
            }

            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Warning($"[WeaponFireTrace][Client] missing unit component, weaponId={message.WeaponId}");
                await ETTask.CompletedTask;
                return;
            }

            Unit caster = unitComponent.Get(message.CasterUnitId);
            Unit target = unitComponent.Get(message.TargetUnitId);
            if (caster == null || target == null)
            {
                Log.Warning($"[WeaponFireTrace][Client] missing caster or target, caster={message.CasterUnitId}, target={message.TargetUnitId}, casterNull={caster == null}, targetNull={target == null}");
                await ETTask.CompletedTask;
                return;
            }

            AnimatorComponent animatorComponent = caster.GetComponent<AnimatorComponent>();
            animatorComponent?.SetTrigger("Fire");
            Log.Info($"[WeaponFireTrace][Client] fire message received, caster={message.CasterUnitId}, target={message.TargetUnitId}, weaponId={message.WeaponId}, bulletCount={message.BulletCount}, effect={weaponConfig.ProjectileEffect}");

            int bulletCount = message.BulletCount > 0 ? message.BulletCount : 1;
            FireLockType lockType = (FireLockType)message.FireLockTypeId;
            for (int i = 0; i < bulletCount; i++)
            {
                CreateProjectileAsync(root, caster, target, weaponConfig, lockType).Coroutine();
            }

            await ETTask.CompletedTask;
        }

        private static async ETTask CreateProjectileAsync(Scene root, Unit caster, Unit target, global::ET.WeaponConfig weaponConfig, FireLockType lockType)
        {
            if (string.IsNullOrEmpty(weaponConfig.ProjectileEffect))
            {
                Log.Warning($"[WeaponFireTrace][Client] empty projectile effect, weaponId={weaponConfig.Id}");
                return;
            }

            Scene currentScene = root.CurrentScene();
            ResourcesLoaderComponent resourcesLoader = currentScene?.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                Log.Warning($"[WeaponFireTrace][Client] missing resources loader, weaponId={weaponConfig.Id}");
                return;
            }

            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> casterRef = caster;
            EntityRef<Unit> targetRef = target;
            GameObject prefab = await resourcesLoader.LoadAssetAsync<GameObject>(weaponConfig.ProjectileEffect);

            root = rootRef;
            caster = casterRef;
            target = targetRef;
            if (root == null || caster == null || target == null || prefab == null)
            {
                Log.Warning($"[WeaponFireTrace][Client] projectile load failed, weaponId={weaponConfig.Id}, prefabNull={prefab == null}, casterNull={caster == null}, targetNull={target == null}");
                return;
            }

            BindPoint targetBindPoint = weaponConfig.ToBindPoint(weaponConfig.TargetBindPointId, BindPoint.Hitted);

            WeaponViewComponent weaponViewComponent = caster.GetComponent<WeaponViewComponent>();
            Vector3 startPos = weaponViewComponent != null
                ? WeaponViewComponentSystem.GetFirePoint(weaponViewComponent, caster, weaponConfig)
                : GetBindPosition(caster, weaponConfig.ToBindPoint(weaponConfig.MuzzleBindPointId, BindPoint.Bullet));

            if (startPos == Vector3.zero)
            {
                BindPoint casterBindPoint = weaponConfig.ToBindPoint(weaponConfig.CasterBindPointId, BindPoint.Attack);
                startPos = GetBindPosition(caster, casterBindPoint);
            }

            Transform targetBindTransform = GetBindTransform(target, targetBindPoint);
            Vector3 lockedTargetPos = targetBindTransform != null ? targetBindTransform.position : GetBindPosition(target, BindPoint.Base);

            GameObject effect = UnityEngine.Object.Instantiate(prefab, startPos, Quaternion.identity);
            Log.Info($"[WeaponFireTrace][Client] projectile instantiated, weaponId={weaponConfig.Id}, effect={prefab.name}, start={startPos}, target={lockedTargetPos}, lockType={lockType}");
            if (effect == null)
            {
                return;
            }

            Transform effectRoot = GameObject.Find("/Global/Unit")?.transform;
            if (effectRoot != null && effect != null)
            {
                effect.transform.SetParent(effectRoot);
            }

            float speed = weaponConfig.ProjectileSpeed > 0 ? weaponConfig.ProjectileSpeed : 30f;
            int durationMs = weaponConfig.ProjectileDurationMs > 0 ? weaponConfig.ProjectileDurationMs : 2000;
            float endTime = Time.time + durationMs / 1000f;
            TimerComponent timerComponent = root.TimerComponent;

            while (Time.time < endTime)
            {
                if (effect == null)
                {
                    return;
                }

                Vector3 destination = lockedTargetPos;
                if (lockType == FireLockType.ForcedTarget && targetBindTransform != null)
                {
                    destination = targetBindTransform.position;
                }

                Vector3 delta = destination - effect.transform.position;
                if (delta.sqrMagnitude <= 0.09f)
                {
                    break;
                }

                Vector3 dir = delta.normalized;
                effect.transform.position += dir * speed * Time.deltaTime;
                effect.transform.forward = dir;

                await timerComponent.WaitAsync(1);
            }

            if (effect != null)
            {
                UnityEngine.Object.Destroy(effect);
            }
        }

        private static Transform GetBindTransform(Unit unit, BindPoint bindPoint)
        {
            GameObject go = unit.GetComponent<GameObjectComponent>()?.GameObject;
            if (go == null)
            {
                return null;
            }

            BindPointComponent bindPointComponent = go.GetComponent<BindPointComponent>();
            if (bindPointComponent != null && bindPointComponent.GetBindPoints().TryGetValue(bindPoint, out Transform transform) && transform != null)
            {
                return transform;
            }

            return go.transform;
        }

        private static Vector3 GetBindPosition(Unit unit, BindPoint bindPoint)
        {
            Transform transform = GetBindTransform(unit, bindPoint);
            return transform != null ? transform.position : Vector3.zero;
        }
    }
}
