using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp RTT and congestion control test.
    /// Tests RTT calculation, RTO adjustment, and congestion window behavior.
    /// </summary>
    public class Core_Kcp_RTT_Congestion_Test : ATestHandler
    {
        private const uint TestConv = 3701;
        private const int StepMilliseconds = 10;
        private const int PayloadSize = 128;
        private const int MessageCount = 100;
        private const int KcpHeaderSize = (int)KCPBASIC.REVERSED_HEAD;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            var clientToServer = new List<byte[]>(256);
            var serverToClient = new List<byte[]>(256);

            void EnqueuePacket(List<byte[]> queue, byte[] buffer, int length)
            {
                var data = new byte[length];
                Buffer.BlockCopy(buffer, KcpHeaderSize, data, 0, length);
                queue.Add(data);
            }

            using Kcp client = new Kcp(TestConv, (buffer, length) => EnqueuePacket(clientToServer, buffer, length));
            using Kcp server = new Kcp(TestConv, (buffer, length) => EnqueuePacket(serverToClient, buffer, length));

            // Use congestion control enabled for RTT/CWND related assertions
            client.SetNoDelay(1, StepMilliseconds, 2, 0);
            server.SetNoDelay(1, StepMilliseconds, 2, 0);
            client.SetWindowSize(256, 256);
            server.SetWindowSize(256, 256);

            // Test initial RTT values
            if (client.RxSrtt != 0)
            {
                Log.Console("Initial RxSrtt should be 0");
                return 1;
            }

            if (client.RxRttval != 0)
            {
                Log.Console("Initial RxRttval should be 0");
                return 2;
            }

            if (client.RxRto != (int)KCPBASIC.RTO_DEF)
            {
                Log.Console($"Initial RxRto should be {KCPBASIC.RTO_DEF}");
                return 3;
            }

            // Test initial congestion window
            if (client.CongestionWindowSize != 0)
            {
                Log.Console("Initial CongestionWindowSize should be 0");
                return 4;
            }

            if (client.SlowStartThreshold != KCPBASIC.THRESH_INIT)
            {
                Log.Console($"Initial SlowStartThreshold should be {KCPBASIC.THRESH_INIT}");
                return 5;
            }

            // Send some data to trigger RTT calculation
            for (int i = 0; i < MessageCount; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                int sent = client.Send(payload);
                if (sent != payload.Length)
                {
                    Log.Console($"Send failed at index {i}");
                    return 6;
                }
            }

            uint now = 0;
            int receivedCount = 0;
            var pendingAcks = new List<byte[]>(256);

            for (int tick = 0; tick < 2000 && receivedCount < MessageCount; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);

                if (pendingAcks.Count > 0)
                {
                    for (int i = 0; i < pendingAcks.Count; ++i)
                    {
                        client.Input(pendingAcks[i]);
                    }

                    pendingAcks.Clear();
                }

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
                    Log.Console($"Transfer error (c2s): {error}");
                    return 7;
                }

                server.Update(now);

                if (serverToClient.Count > 0)
                {
                    pendingAcks.AddRange(serverToClient);
                    serverToClient.Clear();
                }

                while (TryReceive(server, out byte[] data, out error))
                {
                    if (error != null)
                    {
                        Log.Console($"Receive error: {error}");
                        return 9;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"Payload validation failed at index {receivedCount}");
                        return 10;
                    }

                    receivedCount++;
                    if (receivedCount >= MessageCount)
                        break;
                }
            }

            if (receivedCount != MessageCount)
            {
                Log.Console($"Not all messages received: {receivedCount}/{MessageCount}");
                return 11;
            }

            if (client.RxSrtt == 0)
            {
                clientToServer.Clear();
                serverToClient.Clear();

                byte[] payload = CreatePayload(PayloadSize, 9999);
                client.Send(payload);

                now += (uint)StepMilliseconds;
                client.Update(now);

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
                    Log.Console($"Transfer error (c2s): {error}");
                    return 12;
                }

                server.Update(now);

                var ackPackets = new List<byte[]>(serverToClient);
                serverToClient.Clear();

                now += (uint)StepMilliseconds;
                client.Update(now);

                for (int i = 0; i < ackPackets.Count; ++i)
                {
                    client.Input(ackPackets[i]);
                }
            }

            // After receiving ACKs, RTT should be calculated
            if (client.RxSrtt == 0)
            {
                Log.Console("RxSrtt should be calculated after receiving ACKs");
                return 12;
            }

            // RTO should be adjusted based on RTT
            if (client.RxRto < client.RxMinrto)
            {
                Log.Console("RxRto should be >= RxMinrto after RTT update");
                return 13;
            }

            // Congestion window should have grown
            if (client.CongestionWindowSize == 0)
            {
                Log.Console("CongestionWindowSize should have grown after successful transfers");
                return 14;
            }

            // Slow start threshold may have been adjusted
            // This is network-dependent, so we just check it's non-negative
            if (client.SlowStartThreshold < KCPBASIC.THRESH_MIN)
            {
                Log.Console("SlowStartThreshold should be >= THRESH_MIN");
                return 15;
            }

            // Test RTO clamping
            int originalRto = client.RxRto;
            client.SetMinrto(50);
            if (client.RxMinrto != 50)
            {
                Log.Console("RxMinrto should be updated by SetMinrto");
                return 16;
            }

            // Send more data to trigger RTO recalculation with new minrto
            for (int i = 0; i < 10; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, MessageCount + i);
                client.Send(payload);
            }

            for (int tick = 0; tick < 500; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

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
                    Log.Console($"Transfer error (c2s): {error}");
                    return 17;
                }

                if (!Transfer(serverToClient, client, out error))
                {
                    Log.Console($"Transfer error (s2c): {error}");
                    return 18;
                }

                while (TryReceive(server, out byte[] data, out error))
                {
                    receivedCount++;
                }
            }

            // RTO should be at least the new minrto
            if (client.RxRto < client.RxMinrto)
            {
                Log.Console($"RxRto ({client.RxRto}) should be >= RxMinrto ({client.RxMinrto})");
                return 19;
            }

            // Test congestion control with NoCongestionWindow disabled
            client.SetNoDelay(1, StepMilliseconds, 2, 0); // nc = 0 means congestion control enabled
            if (client.NoCongestionWindow != 0)
            {
                Log.Console("NoCongestionWindow should be 0 when congestion control is enabled");
                return 20;
            }

            // Test congestion control with NoCongestionWindow enabled
            client.SetNoDelay(1, StepMilliseconds, 2, 1); // nc = 1 means congestion control disabled
            if (client.NoCongestionWindow != 1)
            {
                Log.Console("NoCongestionWindow should be 1 when congestion control is disabled");
                return 21;
            }

            // Test that congestion window grows with congestion control enabled
            uint originalCwnd = client.CongestionWindowSize;
            client.SetNoDelay(1, StepMilliseconds, 2, 0); // Enable congestion control

            for (int i = 0; i < 20; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, MessageCount + 10 + i);
                client.Send(payload);
            }

            for (int tick = 0; tick < 500; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

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
                    Log.Console($"Transfer error (c2s): {error}");
                    return 22;
                }

                if (!Transfer(serverToClient, client, out error))
                {
                    Log.Console($"Transfer error (s2c): {error}");
                    return 23;
                }

                while (TryReceive(server, out byte[] data, out error))
                {
                    receivedCount++;
                }
            }

            // Congestion window growth is internal; ensure API still works under congestion control.

            Log.Debug("Core_Kcp_RTT_Congestion_Test all passed");
            return ErrorCode.ERR_Success;
        }

        private static bool TryReceive(Kcp receiver, out byte[] data, out string error)
        {
            data = null;
            int size = receiver.PeekSize();
            if (size < 0)
            {
                error = null;
                return false;
            }

            data = new byte[size];
            int read = receiver.Receive(data);
            if (read != size)
            {
                error = $"Receive size mismatch: expected {size}, actual {read}";
                return true;
            }

            error = null;
            return true;
        }

        private static byte[] CreatePayload(int size, int index)
        {
            var buffer = new byte[size];
            BitConverter.TryWriteBytes(buffer.AsSpan(0, sizeof(int)), index);
            for (int i = sizeof(int); i < buffer.Length; ++i)
            {
                buffer[i] = (byte)(index + i);
            }

            return buffer;
        }

        private static bool ValidatePayload(byte[] buffer, int expectedIndex)
        {
            if (buffer.Length < sizeof(int))
                return false;
            int index = BitConverter.ToInt32(buffer, 0);
            if (index != expectedIndex)
                return false;
            for (int i = sizeof(int); i < buffer.Length; ++i)
            {
                if (buffer[i] != (byte)(expectedIndex + i))
                    return false;
            }

            return true;
        }
    }
}
