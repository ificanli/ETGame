namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_ErrorHandler: MessageHandler<Scene, M2C_Error>
    {
        protected override async ETTask Run(Scene root, M2C_Error message)
        {
            TextHelper.OutputText(message.Error, message.Values.ToArray());
            await ETTask.CompletedTask;
        }
    }
}