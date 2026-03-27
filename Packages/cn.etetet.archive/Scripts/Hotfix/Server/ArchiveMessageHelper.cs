using System;
using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// Gate 侧访问 Archive 服务的统一帮助类。
    /// </summary>
    public static class ArchiveMessageHelper
    {
        private const int ArchiveServiceResolveRetryCount = 20;
        private const int ArchiveServiceResolveRetryInterval = 50;

        public static async ETTask<Archive2G_GetOrCreatePlayerArchiveResponse> GetOrCreatePlayerArchive(
            Scene root,
            string account,
            long totalWealth,
            long lastEvacuationWealth,
            int warehouseColumnCount,
            System.Collections.Generic.IList<ArchiveWarehouseItemProto> warehouseItems,
            System.Collections.Generic.IList<ArchiveItemCountProto> lastEvacuationItems)
        {
            EntityRef<Scene> rootRef = root;
            ActorId archiveActorId = await GetArchiveActor(root, account);

            root = rootRef;
            if (root == null || archiveActorId == default)
            {
                return null;
            }

            G2Archive_GetOrCreatePlayerArchiveRequest request = G2Archive_GetOrCreatePlayerArchiveRequest.Create(true);
            request.Account = account ?? string.Empty;
            request.TotalWealth = totalWealth;
            request.LastEvacuationWealth = lastEvacuationWealth;
            request.WarehouseColumnCount = warehouseColumnCount;
            if (warehouseItems != null)
            {
                request.WarehouseItems.AddRange(warehouseItems);
            }

            if (lastEvacuationItems != null)
            {
                request.LastEvacuationItems.AddRange(lastEvacuationItems);
            }

            return (Archive2G_GetOrCreatePlayerArchiveResponse)await root.GetComponent<MessageSender>().Call(archiveActorId, request);
        }

        public static async ETTask<Archive2G_SavePlayerArchiveResponse> SavePlayerArchive(
            Scene root,
            string account,
            long totalWealth,
            long lastEvacuationWealth,
            int warehouseColumnCount,
            System.Collections.Generic.IList<ArchiveWarehouseItemProto> warehouseItems,
            System.Collections.Generic.IList<ArchiveItemCountProto> lastEvacuationItems)
        {
            EntityRef<Scene> rootRef = root;
            ActorId archiveActorId = await GetArchiveActor(root, account);

            root = rootRef;
            if (root == null || archiveActorId == default)
            {
                return null;
            }

            G2Archive_SavePlayerArchiveRequest request = G2Archive_SavePlayerArchiveRequest.Create(true);
            request.Account = account ?? string.Empty;
            request.TotalWealth = totalWealth;
            request.LastEvacuationWealth = lastEvacuationWealth;
            request.WarehouseColumnCount = warehouseColumnCount;
            if (warehouseItems != null)
            {
                request.WarehouseItems.AddRange(warehouseItems);
            }

            if (lastEvacuationItems != null)
            {
                request.LastEvacuationItems.AddRange(lastEvacuationItems);
            }

            return (Archive2G_SavePlayerArchiveResponse)await root.GetComponent<MessageSender>().Call(archiveActorId, request);
        }

        public static async ETTask RecordMatchStart(
            Scene root,
            string account,
            long playerId,
            int gameMode,
            string mapName,
            long mapId)
        {
            EntityRef<Scene> rootRef = root;
            ActorId archiveActorId = await GetArchiveActor(root, account);

            root = rootRef;
            if (root == null || archiveActorId == default)
            {
                return;
            }

            G2Archive_RecordMatchStartRequest request = G2Archive_RecordMatchStartRequest.Create(true);
            request.Account = account ?? string.Empty;
            request.PlayerId = playerId;
            request.GameMode = gameMode;
            request.MapName = mapName ?? string.Empty;
            request.MapId = mapId;
            request.StartTime = TimeInfo.Instance.ServerNow();
            await root.GetComponent<MessageSender>().Call(archiveActorId, request);
        }

        public static async ETTask<long> RecordBattleResult(
            Scene root,
            string account,
            long playerId,
            int resultType,
            bool isSuccess,
            long totalWealth,
            int killNum)
        {
            EntityRef<Scene> rootRef = root;
            ActorId archiveActorId = await GetArchiveActor(root, account);

            root = rootRef;
            if (root == null || archiveActorId == default)
            {
                return 0;
            }

            G2Archive_RecordBattleResultRequest request = G2Archive_RecordBattleResultRequest.Create(true);
            request.Account = account ?? string.Empty;
            request.PlayerId = playerId;
            request.ResultType = resultType;
            request.IsSuccess = isSuccess;
            request.TotalWealth = totalWealth;
            request.KillNum = killNum;
            request.FinishTime = TimeInfo.Instance.ServerNow();
            Archive2G_RecordBattleResultResponse response =
                (Archive2G_RecordBattleResultResponse)await root.GetComponent<MessageSender>().Call(archiveActorId, request);
            return response?.RecordId ?? 0;
        }

        public static async ETTask<Archive2G_GetBattleRecordListResponse> GetBattleRecordList(Scene root, string account, int limit)
        {
            EntityRef<Scene> rootRef = root;
            ActorId archiveActorId = await GetArchiveActor(root, account);

            root = rootRef;
            if (root == null || archiveActorId == default)
            {
                return null;
            }

            G2Archive_GetBattleRecordListRequest request = G2Archive_GetBattleRecordListRequest.Create(true);
            request.Account = account ?? string.Empty;
            request.Limit = limit;
            return (Archive2G_GetBattleRecordListResponse)await root.GetComponent<MessageSender>().Call(archiveActorId, request);
        }

        public static async ETTask<Archive2G_GetBattleRecordDetailResponse> GetBattleRecordDetail(Scene root, string account, long recordId)
        {
            EntityRef<Scene> rootRef = root;
            ActorId archiveActorId = await GetArchiveActor(root, account);

            root = rootRef;
            if (root == null || archiveActorId == default)
            {
                return null;
            }

            G2Archive_GetBattleRecordDetailRequest request = G2Archive_GetBattleRecordDetailRequest.Create(true);
            request.Account = account ?? string.Empty;
            request.RecordId = recordId;
            return (Archive2G_GetBattleRecordDetailResponse)await root.GetComponent<MessageSender>().Call(archiveActorId, request);
        }

        private static async ETTask<ActorId> GetArchiveActor(Scene root, string account)
        {
            if (root == null)
            {
                return default;
            }

            int zone = root.Zone();
            if (TryGetArchiveActor(root, account, zone, out ActorId archiveActorId))
            {
                return archiveActorId;
            }

            EntityRef<Scene> rootRef = root;

            // Gate 刚启动时，Archive 的初始服务回放可能还没进入本地缓存，这里做一次短暂重试。
            for (int i = 0; i < ArchiveServiceResolveRetryCount; ++i)
            {
                await root.Root().TimerComponent.WaitAsync(ArchiveServiceResolveRetryInterval);

                root = rootRef;
                if (root == null)
                {
                    return default;
                }

                if (TryGetArchiveActor(root, account, zone, out archiveActorId))
                {
                    if (i > 0)
                    {
                        Log.Debug($"[Archive] service resolved after retry: zone={zone}, retry={i + 1}, account={account}");
                    }

                    return archiveActorId;
                }
            }

            Log.Warning($"[Archive] service not found: zone={zone}, account={account}");
            return default;
        }

        private static bool TryGetArchiveActor(Scene root, string account, int zone, out ActorId archiveActorId)
        {
            archiveActorId = default;
            ServiceDiscoveryProxy serviceDiscovery = root?.GetComponent<ServiceDiscoveryProxy>();
            List<ServiceInfo> archiveServices = serviceDiscovery?.GetBySceneTypeAndZone(SceneType.Archive, zone);
            if (archiveServices == null || archiveServices.Count == 0)
            {
                return false;
            }

            archiveServices.Sort(CompareArchiveService);
            int index = SelectArchiveIndex(account, archiveServices.Count);
            archiveActorId = archiveServices[index].ActorId;
            return archiveActorId != default;
        }

        private static int SelectArchiveIndex(string account, int count)
        {
            if (count <= 1 || string.IsNullOrWhiteSpace(account))
            {
                return 0;
            }

            return account.Mode(count);
        }

        private static int CompareArchiveService(ServiceInfo left, ServiceInfo right)
        {
            int result = string.CompareOrdinal(left?.SceneName, right?.SceneName);
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(left?.ActorId.Address.IP, right?.ActorId.Address.IP);
            if (result != 0)
            {
                return result;
            }

            result = left.ActorId.Address.Port.CompareTo(right.ActorId.Address.Port);
            if (result != 0)
            {
                return result;
            }

            result = left.ActorId.FiberInstanceId.Fiber.CompareTo(right.ActorId.FiberInstanceId.Fiber);
            if (result != 0)
            {
                return result;
            }

            return left.ActorId.FiberInstanceId.InstanceId.CompareTo(right.ActorId.FiberInstanceId.InstanceId);
        }
    }
}
