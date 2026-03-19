namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_DiscardItemHandler : MessageLocationHandler<Unit, C2M_DiscardItem, M2C_DiscardItem>
    {
        protected override async ETTask Run(Unit unit, C2M_DiscardItem request, M2C_DiscardItem response)
        {
            response.Error = await ItemHelper.TryDiscardItemToGroundAsync(unit, request.ItemId, request.Count);
        }
    }
}
