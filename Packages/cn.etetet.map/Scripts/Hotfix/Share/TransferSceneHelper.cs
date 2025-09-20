namespace ET
{
    public static class TransferSceneHelper
    {
        public static bool IsChangeScene(string preSceneName, string nextSceneName)
        {
            MapConfig preMapConfig = null;
            if (preSceneName != null)
            {
                preMapConfig = MapConfigCategory.Instance.GetByName(preSceneName.GetMapName());
            }
            MapConfig mapConfig = MapConfigCategory.Instance.GetByName(nextSceneName.GetMapName());
            return preMapConfig?.MapResName != mapConfig?.MapResName;
        }

        public static string GetMapName(this string sceneName)
        {
            return sceneName.Split("@")[0];
        }
    }
}