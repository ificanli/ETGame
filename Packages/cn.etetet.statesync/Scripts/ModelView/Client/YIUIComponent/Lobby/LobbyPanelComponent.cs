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
        Weapon = 1,      // 武器1
        Weapon2 = 2,     // 武器2
        Armor = 3,       // 防具
        Bag = 4          // 背包
    }

    public partial class LobbyPanelComponent : Entity
    {
        public EntityRef<YIUILoopScrollChild> m_HeroLoop;
        public YIUILoopScrollChild HeroLoop => m_HeroLoop;

        // 装备背包 LoopScroll
        public EntityRef<YIUILoopScrollChild> m_EquipBagLoop;
        public YIUILoopScrollChild EquipBagLoop => m_EquipBagLoop;

        // 当前正在选择的槽位类型
        public EquipSlotType CurrentSelectingSlot;

        // 背包中的装备ID列表
        public List<int> BagEquipIds = new();
    }
}
