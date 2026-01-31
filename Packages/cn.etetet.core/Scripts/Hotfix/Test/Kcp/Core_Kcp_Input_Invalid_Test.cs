using System;

namespace ET.Test
{
    /// <summary>
    /// Kcp invalid input test.
    /// </summary>
    public class Core_Kcp_Input_Invalid_Test : ATestHandler
    {
        private const uint TestConv = 3501;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            using Kcp kcp = new Kcp(TestConv, (_, _) => { });

            byte[] invalid = new byte[3];
            int result = kcp.Input(invalid);
            if (result >= 0)
            {
                Log.Console("Input should fail for invalid packet");
                return 1;
            }

            if (kcp.PeekSize() != -1)
            {
                Log.Console("PeekSize should remain -1 after invalid input");
                return 2;
            }

            Log.Debug("Core_Kcp_Input_Invalid_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
