using System;
using System.Collections.Generic;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// Author  YIUI
    /// Date    2026.3.1
    /// Desc
    /// </summary>
    public partial class EquipSelectViewComponent : Entity
    {
        // 装备列表 LoopScroll
        public EntityRef<YIUILoopScrollChild> m_EquipLoop;
        public YIUILoopScrollChild EquipLoop => m_EquipLoop;

        // 当前选择的槽位类型
        public EquipSlotType CurrentSlotType;
        public LoadoutItemSourceMode CurrentItemSourceMode;

        // LobbyPanel 引用
        public EntityRef<LobbyPanelComponent> m_LobbyPanel;
        public LobbyPanelComponent LobbyPanel => m_LobbyPanel;

        // 待确认的装备配置Id（点击Prepared后才真正装备）
        public int PendingItemConfigId;
        public LoadoutItemSourceMode PendingItemSourceMode;
    }
}
