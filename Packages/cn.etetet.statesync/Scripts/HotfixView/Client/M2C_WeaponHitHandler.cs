using UnityEngine;

namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_WeaponHitHandler : MessageHandler<Scene, M2C_WeaponHit>
    {
        protected override async ETTask Run(Scene root, M2C_WeaponHit message)
        {
            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                Log.Warning($"[WeaponFireTrace][Hit] missing unit component, weaponId={message.WeaponId}");
                await ETTask.CompletedTask;
                return;
            }

            Unit target = unitComponent.Get(message.TargetUnitId);
            if (target == null)
            {
                Log.Warning($"[WeaponFireTrace][Hit] target missing, target={message.TargetUnitId}, weaponId={message.WeaponId}");
                await ETTask.CompletedTask;
                return;
            }

            if (target.IsMyUnit())
            {
                MainPanelComponent mainPanel = root.YIUIMgr()?.GetPanel<MainPanelComponent>();
                mainPanel?.NotifyHitDirection(message.CasterUnitId);
            }

            global::ET.WeaponConfig weaponConfig = global::ET.WeaponConfigCategory.Instance.GetOrDefault(message.WeaponId);
            if (weaponConfig == null)
            {
                Log.Warning($"[WeaponFireTrace][Hit] missing config, weaponId={message.WeaponId}, target={message.TargetUnitId}");
                await ETTask.CompletedTask;
                return;
            }

            if (string.IsNullOrEmpty(weaponConfig.HitEffect))
            {
                Log.Warning($"[WeaponFireTrace][Hit] empty hit effect, weaponId={message.WeaponId}");
                await ETTask.CompletedTask;
                return;
            }

            Log.Info($"[WeaponFireTrace][Hit] hit message received, weaponId={message.WeaponId}, target={message.TargetUnitId}, effect={weaponConfig.HitEffect}");
            CreateHitEffectAsync(root, target, weaponConfig).Coroutine();

            await ETTask.CompletedTask;
        }

        private static async ETTask CreateHitEffectAsync(Scene root, Unit target, global::ET.WeaponConfig weaponConfig)
        {
            Scene currentScene = root.CurrentScene();
            ResourcesLoaderComponent resourcesLoader = currentScene?.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                Log.Warning($"[WeaponFireTrace][Hit] missing resources loader, weaponId={weaponConfig.Id}");
                return;
            }

            EntityRef<Unit> targetRef = target;
            GameObject prefab = await resourcesLoader.LoadAssetAsync<GameObject>(weaponConfig.HitEffect);

            target = targetRef;
            if (target == null || prefab == null)
            {
                Log.Warning($"[WeaponFireTrace][Hit] hit effect load failed, weaponId={weaponConfig.Id}, prefabNull={prefab == null}, targetNull={target == null}");
                return;
            }

            BindPoint bindPoint = weaponConfig.ToBindPoint(weaponConfig.TargetBindPointId, BindPoint.Hitted);
            Vector3 position = GetBindPosition(target, bindPoint);
            int durationMs = weaponConfig.HitEffectDurationMs > 0 ? weaponConfig.HitEffectDurationMs : 1200;

            GameObject effect = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            Log.Info($"[WeaponFireTrace][Hit] hit effect instantiated, weaponId={weaponConfig.Id}, effect={prefab.name}, pos={position}");
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
