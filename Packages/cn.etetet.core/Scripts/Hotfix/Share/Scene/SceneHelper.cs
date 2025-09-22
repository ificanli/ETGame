namespace ET
{
    public static class SceneHelper
    {
        public static string GetSceneConfigName(this string sceneName)
        {
            return sceneName.Split("@")[0];
        }
    }
}