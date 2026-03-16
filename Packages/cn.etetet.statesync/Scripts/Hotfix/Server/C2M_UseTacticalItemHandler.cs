namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_UseTacticalItemHandler : MessageLocationHandler<Unit, C2M_UseTacticalItem, M2C_UseTacticalItem>
    {
        protected override async ETTask Run(Unit unit, C2M_UseTacticalItem request, M2C_UseTacticalItem response)
        {
            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                response.Error = ErrorCode.ERR_ItemUseFailed;
                response.Message = "item component missing";
                return;
            }

            Item item = itemComponent.GetItemById(request.ItemId);
            if (item == null)
            {
                response.Error = ErrorCode.ERR_ItemNotFound;
                response.Message = "item not found";
                return;
            }

            TacticalItemConfig config = TacticalItemConfigCategory.Instance?.GetByItemConfigId(item.ConfigId);
            if (config == null)
            {
                response.Error = ErrorCode.ERR_TacticalItemConfigMissing;
                response.Message = "tactical item config missing";
                return;
            }

            int error = TacticalItemHelper.Use(unit, item, config, request.TargetPosition);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                response.Message = $"tactical item use failed: {error}";
            }

            await ETTask.CompletedTask;
        }
    }
}
