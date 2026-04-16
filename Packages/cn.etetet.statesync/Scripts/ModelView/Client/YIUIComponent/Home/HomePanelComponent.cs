namespace ET.Client
{
    public partial class HomePanelComponent : Entity, IUpdate
    {
        public string LastHomeSnapshot = string.Empty;
        public int SelectedHomeSlotId;
        public long SelectedHomeBuildingId;
        public int CurrentPageMode;
        public long SelectedMuseumWarehouseItemUid;
        public long SelectedRecycleWarehouseItemUid;
        public long SelectedWarehouseItemUid;
        public bool NeedTimedRefresh;
        public long NextTimedRefreshSecond;
    }
}
