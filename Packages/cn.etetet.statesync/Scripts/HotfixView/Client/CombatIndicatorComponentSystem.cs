using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 管理本地玩家的攻击范围圈和当前攻击目标红圈。
    /// </summary>
    [EntitySystemOf(typeof(CombatIndicatorComponent))]
    public static partial class CombatIndicatorComponentSystem
    {
        private const string RangeIndicatorPrefabName = "RoundIndicator";
        private const string TargetIndicatorPrefabName = "enemyIndicator";
        private const float IndicatorOffsetY = 0.05f;
        private const int ViewReadyRetryCount = 120;
        private const float DistanceWeight = 0.7f;
        private const float HpWeight = 0.3f;
        private const float AttackMeBonus = 0.5f;

        [EntitySystem]
        private static void Awake(this CombatIndicatorComponent self)
        {
            self.RangeIndicatorObject = null;
            self.RangeIndicatorBaseScale = Vector3.one;
            self.TargetIndicatorObject = null;
            self.TargetIndicatorBaseScale = Vector3.one;
            self.IndicatorRoot = null;
            self.CurrentTargetUnitId = 0;
            self.CurrentWeaponId = 0;
            self.CurrentAttackRange = 0f;
            self.LoadVersion = 0;
            self.IsInitialized = false;
        }

        [EntitySystem]
        private static void Destroy(this CombatIndicatorComponent self)
        {
            self.DestroyIndicators();
            self.IndicatorRoot = null;
            self.CurrentTargetUnitId = 0;
            self.CurrentWeaponId = 0;
            self.CurrentAttackRange = 0f;
            self.IsInitialized = false;
        }

        [EntitySystem]
        private static void Update(this CombatIndicatorComponent self)
        {
            if (!self.IsInitialized)
            {
                return;
            }

            Unit owner = self.GetParent<Unit>();
            if (owner == null || owner.IsDisposed)
            {
                return;
            }

            self.RefreshRangeIndicator(owner);
            self.RefreshTargetIndicator(owner);
        }

        public static async ETTask InitializeAsync(this CombatIndicatorComponent self, Scene scene)
        {
            if (self == null || scene == null || scene.IsDisposed)
            {
                return;
            }

            if (self.IsInitialized && self.RangeIndicatorObject != null && self.TargetIndicatorObject != null)
            {
                return;
            }

            self.LoadVersion++;
            int loadVersion = self.LoadVersion;
            EntityRef<CombatIndicatorComponent> selfRef = self;
            EntityRef<Scene> sceneRef = scene;

            if (!await self.WaitViewReadyAsync(scene, loadVersion))
            {
                return;
            }

            self = selfRef;
            scene = sceneRef;
            if (self == null || scene == null || scene.IsDisposed || self.LoadVersion != loadVersion)
            {
                return;
            }

            ResourcesLoaderComponent resourcesLoader = scene.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                Log.Warning("[CombatIndicator] ResourcesLoaderComponent not found");
                return;
            }

            GameObject rangePrefab = await resourcesLoader.LoadAssetAsync<GameObject>(RangeIndicatorPrefabName);

            self = selfRef;
            scene = sceneRef;
            if (self == null || scene == null || scene.IsDisposed || self.LoadVersion != loadVersion)
            {
                return;
            }

            resourcesLoader = scene.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                return;
            }

            GameObject targetPrefab = await resourcesLoader.LoadAssetAsync<GameObject>(TargetIndicatorPrefabName);

            self = selfRef;
            scene = sceneRef;
            if (self == null || scene == null || scene.IsDisposed || self.LoadVersion != loadVersion)
            {
                return;
            }

            if (rangePrefab == null || targetPrefab == null)
            {
                Log.Warning($"[CombatIndicator] indicator prefab load failed, rangeNull={rangePrefab == null}, targetNull={targetPrefab == null}");
                return;
            }

            GlobalComponent globalComponent = scene.Root().GetComponent<GlobalComponent>();
            if (globalComponent?.Unit == null)
            {
                Log.Warning("[CombatIndicator] indicator root not found");
                return;
            }

            self.DestroyIndicators();

            self.IndicatorRoot = globalComponent.Unit;
            self.RangeIndicatorObject = self.InstantiateIndicator(rangePrefab, globalComponent.Unit);
            self.TargetIndicatorObject = self.InstantiateIndicator(targetPrefab, globalComponent.Unit);
            self.RangeIndicatorBaseScale = self.RangeIndicatorObject.transform.localScale;
            self.TargetIndicatorBaseScale = self.TargetIndicatorObject.transform.localScale;
            self.RangeIndicatorObject.SetActive(false);
            self.TargetIndicatorObject.SetActive(false);
            self.IsInitialized = true;
        }

        private static void RefreshRangeIndicator(this CombatIndicatorComponent self, Unit owner)
        {
            if (self.RangeIndicatorObject == null)
            {
                return;
            }

            WeaponComponent weaponComponent = owner.GetComponent<WeaponComponent>();
            int weaponId = weaponComponent?.CurrentWeaponId ?? 0;
            float attackRange = weaponComponent != null ? weaponComponent.GetEffectiveAttackRange(weaponComponent.CurrentSlot) : 0f;
            if (attackRange <= 0f)
            {
                WeaponConfig weaponConfig = weaponId > 0 ? WeaponConfigCategory.Instance.GetOrDefault(weaponId) : null;
                attackRange = weaponConfig?.AttackRange ?? 0f;
            }

            if (attackRange <= 0f)
            {
                self.CurrentWeaponId = 0;
                self.CurrentAttackRange = 0f;
                self.RangeIndicatorObject.SetActive(false);
                return;
            }

            if (self.CurrentWeaponId != weaponId || Mathf.Abs(self.CurrentAttackRange - attackRange) > 0.001f)
            {
                self.CurrentWeaponId = weaponId;
                self.CurrentAttackRange = attackRange;
                self.RangeIndicatorObject.transform.localScale = self.RangeIndicatorBaseScale * attackRange;
            }

            self.RangeIndicatorObject.transform.position = self.GetIndicatorPosition(owner);
            self.RangeIndicatorObject.transform.rotation = Quaternion.identity;
            if (!self.RangeIndicatorObject.activeSelf)
            {
                self.RangeIndicatorObject.SetActive(true);
            }
        }

        private static void RefreshTargetIndicator(this CombatIndicatorComponent self, Unit owner)
        {
            if (self.TargetIndicatorObject == null)
            {
                return;
            }

            float attackRange = self.CurrentAttackRange;
            if (attackRange <= 0f)
            {
                self.CurrentTargetUnitId = 0;
                self.TargetIndicatorObject.SetActive(false);
                return;
            }

            Unit target = self.ResolveCombatTarget(owner, attackRange);
            if (target == null)
            {
                self.CurrentTargetUnitId = 0;
                self.TargetIndicatorObject.SetActive(false);
                return;
            }

            self.CurrentTargetUnitId = target.Id;
            self.TargetIndicatorObject.transform.position = self.GetIndicatorPosition(target);
            self.TargetIndicatorObject.transform.rotation = Quaternion.identity;
            self.TargetIndicatorObject.transform.localScale = self.TargetIndicatorBaseScale;
            if (!self.TargetIndicatorObject.activeSelf)
            {
                self.TargetIndicatorObject.SetActive(true);
            }
        }

        private static Unit ResolveCombatTarget(this CombatIndicatorComponent self, Unit owner, float attackRange)
        {
            Unit target = self.GetManualTarget(owner, attackRange);
            if (target != null)
            {
                return target;
            }

            target = self.GetSelectorTarget(owner, attackRange);
            if (target != null)
            {
                return target;
            }

            return self.FindLocalBestTarget(owner, attackRange);
        }

        private static Unit GetManualTarget(this CombatIndicatorComponent self, Unit owner, float attackRange)
        {
            TargetComponent targetComponent = owner.GetComponent<TargetComponent>();
            Unit target = targetComponent?.Unit;
            return self.IsAttackableTarget(owner, target, attackRange) ? target : null;
        }

        private static Unit GetSelectorTarget(this CombatIndicatorComponent self, Unit owner, float attackRange)
        {
            TargetSelectorComponent selector = owner.GetComponent<TargetSelectorComponent>();
            if (selector == null)
            {
                return null;
            }

            Unit target = selector.GetCurrentTarget();
            if (self.IsAttackableTarget(owner, target, attackRange))
            {
                return target;
            }

            if (selector.CurrentTargetId == 0)
            {
                return null;
            }

            UnitComponent unitComponent = owner.Scene()?.GetComponent<UnitComponent>();
            target = unitComponent?.Get(selector.CurrentTargetId);
            return self.IsAttackableTarget(owner, target, attackRange) ? target : null;
        }

        private static Unit FindLocalBestTarget(this CombatIndicatorComponent self, Unit owner, float attackRange)
        {
            UnitComponent unitComponent = owner.Scene()?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return null;
            }

            Unit bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is not Unit candidate)
                {
                    continue;
                }

                if (!self.IsAttackableTarget(owner, candidate, attackRange))
                {
                    continue;
                }

                float distance = self.GetHorizontalDistance(self.GetWorldPosition(owner), self.GetWorldPosition(candidate));
                float score = distance * DistanceWeight + self.GetHpPercent(candidate) * attackRange * HpWeight;
                if (self.IsAttackingOwner(candidate, owner))
                {
                    score *= AttackMeBonus;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private static bool IsAttackableTarget(this CombatIndicatorComponent self, Unit owner, Unit target, float attackRange)
        {
            if (owner == null || target == null || owner.IsDisposed || target.IsDisposed)
            {
                return false;
            }

            if (owner.Id == target.Id)
            {
                return false;
            }

            if (!CampHelper.IsEnemy(owner, target))
            {
                return false;
            }

            NumericComponent numeric = target.NumericComponent;
            if (numeric != null && numeric.GetAsFloat(NumericType.HP) <= 0f)
            {
                return false;
            }

            return self.GetHorizontalDistance(self.GetWorldPosition(owner), self.GetWorldPosition(target)) <= attackRange;
        }

        private static bool IsAttackingOwner(this CombatIndicatorComponent self, Unit candidate, Unit owner)
        {
            TargetSelectorComponent targetSelectorComponent = candidate.GetComponent<TargetSelectorComponent>();
            return targetSelectorComponent != null && targetSelectorComponent.CurrentTargetId == owner.Id;
        }

        private static float GetHpPercent(this CombatIndicatorComponent self, Unit target)
        {
            NumericComponent numeric = target.NumericComponent;
            if (numeric == null)
            {
                return 1f;
            }

            float hp = numeric.GetAsFloat(NumericType.HP);
            float maxHp = numeric.GetAsFloat(NumericType.MaxHP);
            return maxHp > 0f ? hp / maxHp : 0f;
        }

        private static float GetHorizontalDistance(this CombatIndicatorComponent self, Vector3 a, Vector3 b)
        {
            return math.distance(new float2(a.x, a.z), new float2(b.x, b.z));
        }

        private static Vector3 GetIndicatorPosition(this CombatIndicatorComponent self, Unit unit)
        {
            Vector3 position = self.GetWorldPosition(unit);
            position.y += IndicatorOffsetY;
            return position;
        }

        private static Vector3 GetWorldPosition(this CombatIndicatorComponent self, Unit unit)
        {
            Transform transform = unit.GetComponent<GameObjectComponent>()?.Transform;
            return transform != null ? transform.position : (Vector3)unit.Position;
        }

        private static GameObject InstantiateIndicator(this CombatIndicatorComponent self, GameObject prefab, Transform parent)
        {
            GameObject indicatorObject = UnityEngine.Object.Instantiate(prefab, parent, false);
            indicatorObject.transform.localPosition = new Vector3(0f, IndicatorOffsetY, 0f);
            indicatorObject.transform.localRotation = Quaternion.identity;

            foreach (Collider collider in indicatorObject.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            return indicatorObject;
        }

        private static void DestroyIndicators(this CombatIndicatorComponent self)
        {
            if (self.RangeIndicatorObject != null)
            {
                UnityEngine.Object.Destroy(self.RangeIndicatorObject);
            }

            if (self.TargetIndicatorObject != null)
            {
                UnityEngine.Object.Destroy(self.TargetIndicatorObject);
            }

            self.RangeIndicatorObject = null;
            self.TargetIndicatorObject = null;
        }

        private static async ETTask<bool> WaitViewReadyAsync(this CombatIndicatorComponent self, Scene scene, int loadVersion)
        {
            EntityRef<CombatIndicatorComponent> selfRef = self;
            EntityRef<Scene> sceneRef = scene;

            for (int retry = 0; retry <= ViewReadyRetryCount; ++retry)
            {
                self = selfRef;
                scene = sceneRef;
                if (self == null || scene == null || scene.IsDisposed || self.LoadVersion != loadVersion)
                {
                    return false;
                }

                Unit owner = self.GetParent<Unit>();
                bool unitViewReady = owner?.GetComponent<GameObjectComponent>()?.Transform != null;
                bool indicatorRootReady = scene.Root().GetComponent<GlobalComponent>()?.Unit != null;
                if (unitViewReady && indicatorRootReady)
                {
                    return true;
                }

                if (retry == ViewReadyRetryCount)
                {
                    Log.Warning($"[CombatIndicator] wait view ready timeout, unitId={owner?.Id ?? 0}");
                    return false;
                }

                await scene.Root().TimerComponent.WaitFrameAsync();
            }

            return false;
        }
    }
}
