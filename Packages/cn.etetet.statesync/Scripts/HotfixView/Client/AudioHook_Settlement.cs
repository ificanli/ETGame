namespace ET.Client
{
    /// <summary>
    /// AUD-015 玩家死亡 / AUD-026 撤离成功：
    /// 监听 EventSettlementPopup，根据 IsSuccess 播放不同音效。
    /// </summary>
    [Event(SceneType.Client)]
    public class EventSettlementPopup_Audio : AEvent<Scene, EventSettlementPopup>
    {
        protected override async ETTask Run(Scene root, EventSettlementPopup args)
        {
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            SettlementClientComponent runtime = root.GetComponent<SettlementClientComponent>();
            bool isSuccess = runtime != null && runtime.HasSettlement && runtime.IsSuccess;

            if (isSuccess)
            {
                AudioHelper.PlaySfx(root, AudioEventId.SfxEvacSuccess);
            }
            else
            {
                AudioHelper.PlaySfx(root, AudioEventId.SfxPlayerDeath);
            }

            await ETTask.CompletedTask;
        }
    }
}
