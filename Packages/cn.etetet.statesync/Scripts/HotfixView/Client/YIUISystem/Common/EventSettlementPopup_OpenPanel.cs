namespace ET.Client
{
    [Event(SceneType.Client)]
    public class EventSettlementPopup_OpenPanel : AEvent<Scene, EventSettlementPopup>
    {
        protected override async ETTask Run(Scene root, EventSettlementPopup args)
        {
            await SettlementPanelHelper.TryOpenOrRefresh(root);
        }
    }
}
