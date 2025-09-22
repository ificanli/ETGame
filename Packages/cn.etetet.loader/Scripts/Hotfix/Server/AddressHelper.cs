namespace ET.Server
{
    public static class AddressHelper
    {
        // 注意一个进程中会有多个OuterPort,所以不能只使用AddressSingleton.Instance.OuterPort
        // 如果AddressSingleton.Instance.OuterPort大于0，说明aspire有传入port，那么说明进程中只有一个Scene,并且配置了具体的port，可以直接使用
        // 否则，必须从sceneconfig中获取
        public static int GetSceneOuterPort(string sceneName)
        {
            int outerPort = AddressSingleton.Instance.OuterPort;

            if (outerPort > 0)
            {
                return outerPort;
            }

            StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(sceneName);
            return startSceneConfig.Port;
        }
    }
}