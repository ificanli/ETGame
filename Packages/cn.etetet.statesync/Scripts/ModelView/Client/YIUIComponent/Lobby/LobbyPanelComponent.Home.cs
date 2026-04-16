using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    public partial class LobbyPanelComponent
    {
        public string LastHomeSnapshot = string.Empty;
        public long SelectedHomeBuildingId;
        public int SelectedHomeSlotId;
        public int HomeSubViewMode;
        public long HomeSubViewBuildingId;

        public RectTransform HomeRuntimeRoot;
        public RectTransform HomeBuildEntryRow;
        public RectTransform HomeBuildingListContent;

        public TMP_Text HomeHeaderSubtitleText;
        public TMP_Text HomeBuildEntryHintText;
        public TMP_Text HomeBuildingListTitleText;
        public readonly TMP_Text[] HomeStatLabelTexts = new TMP_Text[4];
        public readonly TMP_Text[] HomeStatValueTexts = new TMP_Text[4];
        public TMP_Text HomeDetailTitleText;
        public TMP_Text HomeDetailSubtitleText;
        public TMP_Text HomeDetailStatusText;
        public TMP_Text HomeDetailHintText;
        public readonly List<Button> HomeBuildPrototypeButtons = new();

        public Button HomeQuickEquipButton;
        public Button HomeQuickMatchButton;
        public Button HomeUpgradeButton;
        public Button HomeCollectButton;
        public Button HomeDemolishButton;
    }
}
