namespace ET.Server
{
    [ConfigProcess]
    public class YIUIItemExtendSingleton : Singleton<YIUIItemExtendSingleton>, ISingletonAwake
    {
        public void Awake()
        {
            Log.Info($"YIUIItemExtendSingleton 初始化完毕");
        }
    }
}
