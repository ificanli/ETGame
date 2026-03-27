using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    [EntitySystemOf(typeof(ArchiveManagerComponent))]
    public static partial class ArchiveManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ArchiveManagerComponent self)
        {
            self.AccountToArchiveId.Clear();
        }

        [EntitySystem]
        private static void Destroy(this ArchiveManagerComponent self)
        {
            self.AccountToArchiveId.Clear();
        }

        public static PlayerArchive GetOrCreate(
            this ArchiveManagerComponent self,
            string account,
            long totalWealth,
            long lastEvacuationWealth,
            int warehouseColumnCount,
            IList<ArchiveWarehouseItemProto> warehouseItems,
            IList<ArchiveItemCountProto> lastEvacuationItems)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                return null;
            }

            if (self.AccountToArchiveId.TryGetValue(account, out long archiveId))
            {
                return self.GetChild<PlayerArchive>(archiveId);
            }

            PlayerArchive archive = self.AddChild<PlayerArchive, string>(account);
            self.AccountToArchiveId.Add(account, archive.Id);
            archive.ApplyStorageSnapshot(totalWealth, lastEvacuationWealth, warehouseColumnCount, warehouseItems, lastEvacuationItems);
            return archive;
        }

        public static PlayerArchive GetByAccount(this ArchiveManagerComponent self, string account)
        {
            if (string.IsNullOrWhiteSpace(account))
            {
                return null;
            }

            if (!self.AccountToArchiveId.TryGetValue(account, out long archiveId))
            {
                return null;
            }

            return self.GetChild<PlayerArchive>(archiveId);
        }
    }

    [EntitySystemOf(typeof(PlayerArchive))]
    [FriendOf(typeof(PlayerArchive))]
    public static partial class PlayerArchiveSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerArchive self, string account)
        {
            self.Account = account;
            self.WarehouseColumnCount = 0;
            self.WarehouseItems.Clear();
            self.LastEvacuationItems.Clear();
            self.LastEvacuationWealth = 0;
            self.TotalWealth = 0;
            self.CurrentBattleRecordId = 0;
        }

        [EntitySystem]
        private static void Destroy(this PlayerArchive self)
        {
            self.WarehouseItems.Clear();
            self.LastEvacuationItems.Clear();
            self.CurrentBattleRecordId = 0;
        }

        public static void ApplyStorageSnapshot(
            this PlayerArchive self,
            long totalWealth,
            long lastEvacuationWealth,
            int warehouseColumnCount,
            IList<ArchiveWarehouseItemProto> warehouseItems,
            IList<ArchiveItemCountProto> lastEvacuationItems)
        {
            self.TotalWealth = totalWealth;
            self.LastEvacuationWealth = lastEvacuationWealth;
            self.WarehouseColumnCount = warehouseColumnCount;

            self.WarehouseItems.Clear();
            if (warehouseItems != null)
            {
                for (int i = 0; i < warehouseItems.Count; ++i)
                {
                    ArchiveWarehouseItemProto item = warehouseItems[i];
                    if (item == null || item.ConfigId <= 0 || item.Count <= 0)
                    {
                        continue;
                    }

                    self.WarehouseItems.Add(new ArchiveWarehouseItemInfo
                    {
                        ItemUid = item.ItemUid,
                        ConfigId = item.ConfigId,
                        Count = item.Count,
                        GridWidth = item.GridWidth,
                        GridHeight = item.GridHeight,
                        AnchorSlotIndex = item.AnchorSlotIndex,
                    });
                }
            }

            self.LastEvacuationItems.Clear();
            if (lastEvacuationItems != null)
            {
                for (int i = 0; i < lastEvacuationItems.Count; ++i)
                {
                    ArchiveItemCountProto item = lastEvacuationItems[i];
                    if (item == null || item.ConfigId <= 0 || item.Count <= 0)
                    {
                        continue;
                    }

                    self.LastEvacuationItems.Add(new ArchiveItemCountInfo
                    {
                        ConfigId = item.ConfigId,
                        Count = item.Count,
                    });
                }
            }
        }

        public static void FillStorageSnapshot(this PlayerArchive self, Archive2G_GetOrCreatePlayerArchiveResponse response)
        {
            response.TotalWealth = self.TotalWealth;
            response.LastEvacuationWealth = self.LastEvacuationWealth;
            response.WarehouseColumnCount = self.WarehouseColumnCount;

            foreach (ArchiveWarehouseItemInfo item in self.WarehouseItems)
            {
                ArchiveWarehouseItemProto data = ArchiveWarehouseItemProto.Create();
                data.ItemUid = item.ItemUid;
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                data.GridWidth = item.GridWidth;
                data.GridHeight = item.GridHeight;
                data.AnchorSlotIndex = item.AnchorSlotIndex;
                response.WarehouseItems.Add(data);
            }

            foreach (ArchiveItemCountInfo item in self.LastEvacuationItems)
            {
                ArchiveItemCountProto data = ArchiveItemCountProto.Create();
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                response.LastEvacuationItems.Add(data);
            }
        }

        public static void FillStorageSnapshot(this PlayerArchive self, Archive2G_SavePlayerArchiveResponse response)
        {
            response.TotalWealth = self.TotalWealth;
            response.LastEvacuationWealth = self.LastEvacuationWealth;
            response.WarehouseColumnCount = self.WarehouseColumnCount;

            foreach (ArchiveWarehouseItemInfo item in self.WarehouseItems)
            {
                ArchiveWarehouseItemProto data = ArchiveWarehouseItemProto.Create();
                data.ItemUid = item.ItemUid;
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                data.GridWidth = item.GridWidth;
                data.GridHeight = item.GridHeight;
                data.AnchorSlotIndex = item.AnchorSlotIndex;
                response.WarehouseItems.Add(data);
            }

            foreach (ArchiveItemCountInfo item in self.LastEvacuationItems)
            {
                ArchiveItemCountProto data = ArchiveItemCountProto.Create();
                data.ConfigId = item.ConfigId;
                data.Count = item.Count;
                response.LastEvacuationItems.Add(data);
            }
        }

        public static PlayerBattleRecord StartBattle(this PlayerArchive self, long playerId, int gameMode, string mapName, long mapId, long startTime)
        {
            if (self.CurrentBattleRecordId > 0)
            {
                PlayerBattleRecord currentRecord = self.GetChild<PlayerBattleRecord>(self.CurrentBattleRecordId);
                if (currentRecord != null && currentRecord.FinishedAt == 0)
                {
                    currentRecord.Complete((int)ArchiveBattleResultType.Abandoned, false, 0, 0, startTime);
                }
            }

            PlayerBattleRecord record = self.AddChild<PlayerBattleRecord>();
            record.PlayerId = playerId;
            record.GameMode = gameMode;
            record.MapName = mapName ?? string.Empty;
            record.MapId = mapId;
            record.StartedAt = startTime > 0 ? startTime : TimeInfo.Instance.ServerNow();
            record.FinishedAt = 0;
            record.ResultType = (int)ArchiveBattleResultType.None;
            record.IsSuccess = false;
            record.KillNum = 0;
            record.TotalWealth = 0;
            record.Events.Clear();
            record.AddEvent((int)ArchiveBattleEventType.MatchStarted, playerId, mapId, $"match start: mode={gameMode}, map={record.MapName}");

            self.CurrentBattleRecordId = record.Id;
            return record;
        }

        public static PlayerBattleRecord CompleteBattle(
            this PlayerArchive self,
            long playerId,
            int resultType,
            bool isSuccess,
            long totalWealth,
            int killNum,
            long finishTime)
        {
            PlayerBattleRecord record = self.CurrentBattleRecordId > 0
                ? self.GetChild<PlayerBattleRecord>(self.CurrentBattleRecordId)
                : null;

            if (record == null)
            {
                record = self.AddChild<PlayerBattleRecord>();
                record.PlayerId = playerId;
                record.GameMode = 0;
                record.MapName = string.Empty;
                record.MapId = 0;
                record.StartedAt = finishTime > 0 ? finishTime : TimeInfo.Instance.ServerNow();
            }

            record.Complete(resultType, isSuccess, totalWealth, killNum, finishTime);
            self.CurrentBattleRecordId = 0;
            return record;
        }

        public static List<PlayerBattleRecord> GetBattleRecordList(this PlayerArchive self, int limit)
        {
            IEnumerable<PlayerBattleRecord> query = self.Children.Values.OfType<PlayerBattleRecord>()
                .OrderByDescending(static record => record.FinishedAt > 0 ? record.FinishedAt : record.StartedAt);

            if (limit > 0)
            {
                query = query.Take(limit);
            }

            return query.ToList();
        }
    }

    [EntitySystemOf(typeof(PlayerBattleRecord))]
    [FriendOf(typeof(PlayerBattleRecord))]
    public static partial class PlayerBattleRecordSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerBattleRecord self)
        {
            self.Events.Clear();
        }

        [EntitySystem]
        private static void Destroy(this PlayerBattleRecord self)
        {
            self.Events.Clear();
        }

        public static void AddEvent(this PlayerBattleRecord self, int eventType, long playerId, long value, string text)
        {
            self.Events.Add(new ArchiveBattleEventInfo
            {
                Timestamp = TimeInfo.Instance.ServerNow(),
                EventType = eventType,
                PlayerId = playerId,
                Value = value,
                Text = text ?? string.Empty,
            });
        }

        public static void Complete(this PlayerBattleRecord self, int resultType, bool isSuccess, long totalWealth, int killNum, long finishTime)
        {
            self.ResultType = resultType;
            self.IsSuccess = isSuccess;
            self.TotalWealth = totalWealth;
            self.KillNum = killNum;
            self.FinishedAt = finishTime > 0 ? finishTime : TimeInfo.Instance.ServerNow();

            int eventType = resultType switch
            {
                (int)ArchiveBattleResultType.Evacuated => (int)ArchiveBattleEventType.Evacuated,
                (int)ArchiveBattleResultType.Dead => (int)ArchiveBattleEventType.Dead,
                (int)ArchiveBattleResultType.Abandoned => (int)ArchiveBattleEventType.BattleAbandoned,
                _ => (int)ArchiveBattleEventType.None,
            };

            self.AddEvent(eventType, self.PlayerId, totalWealth, $"battle finish: result={resultType}, wealth={totalWealth}, kill={killNum}");
        }

        public static void FillSummary(this PlayerBattleRecord self, ArchiveBattleRecordSummaryProto summary)
        {
            summary.RecordId = self.Id;
            summary.PlayerId = self.PlayerId;
            summary.GameMode = self.GameMode;
            summary.MapName = self.MapName ?? string.Empty;
            summary.MapId = self.MapId;
            summary.StartedAt = self.StartedAt;
            summary.FinishedAt = self.FinishedAt;
            summary.ResultType = self.ResultType;
            summary.IsSuccess = self.IsSuccess;
            summary.KillNum = self.KillNum;
            summary.TotalWealth = self.TotalWealth;
        }

        public static void FillEvents(this PlayerBattleRecord self, IList<ArchiveBattleEventProto> target)
        {
            for (int i = 0; i < self.Events.Count; ++i)
            {
                ArchiveBattleEventInfo eventInfo = self.Events[i];
                ArchiveBattleEventProto data = ArchiveBattleEventProto.Create();
                data.Timestamp = eventInfo.Timestamp;
                data.EventType = eventInfo.EventType;
                data.PlayerId = eventInfo.PlayerId;
                data.Value = eventInfo.Value;
                data.Text = eventInfo.Text ?? string.Empty;
                target.Add(data);
            }
        }
    }
}
