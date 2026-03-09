using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 装备槽位类型
    /// </summary>
    public enum EquipSlotType
    {
        Weapon = 1,
        Weapon2 = 2,
        Armor = 3,
        Bag = 4
    }

    public partial class LobbyPanelComponent : Entity
    {
        public EntityRef<YIUILoopScrollChild> m_HeroLoop;
        public YIUILoopScrollChild HeroLoop => m_HeroLoop;

        public EntityRef<YIUILoopScrollChild> m_EquipBagLoop;
        public YIUILoopScrollChild EquipBagLoop => m_EquipBagLoop;

        public EntityRef<YIUI3DDisplayChild> m_HeroDisplay;
        public YIUI3DDisplayChild HeroDisplay => m_HeroDisplay;

        public EquipSlotType CurrentSelectingSlot;

        public List<int> BagEquipIds = new();

        public string CurrentHeroDisplayResName = string.Empty;
        public string CurrentHeroDisplayCameraName = string.Empty;
    }
}