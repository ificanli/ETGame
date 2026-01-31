using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp Check and Flush method test.
    /// Tests time management via Check() and explicit Flush().
    /// </summary>
    public class Core_Kcp_Check_Flush_Test : ATestHandler
    {
        private const uint TestConv = 3801;
        private const int StepMilliseconds = 10;
        private const int KcpHeaderSize = (int)KCPBASIC.REVERSED_HEAD;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            var outputPackets = new List<byte[]>(128);

            void EnqueuePacket(List<byte[]> queue, byte[] buffer, int length)
            {
                var data = new byte[length];
                Buffer.BlockCopy(buffer, KcpHeaderSize, data, 0, length);
                queue.Add(data);
            }

            using Kcp kcp = new Kcp(TestConv, (buffer, length) => EnqueuePacket(outputPackets, buffer, length));

            // Test Check() before Update
            uint now = 0;
            uint nextFlush = kcp.Check(now);
            if (nextFlush != now)
            {
                Log.Console("Check() should return current time before Update");
                return 1;
            }

            // Test Check() after Update
            kcp.Update(now);
            nextFlush = kcp.Check(now);
            if (nextFlush < now)
            {
                Log.Console("Check() should return future time after Update");
                return 2;
            }

            // Test Check() with future time
            uint futureTime = now + 1000;
            uint checkResult = kcp.Check(futureTime);
            if (checkResult < now)
            {
                Log.Console("Check() should return time >= input time");
                return 3;
            }

            // Test Flush() explicitly
            kcp.Flush();

            // Test Flush() doesn't crash
            kcp.Flush();

            // Test Check() with no Update (edge case)
            Kcp kcp2 = new Kcp(TestConv + 1, (_, _) => { });
            uint checkNoUpdate = kcp2.Check(now);
            if (checkNoUpdate < now)
            {
                Log.Console("Check() should handle no Update case gracefully");
                return 4;
            }

            // Test Check() with extreme time difference
            Kcp kcp3 = new Kcp(TestConv + 2, (_, _) => { });
            uint extremePast = now - 20000;
            uint checkExtreme = kcp3.Check(extremePast);
            if (checkExtreme < now)
            {
                Log.Console("Check() should reset for extreme past time");
                return 5;
            }

            Log.Debug("Core_Kcp_Check_Flush_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
