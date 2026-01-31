using System;

namespace ET.Test
{
    /// <summary>
    /// Kcp dispose behavior test.
    /// </summary>
    public class Core_Kcp_Dispose_Test : ATestHandler
    {
        private const uint TestConv = 3401;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            Kcp kcp = new Kcp(TestConv, (_, _) => { });
            if (!kcp.IsSet)
            {
                Log.Console("Kcp should be initialized");
                return 1;
            }

            try
            {
                kcp.Dispose();
            }
            catch (Exception e)
            {
                Log.Console($"Dispose should not throw, error: {e.Message}");
                return 2;
            }

            if (kcp.IsSet)
            {
                Log.Console("Kcp should be disposed");
                return 3;
            }

            try
            {
                kcp.Dispose();
            }
            catch (Exception e)
            {
                Log.Console($"Double dispose should not throw, error: {e.Message}");
                return 4;
            }

            Log.Debug("Core_Kcp_Dispose_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
