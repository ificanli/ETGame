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
    }
}