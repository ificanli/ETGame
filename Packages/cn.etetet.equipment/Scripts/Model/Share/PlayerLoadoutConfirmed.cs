using System.Collections.Generic;

namespace ET
{
    public struct PlayerLoadoutConfirmed
    {
        public long PlayerId;
        public int HeroConfigId;
        public int MainWeaponConfigId;
        public int SubWeaponConfigId;
        public int ArmorConfigId;
        public List<int> ConsumableConfigIds;
        public int BackpackConfigId;
        public int BagWidth;
        public int BagHeight;
        public List<LoadoutGridItemInfo> CarriedBagItems;
    }
}
