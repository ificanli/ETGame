using System;

namespace ET.Test
{
    /// <summary>
    /// Kcp configuration boundary test.
    /// </summary>
    public class Core_Kcp_Config_Test : ATestHandler
    {
        private const uint TestConv = 3101;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            using Kcp kcp = new Kcp(TestConv, (_, _) => { });

            uint defaultMtu = kcp.MaximumTransmissionUnit;
            int invalidMtu = (int)KCPBASIC.OVERHEAD - 1;
            if (kcp.SetMtu(invalidMtu) == 0)
            {
                Log.Console("SetMtu should fail for mtu below overhead");
                return 1;
            }

            if (kcp.MaximumTransmissionUnit != defaultMtu)
            {
                Log.Console("MaximumTransmissionUnit should not change after invalid SetMtu");
                return 2;
            }

            kcp.SetInterval(0);
            if (kcp.Interval != KCPBASIC.INTERVAL_MIN)
            {
                Log.Console("Interval should clamp to INTERVAL_MIN");
                return 3;
            }

            kcp.SetInterval((int)KCPBASIC.INTERVAL_LIMIT + 100);
            if (kcp.Interval != KCPBASIC.INTERVAL_LIMIT)
            {
                Log.Console("Interval should clamp to INTERVAL_LIMIT");
                return 4;
            }

            kcp.SetFastResendLimit(-1);
            if (kcp.FastResendLimit != KCPBASIC.FASTACK_MIN)
            {
                Log.Console("FastResendLimit should clamp to FASTACK_MIN");
                return 5;
            }

            kcp.SetFastResendLimit((int)KCPBASIC.FASTACK_LIMIT + 10);
            if (kcp.FastResendLimit != KCPBASIC.FASTACK_LIMIT)
            {
                Log.Console("FastResendLimit should clamp to FASTACK_LIMIT");
                return 6;
            }

            kcp.SetMinrto(0);
            if (kcp.RxMinrto != (int)KCPBASIC.INTERVAL_MIN)
            {
                Log.Console("Minrto should clamp to INTERVAL_MIN");
                return 7;
            }

            kcp.SetMinrto((int)KCPBASIC.RTO_MAX + 1000);
            if (kcp.RxMinrto != (int)KCPBASIC.RTO_MAX)
            {
                Log.Console("Minrto should clamp to RTO_MAX");
                return 8;
            }

            kcp.SetNoDelay(2, 0, -5, 2);
            if (kcp.NoDelay != KCPBASIC.NODELAY_LIMIT)
            {
                Log.Console("NoDelay should clamp to NODELAY_LIMIT");
                return 9;
            }

            if (kcp.Interval != KCPBASIC.INTERVAL_MIN)
            {
                Log.Console("Interval should clamp to INTERVAL_MIN via SetNoDelay");
                return 10;
            }

            if (kcp.FastResend != 0)
            {
                Log.Console("FastResend should clamp to 0");
                return 11;
            }

            if (kcp.NoCongestionWindow != 0)
            {
                Log.Console("NoCongestionWindow should be 0 when nc != 1");
                return 12;
            }

            kcp.SetWindowSize(0, 0);
            if (kcp.SendWindowSize != KCPBASIC.WND_SND || kcp.ReceiveWindowSize != KCPBASIC.WND_RCV)
            {
                Log.Console("Window size should clamp to WND_SND/WND_RCV");
                return 13;
            }

            kcp.SetStreamMode(1);
            if (kcp.StreamMode != 1)
            {
                Log.Console("StreamMode should be 1 after SetStreamMode(1)");
                return 14;
            }

            kcp.SetStreamMode(0);
            if (kcp.StreamMode != 0)
            {
                Log.Console("StreamMode should be 0 after SetStreamMode(0)");
                return 15;
            }

            Log.Debug("Core_Kcp_Config_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
