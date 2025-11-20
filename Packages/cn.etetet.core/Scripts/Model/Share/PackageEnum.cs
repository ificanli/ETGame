namespace ET
{
    [CodeProcess]
    public class PackageEnum: EnumSingleton<PackageEnum>, ISingletonAwake
    {
        public void Awake()
        {
            Init(typeof(PackageType));
        }
    }
}