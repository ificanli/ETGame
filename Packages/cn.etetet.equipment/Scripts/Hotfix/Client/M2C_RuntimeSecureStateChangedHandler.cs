namespace ET.Client
{
    /// <summary>
    /// 局内运行时安全格快照变更通知。
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_RuntimeSecureStateChangedHandler : MessageHandler<Scene, M2C_RuntimeSecureStateChanged>
    {
        protected override async ETTask Run(Scene scene, M2C_RuntimeSecureStateChanged message)
        {
            LoadoutComponent loadout = scene.GetComponent<LoadoutComponent>() ?? scene.AddComponent<LoadoutComponent>();
            LoadoutClientStateHelper.ApplyRuntimeSecureState(loadout, message.SecureWidth, message.SecureHeight, message.Items);
            await ETTask.CompletedTask;
        }
    }
}
