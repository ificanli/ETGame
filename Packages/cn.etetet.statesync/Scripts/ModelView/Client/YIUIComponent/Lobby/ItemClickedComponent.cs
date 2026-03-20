using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    public struct ItemClickedOpenData
    {
        public EntityRef<LobbyPanelComponent> LobbyPanelRef;
        public int ConfigId;
        public long ItemUid;
        public bool AllowEquipAction;
    }

    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.19
    /// Desc
    /// </summary>
    public partial class ItemClickedComponent : Entity, IYIUIOpen<ItemClickedOpenData>
    {
        public EntityRef<LobbyPanelComponent> LobbyPanelRef;
        public int ConfigId;
        public long ItemUid;
        public bool AllowEquipAction;
        public EquipSlotType TargetEquipSlot;
        public Image IconImage;
        public string LoadedIconName = string.Empty;
        public Sprite LoadedSprite;
    }
}
