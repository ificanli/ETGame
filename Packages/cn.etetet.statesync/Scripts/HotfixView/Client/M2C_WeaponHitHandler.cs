using UnityEngine;

namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponHitHandler : MessageHandler<Scene, M2C_WeaponHit>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponHit message)
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

            if (string.IsNullOrEmpty(visualConfig.HitEffect))
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

            Unit target = unitComponent.Get(message.TargetUnitId);
            if (target == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            CreateHitEffectAsync(root, target, visualConfig).Coroutine();

            await ETTask.CompletedTask;
        }

        private static async ETTask CreateHitEffectAsync(Scene root, Unit target, WeaponVisualConfig visualConfig)
        {
            Scene currentScene = root.CurrentScene();
            ResourcesLoaderComponent resourcesLoader = currentScene?.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                return;
            }

            EntityRef<Unit> targetRef = target;
            GameObject prefab = await resourcesLoader.LoadAssetAsync<GameObject>(visualConfig.HitEffect);

            target = targetRef;
            if (target == null || prefab == null)
            {
                return;
            }

            BindPoint bindPoint = visualConfig.ToBindPoint(visualConfig.TargetBindPointId, BindPoint.Hitted);
            Vector3 position = GetBindPosition(target, bindPoint);
            int durationMs = visualConfig.HitEffectDurationMs > 0 ? visualConfig.HitEffectDurationMs : 1200;

            GameObject effect = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            Transform effectRoot = GameObject.Find("/Global/Unit")?.transform;
            if (effectRoot != null)
            {
                effect.transform.SetParent(effectRoot);
            }
            UnityEngine.Object.Destroy(effect, durationMs / 1000f);
        }

        private static Vector3 GetBindPosition(Unit unit, BindPoint bindPoint)
        {
            GameObject go = unit.GetComponent<GameObjectComponent>()?.GameObject;
            if (go == null)
            {
                return Vector3.zero;
            }

            BindPointComponent bindPointComponent = go.GetComponent<BindPointComponent>();
            if (bindPointComponent != null && bindPointComponent.GetBindPoints().TryGetValue(bindPoint, out Transform transform) && transform != null)
            {
                return transform.position;
            }

            return go.transform.position;
        }
    }
}
