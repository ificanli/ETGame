namespace ET.Client
{
    /// <summary>
    /// 当前携带态与仓库正式快照推送处理器。
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class G2C_LoadoutStateChangedHandler : MessageHandler<Scene, G2C_LoadoutStateChanged>
    {
        protected override async ETTask Run(Scene scene, G2C_LoadoutStateChanged message)
        {
            LoadoutComponent loadout = scene.GetComponent<LoadoutComponent>() ?? scene.AddComponent<LoadoutComponent>();
            LoadoutClientStateHelper.ApplyStateChanged(loadout, message);

            scene.GetComponent<ObjectWait>()?.Notify(new Wait_G2C_LoadoutStateChanged
            {
                G2C_LoadoutStateChanged = message,
            });

            await ETTask.CompletedTask;
        }
    }
}
