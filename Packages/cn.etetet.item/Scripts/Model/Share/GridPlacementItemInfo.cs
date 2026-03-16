namespace ET
{
    /// <summary>
    /// 通用二维容器中的物品摆放信息。
    /// AnchorSlotIndex 使用一维槽位索引表达锚点位置，GridWidth/GridHeight 表达占格尺寸。
    /// </summary>
    public struct GridPlacementItemInfo
    {
        public int ConfigId;
        public int Count;
        public int AnchorSlotIndex;
        public int GridWidth;
        public int GridHeight;
    }
}
