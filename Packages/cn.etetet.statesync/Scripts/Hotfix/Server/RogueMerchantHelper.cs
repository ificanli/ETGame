using System.Collections.Generic;
using System.Globalization;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 肉鸽临时商人生成辅助。
    /// </summary>
    public static class RogueMerchantHelper
    {
        private const string MerchantPointPrefix = "rogue_merchant_";
        private const float DefaultSpawnDistance = 2.5f;
        private const float DefaultInteractRange = 3f;

        public static bool TrySpawnMerchant(Unit owner, Buff buff, BTRogueSpawnMerchant node)
        {
            if (owner == null || owner.IsDisposed || owner.UnitType != UnitType.Player || node == null || node.RewardGold <= 0)
            {
                return false;
            }

            Scene scene = owner.Scene();
            ECAManagerComponent ecaManager = scene?.GetComponent<ECAManagerComponent>();
            if (scene == null || scene.IsDisposed || ecaManager == null)
            {
                return false;
            }

            int npcConfigId = ResolveNpcConfigId(node.NpcConfigId);
            if (npcConfigId <= 0)
            {
                Log.Warning($"[RogueMerchant] npc config missing, owner={owner.Id}, source={buff?.ConfigId ?? 0}");
                return false;
            }

            Unit merchant = UnitFactory.Create(scene, IdGenerater.Instance.GenerateId(), npcConfigId);
            if (merchant == null || merchant.IsDisposed)
            {
                return false;
            }

            merchant.UnitType = UnitType.NPC;
            merchant.Position = ResolveSpawnPosition(owner, node.SpawnDistance);
            merchant.Rotation = owner.Rotation;
            merchant.GetComponent<UnitSpawnPointComponent>()?.SetSpawnPoint(merchant.Position, merchant.Rotation);

            float interactRange = node.InteractRange > 0f ? node.InteractRange : DefaultInteractRange;
            string pointId = $"{MerchantPointPrefix}{owner.Id}_{merchant.Id}";
            ECAPointComponent point = merchant.AddComponent<ECAPointComponent, string, int, float>(pointId, ECAPointType.RangeTrigger, interactRange);
            point.Params = CreatePointParams(owner.Id, node.RewardGold, node.RewardOnce, node.DestroyAfterReward);
            point.FlowGraph = CreateFlowGraph(node.ButtonTextId);

            ecaManager.AddECAPoint(pointId, merchant.Id);
            ECAHelper.CheckPlayerInRange(owner);

            Log.Info(
                $"[RogueMerchant] spawned owner={owner.Id}, merchant={merchant.Id}, point={pointId}, npcConfigId={npcConfigId}, rewardGold={node.RewardGold}, pos={merchant.Position}, sourceBuff={buff?.ConfigId ?? 0}");
            return true;
        }

        private static int ResolveNpcConfigId(int configuredNpcConfigId)
        {
            UnitConfigCategory configCategory = UnitConfigCategory.Instance;
            if (configCategory?.DataList == null)
            {
                return 0;
            }

            if (configuredNpcConfigId > 0)
            {
                UnitConfig configuredNpc = configCategory.GetOrDefault(configuredNpcConfigId);
                if (configuredNpc != null && configuredNpc.UnitType == UnitType.NPC)
                {
                    return configuredNpcConfigId;
                }
            }

            foreach (UnitConfig unitConfig in configCategory.DataList)
            {
                if (unitConfig != null && unitConfig.UnitType == UnitType.NPC)
                {
                    return unitConfig.Id;
                }
            }

            return 0;
        }

        private static float3 ResolveSpawnPosition(Unit owner, float configuredSpawnDistance)
        {
            SpawnPointManagerComponent spawnPointManager = owner.Scene()?.GetComponent<SpawnPointManagerComponent>();
            if (TryResolveMerchantCandidateSpawnPosition(owner, spawnPointManager, out float3 candidatePosition))
            {
                return candidatePosition;
            }

            float spawnDistance = configuredSpawnDistance > 0f ? configuredSpawnDistance : DefaultSpawnDistance;
            float3 fallback = owner.Position;
            float3 forward = owner.Forward;
            if (!math.all(math.isfinite(forward)))
            {
                return fallback;
            }

            forward.y = 0f;
            if (math.lengthsq(forward) <= 0.0001f)
            {
                forward = new float3(1f, 0f, 0f);
            }
            else
            {
                forward = math.normalize(forward);
            }

            float3 candidate = fallback + forward * spawnDistance;
            return TryProjectSpawnPosition(owner, candidate, out float3 projected) ? projected : fallback;
        }

        private static bool TryResolveMerchantCandidateSpawnPosition(Unit owner, SpawnPointManagerComponent spawnPointManager, out float3 resolvedPosition)
        {
            resolvedPosition = default;
            if (owner == null || owner.IsDisposed || spawnPointManager == null || spawnPointManager.RogueMerchantSpawnPoints == null)
            {
                return false;
            }

            int candidateCount = spawnPointManager.RogueMerchantSpawnPoints.Count;
            if (candidateCount == 0)
            {
                return false;
            }

            int startIndex = RandomGenerator.RandomNumber(0, candidateCount);
            for (int offset = 0; offset < candidateCount; ++offset)
            {
                SpawnPointECAConfig spawnPoint = spawnPointManager.RogueMerchantSpawnPoints[(startIndex + offset) % candidateCount];
                float3 candidate = new float3(spawnPoint.PosX, spawnPoint.PosY, spawnPoint.PosZ);
                if (TryProjectSpawnPosition(owner, candidate, out resolvedPosition))
                {
                    return true;
                }

                if (owner.GetComponent<PathfindingComponent>() == null)
                {
                    resolvedPosition = candidate;
                    return true;
                }
            }

            Log.Warning($"[RogueMerchant] rogue merchant spawn points configured but none resolved on navmesh, owner={owner.Id}, scene={owner.Scene()?.Name}");
            return false;
        }

        private static bool TryProjectSpawnPosition(Unit owner, float3 candidate, out float3 projected)
        {
            projected = candidate;
            PathfindingComponent pathfinding = owner.GetComponent<PathfindingComponent>();
            if (pathfinding == null)
            {
                return false;
            }

            float unitRadius = owner.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            return pathfinding.TryRecastFindNearestPointForMovement(candidate, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPointForSpawn(candidate, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPoint(owner.Position, unitRadius, out projected, out _) ||
                   pathfinding.TryRecastFindNearestPointForSpawn(owner.Position, unitRadius, out projected, out _);
        }

        private static List<FlowParam> CreatePointParams(long ownerPlayerId, int rewardGold, bool rewardOnce, bool destroyAfterReward)
        {
            return new List<FlowParam>
            {
                new FlowParam
                {
                    Key = RogueECAPointParamKey.InteractGold,
                    Value = rewardGold.ToString(CultureInfo.InvariantCulture),
                },
                new FlowParam
                {
                    Key = RogueECAPointParamKey.RewardOnce,
                    Value = rewardOnce ? "1" : "0",
                },
                new FlowParam
                {
                    Key = RogueECAPointParamKey.OwnerPlayerId,
                    Value = ownerPlayerId.ToString(CultureInfo.InvariantCulture),
                },
                new FlowParam
                {
                    Key = RogueECAPointParamKey.DestroyAfterReward,
                    Value = destroyAfterReward ? "1" : "0",
                },
            };
        }

        private static FlowGraphData CreateFlowGraph(int buttonTextId)
        {
            List<FlowParam> showButtonParams = new();
            if (buttonTextId > 0)
            {
                showButtonParams.Add(new FlowParam
                {
                    Key = "button_text_id",
                    Value = buttonTextId.ToString(CultureInfo.InvariantCulture),
                });
            }

            return new FlowGraphData
            {
                Nodes = new List<FlowNodeData>
                {
                    new FlowNodeData
                    {
                        NodeId = 1,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerEnterRange,
                    },
                    new FlowNodeData
                    {
                        NodeId = 2,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.ShowInteractButton,
                        Params = showButtonParams,
                    },
                    new FlowNodeData
                    {
                        NodeId = 3,
                        NodeType = ECAFlowNodeType.Event,
                        NodeKey = ECAFlowEventType.OnPlayerLeaveRange,
                    },
                    new FlowNodeData
                    {
                        NodeId = 4,
                        NodeType = ECAFlowNodeType.Action,
                        NodeKey = ECAFlowActionKey.HideInteractButton,
                    },
                },
                Connections = new List<FlowConnectionData>
                {
                    new FlowConnectionData { FromNodeId = 1, ToNodeId = 2, Branch = "Out" },
                    new FlowConnectionData { FromNodeId = 3, ToNodeId = 4, Branch = "Out" },
                },
            };
        }
    }
}
