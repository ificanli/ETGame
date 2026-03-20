namespace ET.Client
{
    /// <summary>
    /// 起装来源列表展示数据。
    /// </summary>
    public struct LoadoutWarehouseItemViewData
    {
        public int ConfigId;
        public int Count;
        public string Name;
        public string Icon;
        public int SortCategory;
        public int Price;
        public bool Affordable;
        public LoadoutItemSourceMode SourceMode;
    }
}
