using UnityEngine;

namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponFireHandler : MessageHandler<Scene, M2C_WeaponFire>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponFire message)
        {
            WeaponVisualConfigComponent visualConfigComponent = root.GetComponent<WeaponVisualConfigComponent>();
            if (visualConfigComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (!visualConfigComponent.TryGetConfig(message.WeaponId, out WeaponVisualConfig visualConfig))
            {
                await ETTask.CompletedTask;
                return;
            }

            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            Unit caster = unitComponent.Get(message.CasterUnitId);
            Unit target = unitComponent.Get(message.TargetUnitId);
            if (caster == null || target == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            int bulletCount = message.BulletCount > 0 ? message.BulletCount : 1;
            FireLockType lockType = (FireLockType)message.FireLockTypeId;
            for (int i = 0; i < bulletCount; i++)
            {
                CreateProjectileAsync(root, caster, target, visualConfig, lockType).Coroutine();
            }

            await ETTask.CompletedTask;
        }

        private static async ETTask CreateProjectileAsync(Scene root, Unit caster, Unit target, WeaponVisualConfig visualConfig, FireLockType lockType)
        {
            if (string.IsNullOrEmpty(visualConfig.ProjectileEffect))
            {
                return;
            }

            Scene currentScene = root.CurrentScene();
            ResourcesLoaderComponent resourcesLoader = currentScene?.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                return;
            }

            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> casterRef = caster;
            EntityRef<Unit> targetRef = target;
            GameObject prefab = await resourcesLoader.LoadAssetAsync<GameObject>(visualConfig.ProjectileEffect);

            root = rootRef;
            caster = casterRef;
            target = targetRef;
            if (root == null || caster == null || target == null || prefab == null)
            {
                return;
            }

            BindPoint casterBindPoint = visualConfig.ToBindPoint(visualConfig.CasterBindPointId, BindPoint.Attack);
            BindPoint targetBindPoint = visualConfig.ToBindPoint(visualConfig.TargetBindPointId, BindPoint.Hitted);

            Vector3 startPos = GetBindPosition(caster, casterBindPoint);
            Transform targetBindTransform = GetBindTransform(target, targetBindPoint);
            Vector3 lockedTargetPos = targetBindTransform != null ? targetBindTransform.position : GetBindPosition(target, BindPoint.Base);

            GameObject effect = UnityEngine.Object.Instantiate(prefab, startPos, Quaternion.identity);
            Transform effectRoot = GameObject.Find("/Global/Unit")?.transform;
            if (effectRoot != null)
            {
                effect.transform.SetParent(effectRoot);
            }

            float speed = visualConfig.ProjectileSpeed > 0 ? visualConfig.ProjectileSpeed : 30f;
            int durationMs = visualConfig.ProjectileDurationMs > 0 ? visualConfig.ProjectileDurationMs : 2000;
            float endTime = Time.time + durationMs / 1000f;
            TimerComponent timerComponent = root.TimerComponent;

            while (Time.time < endTime)
            {
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

            UnityEngine.Object.Destroy(effect);
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
