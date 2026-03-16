using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    public static class TacticalItemClientHelper
    {
        public static async ETTask TryUseBagItem(Scene root, long itemId)
        {
            if (root == null || root.IsDisposed)
            {
                return;
            }

            ItemComponent itemComponent = root.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                Log.Warning("[TacticalItem] item component missing");
                return;
            }

            Item item = itemComponent.GetItemById(itemId);
            if (item == null || item.IsDisposed)
            {
                Log.Warning($"[TacticalItem] item not found: itemId={itemId}");
                return;
            }

            TacticalItemConfig config = TacticalItemConfigCategory.Instance?.GetByItemConfigId(item.ConfigId);
            if (config == null)
            {
                return;
            }

            TacticalCastClientComponent castComponent = root.GetComponent<TacticalCastClientComponent>() ?? root.AddComponent<TacticalCastClientComponent>();
            castComponent.BeginCast(item.Id, item.ConfigId, config);
            EntityRef<Scene> rootRef = root;
            EntityRef<TacticalCastClientComponent> castComponentRef = castComponent;
            long requestItemId = item.Id;

            float3 targetPosition = float3.zero;
            TacticalCastMode castMode = config.GetTacticalCastMode();

            switch (castMode)
            {
                case TacticalCastMode.Self:
                    break;
                case TacticalCastMode.Position:
                case TacticalCastMode.MinimapPoint:
                    targetPosition = await SelectTargetPosition(root, config);
                    root = rootRef;
                    castComponent = castComponentRef;
                    if (root == null || root.IsDisposed || castComponent == null || castComponent.IsDisposed)
                    {
                        return;
                    }
                    break;
                default:
                    Log.Warning($"[TacticalItem] unsupported cast mode: configId={config.Id}, castMode={config.CastMode}");
                    castComponent.CancelCast();
                    return;
            }

            castComponent.CompleteCast(targetPosition);
            await SendUseRequest(root, requestItemId, targetPosition);
            castComponent = castComponentRef;
            if (castComponent == null || castComponent.IsDisposed)
            {
                return;
            }
            castComponent.CancelCast();
        }

        private static async ETTask<float3> SelectTargetPosition(Scene root, TacticalItemConfig config)
        {
            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            Unit unit = unitComponent?.Get(playerComponent?.MyId ?? 0);
            if (unit == null || unit.IsDisposed)
            {
                Log.Warning("[TacticalItem] current player unit missing");
                return float3.zero;
            }

            SpellIndicatorComponent spellIndicatorComponent = unit.GetComponent<SpellIndicatorComponent>();
            if (spellIndicatorComponent == null)
            {
                Log.Warning("[TacticalItem] spell indicator component missing");
                return float3.zero;
            }

            if (string.IsNullOrWhiteSpace(config.IndicatorPath))
            {
                Log.Warning($"[TacticalItem] indicator path missing: tacticalConfigId={config.Id}");
                return float3.zero;
            }

            EntityRef<Scene> sceneRef = currentScene;
            EntityRef<SpellIndicatorComponent> indicatorRef = spellIndicatorComponent;
            GameObject indicatorPrefab = await currentScene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(config.IndicatorPath);

            currentScene = sceneRef;
            spellIndicatorComponent = indicatorRef;
            if (currentScene == null || currentScene.IsDisposed || spellIndicatorComponent == null || spellIndicatorComponent.IsDisposed)
            {
                return float3.zero;
            }

            Vector3 position = await spellIndicatorComponent.WaitPos(indicatorPrefab, config.Radius, config.MaxDistance);
            return (float3)position;
        }

        private static async ETTask SendUseRequest(Scene root, long itemId, float3 targetPosition)
        {
            C2M_UseTacticalItem request = C2M_UseTacticalItem.Create();
            request.ItemId = itemId;
            request.TargetPosition = targetPosition;

            M2C_UseTacticalItem response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_UseTacticalItem;
            if (response == null)
            {
                Log.Warning("[TacticalItem] use failed: null response");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[TacticalItem] use failed: error={response.Error}, msg={response.Message}");
                return;
            }

            Log.Info($"[TacticalItem] use success: itemId={itemId}, target={targetPosition}");
        }
    }
}
