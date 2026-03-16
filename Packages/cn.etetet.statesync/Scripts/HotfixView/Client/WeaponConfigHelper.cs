using System;
using UnityEngine;

namespace ET.Client
{
    public static class WeaponConfigHelper
    {
        public static BindPoint ToBindPoint(this global::ET.WeaponConfig _, int bindPointId, BindPoint fallback)
        {
            return Enum.IsDefined(typeof(BindPoint), bindPointId) ? (BindPoint)bindPointId : fallback;
        }
    }

    /// <summary>
    /// Unit视图创建完成后，同步当前武器表现和持枪动画状态。
    /// </summary>
    [Event(SceneType.Current)]
    public class AfterUnitCreate_SyncWeaponView : AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
            EntityRef<Scene> sceneRef = scene;
            EntityRef<Unit> unitRef = unit;
            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null)
            {
                weaponComponent = TryCreateLocalWeaponComponent(scene, unit);
            }

            if (weaponComponent == null)
            {
                return;
            }

            WeaponViewComponent weaponViewComponent = unit.GetComponent<WeaponViewComponent>();
            if (weaponViewComponent == null)
            {
                weaponViewComponent = unit.AddComponent<WeaponViewComponent>();
            }

            await WeaponViewComponentSystem.RefreshWeaponAsync(weaponViewComponent, scene.Root(), weaponComponent.CurrentWeaponId);

            scene = sceneRef;
            unit = unitRef;
            if (scene == null || unit == null)
            {
                return;
            }

            if (!unit.IsMyUnit())
            {
                return;
            }

            CombatIndicatorComponent combatIndicatorComponent = unit.GetComponent<CombatIndicatorComponent>();
            if (combatIndicatorComponent == null)
            {
                combatIndicatorComponent = unit.AddComponent<CombatIndicatorComponent>();
            }

            await combatIndicatorComponent.InitializeAsync(scene);
        }

        private static WeaponComponent TryCreateLocalWeaponComponent(Scene scene, Unit unit)
        {
            PlayerComponent playerComponent = scene.Root().GetComponent<PlayerComponent>();
            if (playerComponent == null || playerComponent.MyId != unit.Id)
            {
                return null;
            }

            LoadoutComponent loadout = scene.Root().GetComponent<LoadoutComponent>();
            if (loadout == null || !loadout.IsConfirmed)
            {
                Log.Info($"[WeaponInitTrace][LocalFallback] skip, unitId={unit.Id}, hasLoadout={loadout != null}, isConfirmed={loadout?.IsConfirmed}");
                return null;
            }

            if (loadout.MainWeaponConfigId <= 0 && loadout.SubWeaponConfigId <= 0)
            {
                Log.Info($"[WeaponInitTrace][LocalFallback] no weapon, unitId={unit.Id}");
                return null;
            }

            WeaponComponent weaponComponent = unit.AddComponent<WeaponComponent, int, int>(loadout.MainWeaponConfigId, loadout.SubWeaponConfigId);
            Log.Info($"[WeaponInitTrace][LocalFallback] created, unitId={unit.Id}, slot1={loadout.MainWeaponConfigId}, slot2={loadout.SubWeaponConfigId}, currentSlot={weaponComponent.CurrentSlot}");
            return weaponComponent;
        }
    }

    [EntitySystemOf(typeof(WeaponViewComponent))]
    public static partial class WeaponViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WeaponViewComponent self)
        {
            self.WeaponObject = null;
            self.MuzzleTransform = null;
            self.WeaponId = 0;
            self.LoadVersion = 0;
        }

        [EntitySystem]
        private static void Destroy(this WeaponViewComponent self)
        {
            self.ClearWeaponObject();
            self.MuzzleTransform = null;
            self.WeaponId = 0;
        }

        public static async ETTask RefreshWeaponAsync(this WeaponViewComponent self, Scene root, int weaponId)
        {
            Unit unit = self.GetParent<Unit>();
            long unitId = unit?.Id ?? 0;
            Log.Info($"[WeaponInitTrace][Refresh] begin unitId={unitId}, weaponId={weaponId}, hasWeapon={weaponId > 0}");

            self.LoadVersion++;
            int loadVersion = self.LoadVersion;
            self.WeaponId = weaponId;
            self.ClearWeaponObject();

            EntityRef<WeaponViewComponent> selfReadyRef = self;
            EntityRef<Scene> rootReadyRef = root;
            if (!await self.WaitViewReadyAsync(root, loadVersion, weaponId))
            {
                return;
            }

            self = selfReadyRef;
            root = rootReadyRef;
            if (self == null || root == null || self.LoadVersion != loadVersion)
            {
                Log.Info($"[WeaponInitTrace][Refresh] skip after wait, unitId={unitId}, weaponId={weaponId}, loadVersion={loadVersion}, currentVersion={self?.LoadVersion ?? -1}");
                return;
            }

            unit = self.GetParent<Unit>();
            if (unit == null)
            {
                return;
            }

            self.ApplyAnimatorState(weaponId > 0);

            if (weaponId <= 0)
            {
                return;
            }

            global::ET.WeaponConfig weaponConfig = global::ET.WeaponConfigCategory.Instance.GetOrDefault(weaponId);
            if (weaponConfig == null)
            {
                Log.Warning($"[WeaponView] weapon config not found for weaponId={weaponId}");
                return;
            }

            Log.Info($"[WeaponInitTrace][Refresh] config ok unitId={unit.Id}, weaponId={weaponId}, prefab={weaponConfig.WeaponPrefab}, bindPointId={weaponConfig.WeaponBindPointId}, muzzlePath={weaponConfig.WeaponMuzzlePath}");

            if (string.IsNullOrEmpty(weaponConfig.WeaponPrefab))
            {
                Log.Warning($"[WeaponInitTrace][Refresh] empty prefab unitId={unit.Id}, weaponId={weaponId}");
                return;
            }

            Scene currentScene = root.CurrentScene();
            ResourcesLoaderComponent resourcesLoader = currentScene?.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                Log.Warning("[WeaponView] ResourcesLoaderComponent not found");
                return;
            }

            EntityRef<WeaponViewComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;
            GameObject prefab = await resourcesLoader.LoadAssetAsync<GameObject>(weaponConfig.WeaponPrefab);

            self = selfRef;
            root = rootRef;
            if (self == null || prefab == null || self.LoadVersion != loadVersion)
            {
                Log.Warning($"[WeaponInitTrace][Refresh] prefab load failed or stale unitId={unitId}, weaponId={weaponId}, prefabNull={prefab == null}, loadVersion={loadVersion}, currentVersion={self?.LoadVersion ?? -1}");
                return;
            }

            unit = self.GetParent<Unit>();
            if (unit == null)
            {
                return;
            }

            Transform bindTransform = self.GetWeaponBindTransform(unit, weaponConfig);
            if (bindTransform == null)
            {
                Log.Warning($"[WeaponView] bind transform not found, unit={unit.Id}, weaponId={weaponId}");
                return;
            }

            Log.Info($"[WeaponInitTrace][Refresh] bind ok unitId={unit.Id}, weaponId={weaponId}, bindTransform={bindTransform.name}, prefab={prefab.name}");

            GameObject weaponObject = UnityEngine.Object.Instantiate(prefab, bindTransform, false);
            weaponObject.transform.localPosition = new Vector3(
                weaponConfig.WeaponLocalPositionX,
                weaponConfig.WeaponLocalPositionY,
                weaponConfig.WeaponLocalPositionZ);
            weaponObject.transform.localRotation = Quaternion.Euler(
                weaponConfig.WeaponLocalEulerX,
                weaponConfig.WeaponLocalEulerY,
                weaponConfig.WeaponLocalEulerZ);

            float scale = weaponConfig.WeaponLocalScale > 0.0001f ? weaponConfig.WeaponLocalScale : 1f;
            weaponObject.transform.localScale = Vector3.one * scale;

            self.WeaponObject = weaponObject;
            self.MuzzleTransform = self.FindMuzzleTransform(unit, weaponObject.transform, weaponConfig);
            Log.Info($"[WeaponInitTrace][Refresh] instantiated unitId={unit.Id}, weaponId={weaponId}, weaponObject={weaponObject.name}, muzzle={self.MuzzleTransform?.name ?? "null"}");
        }

        public static Vector3 GetFirePoint(this WeaponViewComponent self, Unit unit, global::ET.WeaponConfig weaponConfig)
        {
            if (self?.MuzzleTransform != null)
            {
                return self.MuzzleTransform.position;
            }

            BindPoint fallbackBindPoint = weaponConfig.ToBindPoint(weaponConfig.MuzzleBindPointId, BindPoint.Bullet);
            Transform bindTransform = self.GetUnitBindTransform(unit, fallbackBindPoint);
            if (bindTransform != null)
            {
                return bindTransform.position;
            }

            return self.GetFallbackFirePoint(unit, weaponConfig);
        }

        private static void ApplyAnimatorState(this WeaponViewComponent self, bool hasWeapon)
        {
            Unit unit = self.GetParent<Unit>();
            AnimatorComponent animatorComponent = unit?.GetComponent<AnimatorComponent>();
            WeaponComponent weaponComponent = unit?.GetComponent<WeaponComponent>();
            bool animatorReady = animatorComponent?.Animator != null;
            bool hasParameter = animatorComponent?.HasParameter("HasWeapon") ?? false;
            if (animatorReady && hasParameter)
            {
                animatorComponent.SetBool("HasWeapon", hasWeapon);
            }

            Log.Info($"[WeaponInitTrace][View] unitId={unit?.Id ?? 0}, hasWeapon={hasWeapon}, weaponComponentCurrent={weaponComponent?.CurrentWeaponId ?? 0}, slot1={weaponComponent?.Slot1WeaponId ?? 0}, slot2={weaponComponent?.Slot2WeaponId ?? 0}, viewWeaponId={self.WeaponId}, animatorReady={animatorReady}, hasHasWeaponParam={hasParameter}");
        }

        private static void ClearWeaponObject(this WeaponViewComponent self)
        {
            if (self.WeaponObject != null)
            {
                UnityEngine.Object.Destroy(self.WeaponObject);
            }

            self.WeaponObject = null;
            self.MuzzleTransform = null;
        }

        private static Transform GetWeaponBindTransform(this WeaponViewComponent self, Unit unit, global::ET.WeaponConfig weaponConfig)
        {
            BindPoint bindPoint = weaponConfig.ToBindPoint(weaponConfig.WeaponBindPointId, BindPoint.RightHand);
            Transform bindTransform = self.GetUnitBindTransform(unit, bindPoint);
            return bindTransform ?? unit.GetComponent<GameObjectComponent>()?.Transform;
        }

        private static Transform FindMuzzleTransform(this WeaponViewComponent self, Unit unit, Transform weaponRoot, global::ET.WeaponConfig weaponConfig)
        {
            if (weaponRoot != null && !string.IsNullOrEmpty(weaponConfig.WeaponMuzzlePath))
            {
                Transform muzzleTransform = weaponRoot.Find(weaponConfig.WeaponMuzzlePath);
                if (muzzleTransform != null)
                {
                    return muzzleTransform;
                }

                Log.Warning($"[WeaponView] muzzle path not found: {weaponConfig.WeaponMuzzlePath}, weaponId={weaponConfig.Id}");
            }

            BindPoint muzzleBindPoint = weaponConfig.ToBindPoint(weaponConfig.MuzzleBindPointId, BindPoint.Bullet);
            return self.GetUnitBindTransform(unit, muzzleBindPoint);
        }

        private static Transform GetUnitBindTransform(this WeaponViewComponent self, Unit unit, BindPoint bindPoint)
        {
            GameObject go = unit?.GetComponent<GameObjectComponent>()?.GameObject;
            if (go == null)
            {
                return null;
            }

            BindPointComponent bindPointComponent = go.GetComponent<BindPointComponent>();
            if (bindPointComponent != null &&
                bindPointComponent.GetBindPoints().TryGetValue(bindPoint, out Transform transform) &&
                transform != null)
            {
                return transform;
            }

            return null;
        }

        private static Vector3 GetFallbackFirePoint(this WeaponViewComponent self, Unit unit, global::ET.WeaponConfig weaponConfig)
        {
            BindPoint casterBindPoint = weaponConfig.ToBindPoint(weaponConfig.CasterBindPointId, BindPoint.Attack);
            Transform bindTransform = self.GetUnitBindTransform(unit, casterBindPoint);
            if (bindTransform != null)
            {
                return bindTransform.position;
            }

            return unit?.GetComponent<GameObjectComponent>()?.Transform?.position ?? Vector3.zero;
        }

        private static async ETTask<bool> WaitViewReadyAsync(this WeaponViewComponent self, Scene root, int loadVersion, int weaponId)
        {
            EntityRef<WeaponViewComponent> selfRef = self;
            EntityRef<Scene> rootRef = root;

            const int maxRetryCount = 300;
            for (int retry = 0; retry <= maxRetryCount; ++retry)
            {
                self = selfRef;
                root = rootRef;
                if (self == null || root == null || self.LoadVersion != loadVersion)
                {
                    Log.Info($"[WeaponInitTrace][SyncRetry] abort, weapon view stale, weaponId={weaponId}, loadVersion={loadVersion}, currentVersion={self?.LoadVersion ?? -1}");
                    return false;
                }

                Unit unit = self.GetParent<Unit>();
                GameObjectComponent gameObjectComponent = unit?.GetComponent<GameObjectComponent>();
                AnimatorComponent animatorComponent = unit?.GetComponent<AnimatorComponent>();
                bool gameObjectReady = gameObjectComponent?.GameObject != null;
                bool animatorReady = animatorComponent?.Animator != null;
                if (gameObjectReady && animatorReady)
                {
                    if (retry > 0)
                    {
                        Log.Info($"[WeaponInitTrace][SyncRetry] ready, unitId={unit.Id}, retry={retry}, weaponId={weaponId}");
                    }

                    return true;
                }

                if (retry == maxRetryCount)
                {
                    Log.Warning($"[WeaponInitTrace][SyncRetry] timeout, unitId={unit?.Id ?? 0}, retry={retry}, weaponId={weaponId}, gameObjectReady={gameObjectReady}, animatorReady={animatorReady}");
                    return false;
                }

                if (retry < 3 || retry % 30 == 0)
                {
                    Log.Info($"[WeaponInitTrace][SyncRetry] waiting view ready, unitId={unit?.Id ?? 0}, retry={retry}, weaponId={weaponId}, gameObjectReady={gameObjectReady}, animatorReady={animatorReady}");
                }

                await root.TimerComponent.WaitFrameAsync();
            }

            return false;
        }
    }
}
