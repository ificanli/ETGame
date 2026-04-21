namespace ET.Client
{
    /// <summary>
    /// 拾取提示面板（自定义部分）
    /// 靠近地面物品时弹出，显示物品图标+名称+拾取按钮
    /// </summary>
    public partial class PickupHintPanelComponent : Entity, IYIUIOpen
    {
        public string FocusPointId;
        public int ItemConfigId;
    }
}
