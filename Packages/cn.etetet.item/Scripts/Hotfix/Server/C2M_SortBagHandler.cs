namespace ET.Server
{
    /// <summary>
    /// 整理背包Handler
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_SortBagHandler : MessageLocationHandler<Unit, C2M_SortBag, M2C_SortBag>
    {
        protected override async ETTask Run(Unit unit, C2M_SortBag request, M2C_SortBag response)
        {
            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                response.Error = ErrorCode.ERR_ComponentNotFound;
                response.Message = "ItemComponent not found";
                return;
            }

            // 调用SortBag方法进行整理
            int errorCode = ItemHelper.SortBag(itemComponent);

            if (errorCode != ErrorCode.ERR_Success)
            {
                response.Error = errorCode;
                return;
            }

            await ETTask.CompletedTask;
        }
    }
}
