namespace ET.Client
{
    public static class RunTimeLimitClientHelper
    {
        public static RunTimeLimitClientComponent GetOrAddRuntime(Scene root)
        {
            if (root == null)
            {
                return null;
            }

            RunTimeLimitClientComponent runtime = root.GetComponent<RunTimeLimitClientComponent>();
            if (runtime == null)
            {
                runtime = root.AddComponent<RunTimeLimitClientComponent>();
            }

            return runtime;
        }
    }
}
