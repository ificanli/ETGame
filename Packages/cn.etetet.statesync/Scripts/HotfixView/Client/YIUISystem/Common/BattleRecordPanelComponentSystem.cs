using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.26
    /// Desc
    /// </summary>
    [FriendOf(typeof(BattleRecordPanelComponent))]
    public static partial class BattleRecordPanelComponentSystem
    {
        private const int BATTLE_RECORD_LIMIT = 20;

        [EntitySystem]
        private static void YIUIInitialize(this BattleRecordPanelComponent self)
        {
            if (self.u_ComListItemTemplate != null)
            {
                self.u_ComListItemTemplate.gameObject.SetActive(false);
            }

            if (self.u_ComTimelineItemTemplate != null)
            {
                self.u_ComTimelineItemTemplate.gameObject.SetActive(false);
            }

            EntityRef<BattleRecordPanelComponent> selfRef = self;
            if (self.u_ComMaskButton != null)
            {
                self.u_ComMaskButton.onClick.RemoveAllListeners();
                self.u_ComMaskButton.onClick.AddListener(() => OnCloseClicked(selfRef));
            }

            if (self.u_ComCloseButton != null)
            {
                self.u_ComCloseButton.onClick.RemoveAllListeners();
                self.u_ComCloseButton.onClick.AddListener(() => OnCloseClicked(selfRef));
            }

            self.UIBase?.SetActive(false);
        }

        [EntitySystem]
        private static void Destroy(this BattleRecordPanelComponent self)
        {
            self.BattleRecordSummaries.Clear();
            self.BattleRecordEvents.Clear();
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this BattleRecordPanelComponent self)
        {
            await ETTask.CompletedTask;
            return true;
        }

        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this BattleRecordPanelComponent self, BattleRecordPanelOpenData openData)
        {
            self.LobbyPanelRef = openData.LobbyPanelRef;
            self.UIBase?.SetActive(true);
            self.RefreshBattleRecordHintText("正在加载战绩...");

            EntityRef<BattleRecordPanelComponent> selfRef = self;
            await self.RequestBattleRecordListAsync();
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return false;
            }

            self.RefreshBattleRecordListView();
            return true;
        }

        private static async ETTask RequestBattleRecordListAsync(this BattleRecordPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            C2G_GetBattleRecordList request = C2G_GetBattleRecordList.Create();
            request.Limit = BATTLE_RECORD_LIMIT;
            EntityRef<BattleRecordPanelComponent> selfRef = self;

            G2C_GetBattleRecordList response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(request) as G2C_GetBattleRecordList;
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                self.ApplyBattleRecordListError("战绩服务无响应");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                self.ApplyBattleRecordListError(string.IsNullOrWhiteSpace(response.Message) ? "战绩加载失败" : response.Message);
                return;
            }

            self.BattleRecordSummaries.Clear();
            for (int i = 0; i < response.Records.Count; ++i)
            {
                ArchiveBattleRecordSummaryProto record = response.Records[i];
                if (record == null)
                {
                    continue;
                }

                self.BattleRecordSummaries.Add(ToBattleRecordSummary(record));
            }

            if (self.BattleRecordSummaries.Count == 0)
            {
                self.SelectedBattleRecordId = 0;
                self.LoadedBattleRecordId = 0;
                self.HasBattleRecordDetail = false;
                self.BattleRecordEvents.Clear();
                self.RefreshBattleRecordHintText("当前档案为运行时内存，暂无战绩，进程重启后会清空。");
                return;
            }

            bool exists = false;
            for (int i = 0; i < self.BattleRecordSummaries.Count; ++i)
            {
                if (self.BattleRecordSummaries[i].RecordId == self.SelectedBattleRecordId)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                self.SelectedBattleRecordId = self.BattleRecordSummaries[0].RecordId;
            }

            self.RefreshBattleRecordHintText($"当前档案为运行时内存，已加载 {self.BattleRecordSummaries.Count} 场对局，进程重启后会清空。");
            await self.RequestBattleRecordDetailAsync(self.SelectedBattleRecordId);
        }

        private static async ETTask RequestBattleRecordDetailAsync(this BattleRecordPanelComponent self, long recordId)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (recordId <= 0)
            {
                self.HasBattleRecordDetail = false;
                self.LoadedBattleRecordId = 0;
                self.BattleRecordEvents.Clear();
                return;
            }

            C2G_GetBattleRecordDetail request = C2G_GetBattleRecordDetail.Create();
            request.RecordId = recordId;
            EntityRef<BattleRecordPanelComponent> selfRef = self;

            G2C_GetBattleRecordDetail response =
                    await self.Root().GetComponent<ClientSenderComponent>().Call(request) as G2C_GetBattleRecordDetail;
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (response == null)
            {
                self.ApplyBattleRecordDetailError("战绩详情无响应");
                return;
            }

            if (response.Error != ErrorCode.ERR_Success || response.Record == null)
            {
                self.ApplyBattleRecordDetailError(string.IsNullOrWhiteSpace(response.Message) ? "战绩详情加载失败" : response.Message);
                return;
            }

            self.CurrentBattleRecordDetail = ToBattleRecordSummary(response.Record);
            self.LoadedBattleRecordId = response.Record.RecordId;
            self.HasBattleRecordDetail = true;
            self.BattleRecordEvents.Clear();
            for (int i = 0; i < response.Events.Count; ++i)
            {
                ArchiveBattleEventProto battleEvent = response.Events[i];
                if (battleEvent == null)
                {
                    continue;
                }

                self.BattleRecordEvents.Add(new BattleRecordEventViewData
                {
                    Timestamp = battleEvent.Timestamp,
                    EventType = battleEvent.EventType,
                    PlayerId = battleEvent.PlayerId,
                    Value = battleEvent.Value,
                    Text = battleEvent.Text ?? string.Empty,
                });
            }
        }

        private static void RefreshBattleRecordListView(this BattleRecordPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            ClearRuntimeChildren(self.u_ComListContent, self.u_ComListItemTemplate);
            bool hasRecords = self.BattleRecordSummaries.Count > 0;
            if (self.u_ComListEmptyText != null)
            {
                self.u_ComListEmptyText.text = hasRecords ? string.Empty : "暂无战绩";
                self.u_ComListEmptyText.gameObject.SetActive(!hasRecords);
            }

            for (int i = 0; i < self.BattleRecordSummaries.Count; ++i)
            {
                BattleRecordSummaryViewData record = self.BattleRecordSummaries[i];
                RectTransform itemView = UnityEngine.Object.Instantiate(self.u_ComListItemTemplate, self.u_ComListContent);
                itemView.gameObject.SetActive(true);
                itemView.name = $"Record_{record.RecordId}";

                Image itemImage = itemView.GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.color = record.RecordId == self.SelectedBattleRecordId
                        ? new Color32(188, 154, 84, 255)
                        : new Color32(62, 72, 92, 255);
                }

                TMP_Text label = itemView.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = BuildBattleRecordListLabel(record);
                    label.color = record.RecordId == self.SelectedBattleRecordId
                        ? new Color32(28, 22, 14, 255)
                        : new Color32(239, 240, 244, 255);
                }

                Button button = itemView.GetComponent<Button>();
                if (button != null)
                {
                    EntityRef<BattleRecordPanelComponent> selfRef = self;
                    long selectedRecordId = record.RecordId;
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnBattleRecordItemClicked(selfRef, selectedRecordId));
                }
            }

            self.RefreshBattleRecordDetailView();
        }

        private static async ETTask SelectBattleRecordAsync(this BattleRecordPanelComponent self, long recordId)
        {
            if (self == null || self.IsDisposed || recordId <= 0)
            {
                return;
            }

            self.SelectedBattleRecordId = recordId;
            self.RefreshBattleRecordListView();

            EntityRef<BattleRecordPanelComponent> selfRef = self;
            await self.RequestBattleRecordDetailAsync(recordId);
            self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.RefreshBattleRecordDetailView();
        }

        private static void RefreshBattleRecordDetailView(this BattleRecordPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            ClearRuntimeChildren(self.u_ComTimelineContent, self.u_ComTimelineItemTemplate);

            if (!self.HasBattleRecordDetail || self.LoadedBattleRecordId != self.SelectedBattleRecordId)
            {
                if (self.u_ComDetailSummaryText != null)
                {
                    self.u_ComDetailSummaryText.text = string.Empty;
                }

                if (self.u_ComDetailEmptyText != null)
                {
                    self.u_ComDetailEmptyText.text = self.BattleRecordSummaries.Count > 0
                        ? "选择左侧一场对局查看详情。"
                        : "暂无可展示的战绩详情。";
                    self.u_ComDetailEmptyText.gameObject.SetActive(true);
                }

                return;
            }

            if (self.u_ComDetailSummaryText != null)
            {
                self.u_ComDetailSummaryText.text = BuildBattleRecordDetailSummary(self.CurrentBattleRecordDetail);
            }

            if (self.u_ComDetailEmptyText != null)
            {
                bool hasEvents = self.BattleRecordEvents.Count > 0;
                self.u_ComDetailEmptyText.text = hasEvents ? string.Empty : "该场战斗当前没有更多时间轴事件。";
                self.u_ComDetailEmptyText.gameObject.SetActive(!hasEvents);
            }

            for (int i = 0; i < self.BattleRecordEvents.Count; ++i)
            {
                BattleRecordEventViewData battleEvent = self.BattleRecordEvents[i];
                RectTransform itemView = UnityEngine.Object.Instantiate(self.u_ComTimelineItemTemplate, self.u_ComTimelineContent);
                itemView.gameObject.SetActive(true);
                itemView.name = $"Event_{i}";

                TMP_Text[] texts = itemView.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length > 0)
                {
                    texts[0].text = FormatBattleRecordTime(battleEvent.Timestamp, "HH:mm:ss");
                }

                if (texts.Length > 1)
                {
                    texts[1].text = BuildBattleRecordEventText(battleEvent);
                }
            }
        }

        private static void ApplyBattleRecordListError(this BattleRecordPanelComponent self, string message)
        {
            self.BattleRecordSummaries.Clear();
            self.BattleRecordEvents.Clear();
            self.HasBattleRecordDetail = false;
            self.SelectedBattleRecordId = 0;
            self.LoadedBattleRecordId = 0;
            self.RefreshBattleRecordHintText(message);
        }

        private static void ApplyBattleRecordDetailError(this BattleRecordPanelComponent self, string message)
        {
            self.HasBattleRecordDetail = false;
            self.LoadedBattleRecordId = 0;
            self.BattleRecordEvents.Clear();
            if (self.u_ComDetailSummaryText != null)
            {
                self.u_ComDetailSummaryText.text = string.Empty;
            }

            if (self.u_ComDetailEmptyText != null)
            {
                self.u_ComDetailEmptyText.text = message;
                self.u_ComDetailEmptyText.gameObject.SetActive(true);
            }
        }

        private static void RefreshBattleRecordHintText(this BattleRecordPanelComponent self, string text)
        {
            if (self?.u_ComHintText != null)
            {
                self.u_ComHintText.text = text ?? string.Empty;
            }
        }

        private static void CloseSelf(this BattleRecordPanelComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            LobbyPanelComponent lobbyPanel = self.LobbyPanelRef;
            if (lobbyPanel != null && !lobbyPanel.IsDisposed)
            {
                lobbyPanel.CloseBattleRecordOverlay();
                return;
            }

            self.UIBase?.SetActive(false);
        }

        private static void ClearRuntimeChildren(RectTransform contentRoot, RectTransform template)
        {
            if (contentRoot == null)
            {
                return;
            }

            for (int i = contentRoot.childCount - 1; i >= 0; --i)
            {
                Transform child = contentRoot.GetChild(i);
                if (template != null && child == template)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static BattleRecordSummaryViewData ToBattleRecordSummary(ArchiveBattleRecordSummaryProto record)
        {
            return new BattleRecordSummaryViewData
            {
                RecordId = record.RecordId,
                PlayerId = record.PlayerId,
                GameMode = record.GameMode,
                MapName = record.MapName ?? string.Empty,
                MapId = record.MapId,
                StartedAt = record.StartedAt,
                FinishedAt = record.FinishedAt,
                ResultType = record.ResultType,
                IsSuccess = record.IsSuccess,
                KillNum = record.KillNum,
                TotalWealth = record.TotalWealth,
            };
        }

        private static string BuildBattleRecordListLabel(BattleRecordSummaryViewData record)
        {
            return $"{FormatBattleRecordTime(record.StartedAt, "MM-dd HH:mm")}\n{GetBattleRecordGameModeText(record.GameMode)} | {GetBattleRecordResultText(record.ResultType, record.IsSuccess)} | 击杀 {record.KillNum} | 财富 {record.TotalWealth}";
        }

        private static string BuildBattleRecordDetailSummary(BattleRecordSummaryViewData record)
        {
            string started = FormatBattleRecordTime(record.StartedAt, "yyyy-MM-dd HH:mm:ss");
            string finished = record.FinishedAt > 0
                ? FormatBattleRecordTime(record.FinishedAt, "yyyy-MM-dd HH:mm:ss")
                : "进行中";
            long durationMs = record.FinishedAt > record.StartedAt ? record.FinishedAt - record.StartedAt : 0;
            string duration = durationMs > 0 ? $"{durationMs / 1000f:F1} 秒" : "未知";
            string mapName = string.IsNullOrWhiteSpace(record.MapName) ? $"Map#{record.MapId}" : record.MapName;
            return
                $"模式：{GetBattleRecordGameModeText(record.GameMode)}\n" +
                $"地图：{mapName}\n" +
                $"结果：{GetBattleRecordResultText(record.ResultType, record.IsSuccess)}\n" +
                $"开始：{started}\n" +
                $"结束：{finished}\n" +
                $"时长：{duration}\n" +
                $"击杀：{record.KillNum}\n" +
                $"财富：{record.TotalWealth}";
        }

        private static string BuildBattleRecordEventText(BattleRecordEventViewData battleEvent)
        {
            if (!string.IsNullOrWhiteSpace(battleEvent.Text))
            {
                return battleEvent.Text;
            }

            return battleEvent.EventType switch
            {
                1 => "开始匹配",
                2 => "对局中断",
                3 => $"成功撤离，财富 {battleEvent.Value}",
                4 => "战败阵亡",
                _ => $"事件 {battleEvent.EventType}",
            };
        }

        private static string GetBattleRecordGameModeText(int gameMode)
        {
            return gameMode switch
            {
                GameModeType.PVE => "PVE",
                GameModeType.OneVsOne => "1v1",
                GameModeType.ThreeVsThree => "3v3",
                GameModeType.Extraction => "搜打撤",
                _ => $"模式{gameMode}",
            };
        }

        private static string GetBattleRecordResultText(int resultType, bool isSuccess)
        {
            return resultType switch
            {
                1 => isSuccess ? "撤离成功" : "撤离结束",
                2 => "战败阵亡",
                3 => "中断放弃",
                _ => isSuccess ? "完成" : "未知",
            };
        }

        private static string FormatBattleRecordTime(long timestamp, string format)
        {
            if (timestamp <= 0)
            {
                return "--";
            }

            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToLocalTime().ToString(format);
        }

        private static void OnCloseClicked(EntityRef<BattleRecordPanelComponent> selfRef)
        {
            BattleRecordPanelComponent self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.CloseSelf();
        }

        private static void OnBattleRecordItemClicked(EntityRef<BattleRecordPanelComponent> selfRef, long recordId)
        {
            BattleRecordPanelComponent self = selfRef;
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.SelectBattleRecordAsync(recordId).Coroutine();
        }

        #region YIUIEvent开始
        #endregion YIUIEvent结束
    }
}
