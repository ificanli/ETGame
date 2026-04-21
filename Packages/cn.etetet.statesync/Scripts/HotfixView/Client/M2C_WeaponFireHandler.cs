using UnityEngine;

namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponFireHandler : MessageHandler<Scene, M2C_WeaponFire>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponFire message)
        {
            long recvClientNow = TimeInfo.Instance.ClientNow();
            long recvServerNow = TimeInfo.Instance.ServerNow();
            global::ET.WeaponConfig weaponConfig = global::ET.WeaponConfigCategory.Instance.GetOrDefault(message.WeaponId);
            if (weaponConfig == null)
            {
                Log.Warning(
                    $"[WeaponFireTrace][ClientRecv] clientNow={recvClientNow}, serverNow={recvServerNow}, weaponId={message.WeaponId}, caster={message.CasterUnitId}, target={message.TargetUnitId}, result=missing_config");
                Log.Warning($"[WeaponFireTrace][Client] missing config, weaponId={message.WeaponId}, caster={message.CasterUnitId}, target={message.TargetUnitId}");
                await ETTask.CompletedTask;
                return;
            }

            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Warning(
                    $"[WeaponFireTrace][ClientRecv] clientNow={recvClientNow}, serverNow={recvServerNow}, weaponId={message.WeaponId}, caster={message.CasterUnitId}, target={message.TargetUnitId}, result=unit_component_missing");
                Log.Warning($"[WeaponFireTrace][Client] missing unit component, weaponId={message.WeaponId}");
                await ETTask.CompletedTask;
                return;
            }

            Unit caster = unitComponent.Get(message.CasterUnitId);
            Unit target = unitComponent.Get(message.TargetUnitId);
            if (caster == null || target == null)
            {
                Log.Warning(
                    $"[WeaponFireTrace][ClientRecv] clientNow={recvClientNow}, serverNow={recvServerNow}, weaponId={message.WeaponId}, caster={message.CasterUnitId}, target={message.TargetUnitId}, result=unit_missing, casterNull={caster == null}, targetNull={target == null}");
                Log.Warning($"[WeaponFireTrace][Client] missing caster or target, caster={message.CasterUnitId}, target={message.TargetUnitId}, casterNull={caster == null}, targetNull={target == null}");
                await ETTask.CompletedTask;
                return;
            }

            // 射击时面向目标
            Vector3 toTarget = (Vector3)(target.Position - caster.Position);
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                caster.Rotation = Quaternion.LookRotation(toTarget);
                GameObjectComponent casterGo = caster.GetComponent<GameObjectComponent>();
                if (casterGo?.Transform != null)
                {
                    casterGo.Transform.rotation = Quaternion.LookRotation(toTarget);
                }
            }
            AnimatorComponent animatorComponent = caster.GetComponent<AnimatorComponent>();
            WeaponViewComponent weaponViewComponent = caster.GetComponent<WeaponViewComponent>();
            if (weaponConfig.FireAnimationType == 2)
            {
                // 连发武器：每次开火都刷新停火超时，没有后续开火消息时自动回落到持枪待机/移动
                if (weaponViewComponent == null)
                {
                    weaponViewComponent = caster.AddComponent<WeaponViewComponent>();
                }

                weaponViewComponent.StartRapidFireAnimation(root, weaponConfig.AttackIntervalMs);
            }
            else
            {
                // 单发武器：每次开火触发一次完整射击动画
                animatorComponent?.SetTrigger("FireSingle");
            }
            Log.Info(
                $"[WeaponFireTrace][ClientRecv] clientNow={recvClientNow}, serverNow={recvServerNow}, caster={message.CasterUnitId}, target={message.TargetUnitId}, weaponId={message.WeaponId}, slot={message.SlotIndex}, bulletCount={message.BulletCount}, fireAnimationType={weaponConfig.FireAnimationType}, effect={weaponConfig.ProjectileEffect}");
            Log.Info($"[WeaponFireTrace][Client] fire message received, caster={message.CasterUnitId}, target={message.TargetUnitId}, weaponId={message.WeaponId}, bulletCount={message.BulletCount}, effect={weaponConfig.ProjectileEffect}");

            // 枪口火焰
            if (!string.IsNullOrEmpty(weaponConfig.MuzzleFlashEffect))
            {
                SpawnMuzzleFlashAsync(root, caster, weaponConfig).Coroutine();
            }

            int bulletCount = message.BulletCount > 0 ? message.BulletCount : 1;
            FireLockType lockType = (FireLockType)message.FireLockTypeId;
            for (int i = 0; i < bulletCount; i++)
            {
                CreateProjectileAsync(root, caster, target, weaponConfig, lockType, i, bulletCount).Coroutine();
            }

            await ETTask.CompletedTask;
        }

        private static async ETTask CreateProjectileAsync(
            Scene root,
            Unit caster,
            Unit target,
            global::ET.WeaponConfig weaponConfig,
            FireLockType lockType,
            int projectileIndex,
            int projectileCount)
        {
            long projectileBeginClientNow = TimeInfo.Instance.ClientNow();
            long projectileBeginServerNow = TimeInfo.Instance.ServerNow();
            if (string.IsNullOrEmpty(weaponConfig.ProjectileEffect))
            {
                Log.Warning(
                    $"[WeaponFireTrace][ClientProjectileBegin] clientNow={projectileBeginClientNow}, serverNow={projectileBeginServerNow}, weaponId={weaponConfig.Id}, caster={caster?.Id ?? 0}, target={target?.Id ?? 0}, result=empty_effect");
                Log.Warning($"[WeaponFireTrace][Client] empty projectile effect, weaponId={weaponConfig.Id}");
                return;
            }

            Scene currentScene = root.CurrentScene();
            ResourcesLoaderComponent resourcesLoader = currentScene?.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                Log.Warning(
                    $"[WeaponFireTrace][ClientProjectileBegin] clientNow={projectileBeginClientNow}, serverNow={projectileBeginServerNow}, weaponId={weaponConfig.Id}, caster={caster?.Id ?? 0}, target={target?.Id ?? 0}, effect={weaponConfig.ProjectileEffect}, result=resources_loader_missing");
                Log.Warning($"[WeaponFireTrace][Client] missing resources loader, weaponId={weaponConfig.Id}");
                return;
            }

            Log.Info(
                $"[WeaponFireTrace][ClientProjectileBegin] clientNow={projectileBeginClientNow}, serverNow={projectileBeginServerNow}, weaponId={weaponConfig.Id}, caster={caster.Id}, target={target.Id}, effect={weaponConfig.ProjectileEffect}, lockType={lockType}");
            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> casterRef = caster;
            EntityRef<Unit> targetRef = target;
            long loadStartClientNow = TimeInfo.Instance.ClientNow();
            GameObject prefab = await resourcesLoader.LoadAssetAsync<GameObject>(weaponConfig.ProjectileEffect);
            long loadDoneClientNow = TimeInfo.Instance.ClientNow();

            root = rootRef;
            caster = casterRef;
            target = targetRef;
            if (root == null || caster == null || target == null || prefab == null)
            {
                Log.Warning(
                    $"[WeaponFireTrace][ClientProjectileLoad] clientNow={loadDoneClientNow}, serverNow={TimeInfo.Instance.ServerNow()}, weaponId={weaponConfig.Id}, effect={weaponConfig.ProjectileEffect}, loadCostMs={loadDoneClientNow - loadStartClientNow}, prefabNull={prefab == null}, casterNull={caster == null}, targetNull={target == null}, rootNull={root == null}, result=load_failed");
                Log.Warning($"[WeaponFireTrace][Client] projectile load failed, weaponId={weaponConfig.Id}, prefabNull={prefab == null}, casterNull={caster == null}, targetNull={target == null}");
                return;
            }

            Log.Info(
                $"[WeaponFireTrace][ClientProjectileLoad] clientNow={loadDoneClientNow}, serverNow={TimeInfo.Instance.ServerNow()}, weaponId={weaponConfig.Id}, caster={caster.Id}, target={target.Id}, effect={weaponConfig.ProjectileEffect}, prefab={prefab.name}, loadCostMs={loadDoneClientNow - loadStartClientNow}");

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

            Vector3 projectileTargetPos = ResolveProjectileTargetPosition(startPos, lockedTargetPos, lockType, weaponConfig.SpreadAngle, projectileIndex, projectileCount);
            Vector3 initialDirection = projectileTargetPos - startPos;
            Quaternion initialRotation = initialDirection.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(initialDirection.normalized)
                : Quaternion.identity;

            GameObject effect = UnityEngine.Object.Instantiate(prefab, startPos, initialRotation);
            long projectileDoneClientNow = TimeInfo.Instance.ClientNow();
            Log.Info(
                $"[WeaponFireTrace][ClientProjectileDone] clientNow={projectileDoneClientNow}, serverNow={TimeInfo.Instance.ServerNow()}, weaponId={weaponConfig.Id}, caster={caster.Id}, target={target.Id}, effect={prefab.name}, start={startPos}, targetPos={projectileTargetPos}, lockType={lockType}, projectileIndex={projectileIndex}, projectileCount={projectileCount}, loadCostMs={loadDoneClientNow - loadStartClientNow}, totalCostMs={projectileDoneClientNow - projectileBeginClientNow}");
            Log.Info($"[WeaponFireTrace][Client] projectile instantiated, weaponId={weaponConfig.Id}, effect={prefab.name}, start={startPos}, target={projectileTargetPos}, lockType={lockType}, projectileIndex={projectileIndex}, projectileCount={projectileCount}");
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

                Vector3 destination = projectileTargetPos;
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

        private static Vector3 ResolveProjectileTargetPosition(
            Vector3 startPos,
            Vector3 lockedTargetPos,
            FireLockType lockType,
            float spreadAngle,
            int projectileIndex,
            int projectileCount)
        {
            if (lockType != FireLockType.Direction || projectileCount <= 1 || spreadAngle <= 0f)
            {
                return lockedTargetPos;
            }

            Vector3 baseDelta = lockedTargetPos - startPos;
            if (baseDelta.sqrMagnitude <= 0.0001f)
            {
                return lockedTargetPos;
            }

            float halfSpread = spreadAngle * 0.5f;
            float t = projectileCount > 1 ? projectileIndex / (float)(projectileCount - 1) : 0.5f;
            float angleOffset = Mathf.Lerp(-halfSpread, halfSpread, t);
            Vector3 scatteredDirection = Quaternion.AngleAxis(angleOffset, Vector3.up) * baseDelta.normalized;
            return startPos + scatteredDirection * baseDelta.magnitude;
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

        private static async ETTask SpawnMuzzleFlashAsync(Scene root, Unit caster, global::ET.WeaponConfig weaponConfig)
        {
            Scene currentScene = root.CurrentScene();
            ResourcesLoaderComponent resourcesLoader = currentScene?.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null) return;

            WeaponViewComponent weaponView = caster.GetComponent<WeaponViewComponent>();
            Vector3 muzzlePos = weaponView != null
                ? WeaponViewComponentSystem.GetFirePoint(weaponView, caster, weaponConfig)
                : GetBindPosition(caster, weaponConfig.ToBindPoint(weaponConfig.MuzzleBindPointId, BindPoint.Bullet));
            Quaternion muzzleRot = caster.GetComponent<GameObjectComponent>()?.GameObject?.transform.rotation ?? Quaternion.identity;

            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> casterRef = caster;
            GameObject prefab = await resourcesLoader.LoadAssetAsync<GameObject>(weaponConfig.MuzzleFlashEffect);

            root = rootRef;
            caster = casterRef;
            if (root == null || caster == null || prefab == null) return;

            GameObject flash = UnityEngine.Object.Instantiate(prefab, muzzlePos, muzzleRot);
            float durationSec = (weaponConfig.MuzzleFlashDurationMs > 0 ? weaponConfig.MuzzleFlashDurationMs : 100) / 1000f;
            UnityEngine.Object.Destroy(flash, durationSec);
        }

    }
}
