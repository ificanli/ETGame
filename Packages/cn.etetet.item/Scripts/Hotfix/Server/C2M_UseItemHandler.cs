namespace ET.Server
{
    /// <summary>
    /// 使用物品Handler
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_UseItemHandler : MessageLocationHandler<Unit, C2M_UseItem, M2C_UseItem>
    {
        protected override async ETTask Run(Unit unit, C2M_UseItem request, M2C_UseItem response)
        {
            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "ItemComponent not found";
                return;
            }

            // 获取指定槽位的物品
            Item item = itemComponent.GetItemBySlot(request.SlotIndex);
            if (item == null || item.IsDisposed)
            {
                response.Error = ErrorCode.ERR_ItemNotFound;
                response.Message = "item not found in slot";
                return;
            }

            // 检查数量是否足够
            if (item.Count < request.Count)
            {
                response.Error = ErrorCode.ERR_ItemNotEnough;
                response.Message = "not enough items to use";
                return;
            }

            int configId = item.ConfigId;

            // 这里可以添加物品使用的具体逻辑
            // 例如：使用药水恢复血量等

            // 使用RemoveItem方法移除物品，会自动通知客户端
            if (!itemComponent.RemoveItem(configId, request.Count))
            {
                response.Error = ErrorCode.ERR_ItemUseFailed;
                response.Message = "failed to use item";
                return;
            }

            await ETTask.CompletedTask;
        }
    }
}