using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 肉鸽升级表现：仅本地玩家自己播放升级特效与弹字。
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_RogueLevelUp_FeedbackHandler : MessageHandler<Scene, M2C_RogueLevelUp>
    {
        private const string LevelUpEffectLocation = "LevelUp";
        private const string LevelUpEffectFallbackLocation = "Effect/LevelUp";
        private const float LevelUpEffectYOffset = 1.1f;
        private const float LevelUpEffectLifetimeSeconds = 2.8f;

        protected override async ETTask Run(Scene root, M2C_RogueLevelUp message)
        {
            if (root == null || root.IsDisposed || message == null || message.NewLevel <= message.OldLevel)
            {
                await ETTask.CompletedTask;
                return;
            }

            Scene currentScene = root.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (currentScene == null || myUnit == null || myUnit.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            var unitPosition = myUnit.Position;
            Vector3 worldPos = new Vector3(unitPosition.x, unitPosition.y + LevelUpEffectYOffset, unitPosition.z);

            MainPanelComponent mainPanel = root.YIUIMgr()?.GetPanel<MainPanelComponent>();
            mainPanel?.ShowLevelUpToast(worldPos, "LEVEL UP");

            PlayLevelUpEffectAsync(root, myUnit, worldPos).Coroutine();
            await ETTask.CompletedTask;
        }

        private static async ETTask PlayLevelUpEffectAsync(Scene root, Unit unit, Vector3 worldPos)
        {
            Scene currentScene = root.CurrentScene();
            ResourcesLoaderComponent resourcesLoader = currentScene?.GetComponent<ResourcesLoaderComponent>();
            if (resourcesLoader == null)
            {
                Log.Warning("[RogueLevelUpFeedback] missing ResourcesLoaderComponent");
                return;
            }

            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> unitRef = unit;
            EntityRef<ResourcesLoaderComponent> resourcesLoaderRef = resourcesLoader;

            GameObject prefab = await resourcesLoader.LoadAssetAsync<GameObject>(LevelUpEffectLocation);
            root = rootRef;
            unit = unitRef;
            resourcesLoader = resourcesLoaderRef;
            if (root == null || root.IsDisposed || unit == null || unit.IsDisposed || resourcesLoader == null || resourcesLoader.IsDisposed)
            {
                Log.Warning("[RogueLevelUpFeedback] level up effect load interrupted after primary asset request");
                return;
            }

            if (prefab == null)
            {
                prefab = await resourcesLoader.LoadAssetAsync<GameObject>(LevelUpEffectFallbackLocation);
                root = rootRef;
                unit = unitRef;
                resourcesLoader = resourcesLoaderRef;
                if (root == null || root.IsDisposed || unit == null || unit.IsDisposed || resourcesLoader == null || resourcesLoader.IsDisposed)
                {
                    Log.Warning("[RogueLevelUpFeedback] level up effect load interrupted after fallback asset request");
                    return;
                }
            }

            root = rootRef;
            unit = unitRef;
            resourcesLoader = resourcesLoaderRef;
            if (root == null || root.IsDisposed || unit == null || unit.IsDisposed || resourcesLoader == null || resourcesLoader.IsDisposed || prefab == null)
            {
                Log.Warning($"[RogueLevelUpFeedback] level up effect load failed, prefabNull={prefab == null}");
                return;
            }

            GameObject effect = UnityEngine.Object.Instantiate(prefab, worldPos, Quaternion.identity);
            Transform effectRoot = GameObject.Find("/Global/Unit")?.transform;
            if (effectRoot != null)
            {
                effect.transform.SetParent(effectRoot);
            }

            UnityEngine.Object.Destroy(effect, LevelUpEffectLifetimeSeconds);
        }
    }
}
