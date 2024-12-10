namespace ET.Client
{
    [MessageHandler(SceneType.WOW)]
    public class M2C_ErrorViewHandler : MessageHandler<Scene, M2C_Error>
    {
        protected override async ETTask Run(Scene root, M2C_Error message)
        {
            TipsHelper.OpenSync<TipsTextViewComponent>($"<color=red>{OutputText(message.Error, message.Values.ToArray())}</color>");
            await ETTask.CompletedTask;
        }

        private static string OutputText(int key, params string[] args)
        {
            return string.Format(GetText(key), args);
        }

        private static string GetText(int key)
        {
            return TextConfigCategory.Instance.Get(key).CN;
        }
    }
}