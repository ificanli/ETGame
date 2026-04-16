namespace ET.Server
{
    public static class RunTimeLimitMessageHelper
    {
        public static void SyncState(Unit unit, RunTimeLimitComponent runTimeLimit)
        {
            if (unit == null || unit.IsDisposed || runTimeLimit == null || runTimeLimit.IsDisposed)
            {
                return;
            }

            long remainMs = runTimeLimit.DeadlineTime - TimeInfo.Instance.ServerNow();
            if (remainMs < 0)
            {
                remainMs = 0;
            }

            M2C_RunTimeLimitState message = M2C_RunTimeLimitState.Create();
            message.IsActive = true;
            message.RemainMs = remainMs;
            MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
        }

        public static void ClearState(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            M2C_RunTimeLimitState message = M2C_RunTimeLimitState.Create();
            message.IsActive = false;
            message.RemainMs = 0;
            MapMessageHelper.NoticeClient(unit, message, NoticeType.Self);
        }
    }
}
