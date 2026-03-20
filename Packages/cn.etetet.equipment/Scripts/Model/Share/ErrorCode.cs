namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_EquipmentSlotNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 1; // 200047001
        public const int ERR_EquipmentNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 2; // 200047002
        public const int ERR_EquipmentAlreadyEquipped = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 3; // 200047003
        public const int ERR_EquipmentCannotEquip = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 4; // 200047004
        public const int ERR_EquipmentLevelInsufficient = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 5; // 200047005
        public const int ERR_EquipmentItemNotInBag = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 6; // 200047006
        public const int ERR_EquipmentBagFull = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 7; // 200047007
        public const int ERR_EquipmentSlotEmpty = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 8; // 200047008
        public const int ERR_EquipmentInvalidSlotType = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 9; // 200047009
        public const int ERR_ItemComponentNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 10; // 200047010

        // Loadout 错误码
        public const int ERR_LoadoutHeroNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 20; // 200047020
        public const int ERR_LoadoutItemNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 21; // 200047021
        public const int ERR_LoadoutSlotMismatch = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 22; // 200047022
        public const int ERR_LoadoutNotConfirmed = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 23; // 200047023
        public const int ERR_LoadoutGridInvalid = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 24; // 200047024
        public const int ERR_LoadoutStateConflict = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 25; // 200047025
        public const int ERR_LoadoutWarehouseNotEnough = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 26; // 200047026
        public const int ERR_LoadoutAreaInvalid = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 27; // 200047027
        public const int ERR_LoadoutSourceEmpty = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 28; // 200047028
        public const int ERR_LoadoutTargetOccupied = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 29; // 200047029
        public const int ERR_LoadoutBagNotEmpty = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 30; // 200047030
        public const int ERR_LoadoutCountInvalid = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 31; // 200047031
        public const int ERR_LoadoutShopItemUnavailable = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 32; // 200047032
        public const int ERR_LoadoutWealthNotEnough = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 33; // 200047033
    }
}
