namespace ET.Client
{
    /// <summary>
    /// 撤离结算消息处理器（客户端接收服务端结算通知）
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_EvacuationSettlementHandler : MessageHandler<Scene, M2C_EvacuationSettlement>
    {
        protected override async ETTask Run(Scene scene, M2C_EvacuationSettlement message)
        {
            // 更新客户端起装组件的结算状态
            LoadoutComponent loadout = scene.GetComponent<LoadoutComponent>();
            if (loadout != null)
            {
                loadout.IsConfirmed = false; // 撤离后重置起装状态
            }

            // 通知等待中的测试
            scene.GetComponent<ObjectWait>()?.Notify(new Wait_M2C_EvacuationSettlement
            {
                M2C_EvacuationSettlement = message,
            });

            await ETTask.CompletedTask;
        }
    }
}
