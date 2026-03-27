using System;
using System.Collections.Generic;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    public struct BattleRecordPanelOpenData
    {
        public EntityRef<LobbyPanelComponent> LobbyPanelRef;
    }

    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.26
    /// Desc
    /// </summary>
    public partial class BattleRecordPanelComponent : Entity, IYIUIOpen<BattleRecordPanelOpenData>
    {
        public EntityRef<LobbyPanelComponent> LobbyPanelRef;
        public long SelectedBattleRecordId;
        public long LoadedBattleRecordId;
        public bool HasBattleRecordDetail;
        public BattleRecordSummaryViewData CurrentBattleRecordDetail;
        public readonly List<BattleRecordSummaryViewData> BattleRecordSummaries = new();
        public readonly List<BattleRecordEventViewData> BattleRecordEvents = new();
    }
}
