namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_EquipmentSlotNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 1;
        public const int ERR_EquipmentNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 2;
        public const int ERR_EquipmentAlreadyEquipped = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 3;
        public const int ERR_EquipmentCannotEquip = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 4;
        public const int ERR_EquipmentLevelInsufficient = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 5;
        public const int ERR_EquipmentItemNotInBag = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 6;
        public const int ERR_EquipmentBagFull = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 7;
        public const int ERR_EquipmentSlotEmpty = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 8;
        public const int ERR_EquipmentInvalidSlotType = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 9;
        public const int ERR_ItemComponentNotFound = ErrorCode.ERR_WithoutException + PackageType.Equipment * 1000 + 10;
    }
}