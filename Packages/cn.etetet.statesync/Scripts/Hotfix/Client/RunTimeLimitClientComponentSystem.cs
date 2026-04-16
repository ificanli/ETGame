namespace ET.Client
{
    [EntitySystemOf(typeof(RunTimeLimitClientComponent))]
    public static partial class RunTimeLimitClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RunTimeLimitClientComponent self)
        {
            self.IsActive = false;
            self.RemainMs = 0;
            self.EndTimeMs = 0;
        }

        public static void ApplyState(this RunTimeLimitClientComponent self, bool isActive, long remainMs)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            if (!isActive)
            {
                self.ClearState();
                return;
            }

            long clampedRemainMs = remainMs > 0 ? remainMs : 0;
            self.IsActive = true;
            self.RemainMs = clampedRemainMs;
            self.EndTimeMs = TimeInfo.Instance.ClientNow() + clampedRemainMs;
        }

        public static void ClearState(this RunTimeLimitClientComponent self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.IsActive = false;
            self.RemainMs = 0;
            self.EndTimeMs = 0;
        }
    }
}
