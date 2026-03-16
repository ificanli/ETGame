namespace ET.Client
{
    /// <summary>
    /// 死亡结算消息处理器（客户端接收服务端死亡通知）
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_DeathSettlementHandler : MessageHandler<Scene, M2C_DeathSettlement>
    {
        protected override async ETTask Run(Scene scene, M2C_DeathSettlement message)
        {
            SettlementClientComponent settlement = scene.GetComponent<SettlementClientComponent>();
            if (settlement == null)
            {
                settlement = scene.AddComponent<SettlementClientComponent>();
            }

            settlement.SetSettlement(false, 0, 0);
            EventSystem.Instance.Publish(scene, new EventSettlementPopup());

            // 通知等待中的测试
            scene.GetComponent<ObjectWait>()?.Notify(new Wait_M2C_DeathSettlement
            {
                M2C_DeathSettlement = message,
            });

            await ETTask.CompletedTask;
        }
    }
}
