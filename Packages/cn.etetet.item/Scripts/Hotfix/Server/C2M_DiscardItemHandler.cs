namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_DiscardItemHandler : MessageLocationHandler<Unit, C2M_DiscardItem, M2C_DiscardItem>
    {
        protected override async ETTask Run(Unit unit, C2M_DiscardItem request, M2C_DiscardItem response)
        {
            if (request.Count <= 0)
            {
                response.Error = ErrorCode.ERR_ItemUseCountInvalid;
                return;
            }

            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            Item item = itemComponent.GetItemById(request.ItemId);
            if (item == null)
            {
                response.Error = ErrorCode.ERR_ItemNotFound;
                return;
            }

            int discardCount = request.Count;
            if (discardCount >= item.Count)
            {
                ItemHelper.RemoveItemById(itemComponent, request.ItemId, ItemChangeReason.DropItem);
            }
            else
            {
                item.ReduceCount(discardCount);
                ItemHelper.NotifyItemUpdate(itemComponent, item);
            }

            await ETTask.CompletedTask;
        }
    }
}