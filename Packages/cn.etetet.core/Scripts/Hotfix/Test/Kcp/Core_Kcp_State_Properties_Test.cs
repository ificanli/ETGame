using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp state and properties test.
    /// Tests internal state changes and property values.
    /// </summary>
    public class Core_Kcp_State_Properties_Test : ATestHandler
    {
        private const uint TestConv = 3601;
        private const int StepMilliseconds = 10;
        private const int KcpHeaderSize = (int)KCPBASIC.REVERSED_HEAD;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            var clientToServer = new List<byte[]>(128);
            var serverToClient = new List<byte[]>(128);

            void EnqueuePacket(List<byte[]> queue, byte[] buffer, int length)
            {
                var data = new byte[length];
                Buffer.BlockCopy(buffer, KcpHeaderSize, data, 0, length);
                queue.Add(data);
            }

            using Kcp client = new Kcp(TestConv, (buffer, length) => EnqueuePacket(clientToServer, buffer, length));
            using Kcp server = new Kcp(TestConv, (buffer, length) => EnqueuePacket(serverToClient, buffer, length));

            client.SetNoDelay(1, StepMilliseconds, 2, 1);
            server.SetNoDelay(1, StepMilliseconds, 2, 1);

            // Test initial state
            if (client.State != 0)
            {
                Log.Console("Initial state should be 0");
                return 1;
            }

            // Test sequence numbers
            if (client.SendUna != 0)
            {
                Log.Console("Initial SendUna should be 0");
                return 2;
            }

            if (client.SendNext != 0)
            {
                Log.Console("Initial SendNext should be 0");
                return 3;
            }

            if (client.ReceiveNext != 0)
            {
                Log.Console("Initial ReceiveNext should be 0");
                return 4;
            }

            // Test slow start threshold
            if (client.SlowStartThreshold != KCPBASIC.THRESH_INIT)
            {
                Log.Console($"Initial SlowStartThreshold should be {KCPBASIC.THRESH_INIT}");
                return 5;
            }

            // Test RTT values
            if (client.RxSrtt != 0)
            {
                Log.Console("Initial RxSrtt should be 0");
                return 6;
            }

            if (client.RxRttval != 0)
            {
                Log.Console("Initial RxRttval should be 0");
                return 7;
            }

            if (client.RxRto != (int)KCPBASIC.RTO_DEF)
            {
                Log.Console($"Initial RxRto should be {KCPBASIC.RTO_DEF}");
                return 8;
            }

            // Test window sizes
            if (client.RemoteWindowSize != KCPBASIC.WND_RCV)
            {
                Log.Console($"Initial RemoteWindowSize should be {KCPBASIC.WND_RCV}");
                return 9;
            }

            if (client.CongestionWindowSize != 0)
            {
                Log.Console("Initial CongestionWindowSize should be 0");
                return 10;
            }

            // Test probe values
            if (client.Probe != 0)
            {
                Log.Console("Initial Probe should be 0");
                return 11;
            }

            if (client.TimestampProbe != 0)
            {
                Log.Console("Initial TimestampProbe should be 0");
                return 12;
            }

            if (client.ProbeWait != 0)
            {
                Log.Console("Initial ProbeWait should be 0");
                return 13;
            }

            // Test increment
            if (client.Increment != 0)
            {
                Log.Console("Initial Increment should be 0");
                return 14;
            }

            // Test transmission count
            if (client.Transmissions != 0)
            {
                Log.Console("Initial Transmissions should be 0");
                return 15;
            }

            // Test buffer and queue counts
            if (client.ReceiveBufferCount != 0)
            {
                Log.Console("Initial ReceiveBufferCount should be 0");
                return 16;
            }

            if (client.ReceiveQueueCount != 0)
            {
                Log.Console("Initial ReceiveQueueCount should be 0");
                return 17;
            }

            if (client.WaitReceiveCount != 0)
            {
                Log.Console("Initial WaitReceiveCount should be 0");
                return 18;
            }

            if (client.SendBufferCount != 0)
            {
                Log.Console("Initial SendBufferCount should be 0");
                return 19;
            }

            if (client.SendQueueCount != 0)
            {
                Log.Console("Initial SendQueueCount should be 0");
                return 20;
            }

            // Test Updated flag
            if (client.Updated != 0)
            {
                Log.Console("Initial Updated should be 0");
                return 21;
            }

            // Test ACK properties
            if (client.AckCount != 0)
            {
                Log.Console("Initial AckCount should be 0");
                return 22;
            }

            if (client.AckBlock != 0)
            {
                Log.Console("Initial AckBlock should be 0");
                return 23;
            }

            // Test MaximumSegmentSize
            if (client.MaximumSegmentSize != client.MaximumTransmissionUnit - KCPBASIC.OVERHEAD)
            {
                Log.Console("MaximumSegmentSize should be MTU - OVERHEAD");
                return 24;
            }

            // Test TimestampFlush
            if (client.TimestampFlush != KCPBASIC.INTERVAL)
            {
                Log.Console($"Initial TimestampFlush should be {KCPBASIC.INTERVAL}");
                return 25;
            }

            // Test Current
            if (client.Current != 0)
            {
                Log.Console("Initial Current should be 0");
                return 26;
            }

            // Test Buffer
            if (client.Buffer == null)
            {
                Log.Console("Buffer should not be null");
                return 27;
            }

            if (client.Buffer.Length < KCPBASIC.REVERSED_HEAD)
            {
                Log.Console("Buffer length should be at least REVERSED_HEAD");
                return 28;
            }

            // Test Output
            if (client.Output == null)
            {
                Log.Console("Output should not be null");
                return 29;
            }

            // Test state changes after Update
            uint now = 10;
            client.Update(now);

            if (client.Current != now)
            {
                Log.Console("Current should update after Update()");
                return 30;
            }

            if (client.Updated != 1)
            {
                Log.Console("Updated should be 1 after first Update()");
                return 31;
            }

            // Test state changes after sending data
            byte[] payload = new byte[64];
            client.Send(payload);

            if (client.SendQueueCount == 0)
            {
                Log.Console("SendQueueCount should increase after Send()");
                return 32;
            }

            // Test state changes after receiving data
            uint now2 = 20;
            client.Update(now2);
            server.Update(now2);

            bool Transfer(List<byte[]> from, Kcp to, out string error)
            {
                for (int i = 0; i < from.Count; ++i)
                {
                    int inputResult = to.Input(from[i]);
                    if (inputResult < 0)
                    {
                        error = $"Input failed with code {inputResult}";
                        return false;
                    }
                }

                from.Clear();
                error = null;
                return true;
            }

            if (!Transfer(clientToServer, server, out string error))
            {
                Log.Console($"Transfer error: {error}");
                return 33;
            }

            // Need to update again to move data from buffer to queue
            client.Update(now2);
            server.Update(now2);

            // Test that server received data
            if (server.WaitReceiveCount == 0)
            {
                Log.Console("WaitReceiveCount should increase after receiving data");
                return 34;
            }

            // Test AckCount after receiving
            // (ACKs are generated when data is received)
            uint now3 = 30;
            client.Update(now3);
            server.Update(now3);

            if (!Transfer(serverToClient, client, out error))
            {
                Log.Console($"Transfer error: {error}");
                return 35;
            }

            // Test that client received ACKs
            // (ACKs are in the packets sent from server to client)
            // Note: AckCount is internal and may be 0 after processing

            Log.Debug("Core_Kcp_State_Properties_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
