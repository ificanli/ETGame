namespace ET.Server
{
    /// <summary>
    /// 移动/堆叠物品Handler
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_MoveItemHandler : MessageLocationHandler<Unit, C2M_MoveItem, M2C_MoveItem>
    {
        protected override async ETTask Run(Unit unit, C2M_MoveItem request, M2C_MoveItem response)
        {
            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();

            // 调用MoveItem方法进行移动或堆叠
            int errorCode = ItemHelper.MoveItem(itemComponent, request.ItemId, request.ToSlot);
            
            if (errorCode != ErrorCode.ERR_Success)
            {
                response.Error = errorCode;
                return;
            }

            await ETTask.CompletedTask;
        }
    }
}