using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp extreme network conditions test.
    /// Tests behavior under high latency, high packet loss, and jitter.
    /// </summary>
    public class Core_Kcp_Extreme_Network_Test : ATestHandler
    {
        private const uint TestConv = 4201;
        private const int StepMilliseconds = 10;
        private const int PayloadSize = 128;
        private const int KcpHeaderSize = (int)KCPBASIC.REVERSED_HEAD;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            // Test 1: High latency scenario
            if (!TestHighLatency())
            {
                return 1;
            }

            // Test 2: High packet loss scenario
            if (!TestHighPacketLoss())
            {
                return 2;
            }

            // Test 3: High jitter scenario
            if (!TestHighJitter())
            {
                return 3;
            }

            // Test 4: Combined extreme scenario
            if (!TestCombinedExtreme())
            {
                return 4;
            }

            // Test 5: Connection timeout scenario
            if (!TestConnectionTimeout())
            {
                return 5;
            }

            Log.Debug("Core_Kcp_Extreme_Network_Test all passed");
            return ErrorCode.ERR_Success;
        }

        private bool TestHighLatency()
        {
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

            // Configure for high latency
            client.SetNoDelay(0, 100, 2, 1); // Higher interval for high latency
            server.SetNoDelay(0, 100, 2, 1);
            client.SetWindowSize(512, 512);
            server.SetWindowSize(512, 512);
            client.SetMinrto(200); // Higher minimum RTO
            server.SetMinrto(200);

            int messageCount = 100;
            for (int i = 0; i < messageCount; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                client.Send(payload);
            }

            uint now = 0;
            int receivedCount = 0;

            // Simulate high latency by delaying packet delivery
            for (int tick = 0; tick < 5000 && receivedCount < messageCount; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                // Introduce delay: only deliver packets every 10 ticks
                if (tick % 10 == 0)
                {
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
                        Log.Console($"High latency transfer error: {error}");
                        return false;
                    }

                    if (!Transfer(serverToClient, client, out error))
                    {
                        Log.Console($"High latency transfer error: {error}");
                        return false;
                    }
                }

                while (TryReceive(server, out byte[] data, out string receiveError))
                {
                    if (receiveError != null)
                    {
                        Log.Console($"High latency receive error: {receiveError}");
                        return false;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"High latency payload validation failed at index {receivedCount}");
                        return false;
                    }

                    receivedCount++;
                }
            }

            if (receivedCount != messageCount)
            {
                Log.Console($"High latency: Not all messages received: {receivedCount}/{messageCount}");
                return false;
            }

            Log.Debug("High latency test passed");
            return true;
        }

        private bool TestHighPacketLoss()
        {
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

            client.SetNoDelay(1, StepMilliseconds, 2, 1);
            server.SetNoDelay(1, StepMilliseconds, 2, 1);
            client.SetWindowSize(256, 256);
            server.SetWindowSize(256, 256);

            int messageCount = 100;
            for (int i = 0; i < messageCount; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                client.Send(payload);
            }

            uint now = 0;
            int receivedCount = 0;
            int packetIndex = 0;

            // Simulate 50% packet loss
            for (int tick = 0; tick < 5000 && receivedCount < messageCount; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                bool TransferWithLoss(List<byte[]> from, Kcp to, out string error)
                {
                    for (int i = 0; i < from.Count; ++i)
                    {
                        packetIndex++;
                        // Drop 50% of packets
                        if (packetIndex % 2 == 0)
                        {
                            continue;
                        }

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

                if (!TransferWithLoss(clientToServer, server, out string error))
                {
                    Log.Console($"High packet loss transfer error: {error}");
                    return false;
                }

                if (!TransferWithLoss(serverToClient, client, out error))
                {
                    Log.Console($"High packet loss transfer error: {error}");
                    return false;
                }

                while (TryReceive(server, out byte[] data, out string receiveError))
                {
                    if (receiveError != null)
                    {
                        Log.Console($"High packet loss receive error: {receiveError}");
                        return false;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"High packet loss payload validation failed at index {receivedCount}");
                        return false;
                    }

                    receivedCount++;
                }
            }

            if (receivedCount != messageCount)
            {
                Log.Console($"High packet loss: Not all messages received: {receivedCount}/{messageCount}");
                return false;
            }

            Log.Debug("High packet loss test passed");
            return true;
        }

        private bool TestHighJitter()
        {
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

            client.SetNoDelay(1, StepMilliseconds, 2, 1);
            server.SetNoDelay(1, StepMilliseconds, 2, 1);
            client.SetWindowSize(256, 256);
            server.SetWindowSize(256, 256);

            int messageCount = 100;
            for (int i = 0; i < messageCount; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                client.Send(payload);
            }

            uint now = 0;
            int receivedCount = 0;
            var random = new Random(42);

            // Simulate high jitter by delivering packets at random intervals
            for (int tick = 0; tick < 5000 && receivedCount < messageCount; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                // Randomly decide whether to deliver packets
                if (random.Next(0, 10) < 3) // 30% chance to deliver
                {
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
                        Log.Console($"High jitter transfer error: {error}");
                        return false;
                    }

                    if (!Transfer(serverToClient, client, out error))
                    {
                        Log.Console($"High jitter transfer error: {error}");
                        return false;
                    }
                }

                while (TryReceive(server, out byte[] data, out string receiveError))
                {
                    if (receiveError != null)
                    {
                        Log.Console($"High jitter receive error: {receiveError}");
                        return false;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"High jitter payload validation failed at index {receivedCount}");
                        return false;
                    }

                    receivedCount++;
                }
            }

            if (receivedCount != messageCount)
            {
                Log.Console($"High jitter: Not all messages received: {receivedCount}/{messageCount}");
                return false;
            }

            Log.Debug("High jitter test passed");
            return true;
        }

        private bool TestCombinedExtreme()
        {
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

            // Configure for extreme conditions
            client.SetNoDelay(1, 50, 2, 1);
            server.SetNoDelay(1, 50, 2, 1);
            client.SetWindowSize(512, 512);
            server.SetWindowSize(512, 512);
            client.SetMinrto(100);
            server.SetMinrto(100);

            int messageCount = 100;
            for (int i = 0; i < messageCount; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                client.Send(payload);
            }

            uint now = 0;
            int receivedCount = 0;
            int packetIndex = 0;
            var random = new Random(42);

            // Simulate combined extreme conditions
            for (int tick = 0; tick < 10000 && receivedCount < messageCount; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                bool TransferExtreme(List<byte[]> from, Kcp to, out string error)
                {
                    for (int i = 0; i < from.Count; ++i)
                    {
                        packetIndex++;

                        // 30% packet loss
                        if (random.Next(0, 10) < 3)
                        {
                            continue;
                        }

                        // 20% chance to delay packet
                        if (random.Next(0, 10) < 2)
                        {
                            continue; // Delay by not delivering this tick
                        }

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

                if (!TransferExtreme(clientToServer, server, out string error))
                {
                    Log.Console($"Combined extreme transfer error: {error}");
                    return false;
                }

                if (!TransferExtreme(serverToClient, client, out error))
                {
                    Log.Console($"Combined extreme transfer error: {error}");
                    return false;
                }

                while (TryReceive(server, out byte[] data, out string receiveError))
                {
                    if (receiveError != null)
                    {
                        Log.Console($"Combined extreme receive error: {receiveError}");
                        return false;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"Combined extreme payload validation failed at index {receivedCount}");
                        return false;
                    }

                    receivedCount++;
                }
            }

            if (receivedCount != messageCount)
            {
                Log.Console($"Combined extreme: Not all messages received: {receivedCount}/{messageCount}");
                return false;
            }

            Log.Debug("Combined extreme test passed");
            return true;
        }

        private bool TestConnectionTimeout()
        {
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

            client.SetNoDelay(1, StepMilliseconds, 2, 1);
            server.SetNoDelay(1, StepMilliseconds, 2, 1);

            // Send data
            for (int i = 0; i < 10; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                client.Send(payload);
            }

            uint now = 0;
            client.Update(now);

            // Transfer initial packets
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
                Log.Console($"Connection timeout transfer error: {error}");
                return false;
            }

            // Simulate connection timeout by stopping all ACKs
            // Keep sending data but never deliver ACKs
            for (int i = 10; i < 20; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                client.Send(payload);
            }

            // Update for a long time without ACKs
            for (int tick = 0; tick < 10000; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);

                // Check if connection state changes
                if (client.State != 0)
                {
                    // Connection may be marked as dead
                    Log.Debug($"Connection state changed to {client.State} after timeout");
                    break;
                }
            }

            // Transmissions should have increased due to retransmissions
            if (client.Transmissions == 0)
            {
                Log.Console("Transmissions should increase during timeout");
                return false;
            }

            Log.Debug("Connection timeout test passed");
            return true;
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
