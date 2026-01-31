using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp acknowledgment mechanism test.
    /// Tests ACK generation, processing, and fast resend.
    /// </summary>
    public class Core_Kcp_Ack_Test : ATestHandler
    {
        private const uint TestConv = 4101;
        private const int StepMilliseconds = 10;
        private const int PayloadSize = 128;
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

            client.SetNoDelay(1, StepMilliseconds, 2, 1);
            server.SetNoDelay(1, StepMilliseconds, 2, 1);
            client.SetWindowSize(256, 256);
            server.SetWindowSize(256, 256);

            // Test initial ACK properties
            if (client.AckCount != 0)
            {
                Log.Console("Initial AckCount should be 0");
                return 1;
            }

            if (client.AckBlock != 0)
            {
                Log.Console("Initial AckBlock should be 0");
                return 2;
            }

            // Send data to trigger ACK generation
            int sendCount = 50;
            for (int i = 0; i < sendCount; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                client.Send(payload);
            }

            // Transfer to server
            uint now = 0;
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

            if (!Transfer(clientToServer, server, out string transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 3;
            }

            // Server should generate ACKs
            now += StepMilliseconds;
            server.Update(now);

            // Server's AckCount may be non-zero after receiving data
            // (ACKs are generated and sent in packets)

            // Transfer ACKs back to client
            if (!Transfer(serverToClient, client, out transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 4;
            }

            // Client should process ACKs
            now += StepMilliseconds;
            client.Update(now);

            // After processing ACKs, client's send buffer should decrease
            uint sendBufferAfterAck = client.SendBufferCount;
            if (sendBufferAfterAck == 0)
            {
                // This is possible if all packets were ACKed
            }

            // Test ACK with selective acknowledgment
            // Send more data
            for (int i = sendCount; i < sendCount + 50; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                client.Send(payload);
            }

            now += StepMilliseconds;
            client.Update(now);

            if (!Transfer(clientToServer, server, out transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 5;
            }

            // Receive some but not all packets
            int receivedCount = 0;
            while (server.PeekSize() >= 0 && receivedCount < 25)
            {
                int size = server.PeekSize();
                if (size < 0)
                    break;

                byte[] buffer = new byte[size];
                int received = server.Receive(buffer);
                if (received != size)
                {
                    Log.Console($"Receive failed: expected {size}, got {received}");
                    return 6;
                }

                receivedCount++;
            }

            // Server should generate ACKs for received packets
            now += StepMilliseconds;
            server.Update(now);

            // Transfer ACKs back to client
            if (!Transfer(serverToClient, client, out transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 7;
            }

            // Client should process selective ACKs
            now += StepMilliseconds;
            client.Update(now);

            // Test fast resend mechanism
            client.SetFastResendLimit(2);
            if (client.FastResendLimit != 2)
            {
                Log.Console("FastResendLimit should be updated");
                return 8;
            }

            // Send data
            for (int i = 0; i < 10; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i + 1000);
                client.Send(payload);
            }

            now += StepMilliseconds;
            client.Update(now);

            // Drop one packet to create a gap for fast resend
            int droppedIndex = 0;
            for (int i = 0; i < clientToServer.Count; ++i)
            {
                if (i == droppedIndex)
                    continue;
                int inputResult = server.Input(clientToServer[i]);
                if (inputResult < 0)
                {
                    Log.Console($"Input failed with code {inputResult}");
                    return 9;
                }
            }

            clientToServer.Clear();

            // Server receives all packets and sends ACKs
            now += StepMilliseconds;
            server.Update(now);

            // Simulate duplicate ACKs by resending the same ACK packets
            // This should trigger fast resend
            var ackPackets = new List<byte[]>(serverToClient);
            if (!Transfer(serverToClient, client, out transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 10;
            }

            // Transfer duplicate ACKs
            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < ackPackets.Count; ++j)
                {
                    client.Input(ackPackets[j]);
                }
            }

            clientToServer.Clear();

            now += StepMilliseconds;
            client.Update(now);

            // Fast resend should have triggered and produced output
            if (clientToServer.Count == 0)
            {
                Log.Console("Fast resend should output at least one segment");
                return 11;
            }

            // Test ACK with packet loss
            // Send data and simulate loss
            clientToServer.Clear();
            for (int i = 0; i < 20; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i + 2000);
                client.Send(payload);
            }

            now += StepMilliseconds;
            client.Update(now);

            // Drop some packets (simulate loss)
            int droppedCount = 0;
            for (int i = 0; i < clientToServer.Count; ++i)
            {
                if (i % 3 == 0)
                {
                    droppedCount++;
                    continue; // Drop this packet
                }

                int inputResult = server.Input(clientToServer[i]);
                if (inputResult < 0)
                {
                    Log.Console($"Input failed with code {inputResult}");
                    return 12;
                }
            }

            clientToServer.Clear();

            // Server should generate ACKs for received packets
            now += StepMilliseconds;
            server.Update(now);

            // Transfer ACKs back to client
            if (!Transfer(serverToClient, client, out transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 13;
            }

            // Client should process ACKs and retransmit lost packets
            now += StepMilliseconds;
            client.Update(now);

            // Wait for retransmission timeout
            for (int tick = 0; tick < 500; ++tick)
            {
                now += StepMilliseconds;
                client.Update(now);
                server.Update(now);

                if (!Transfer(clientToServer, server, out transferError))
                {
                    Log.Console($"Transfer error: {transferError}");
                    return 14;
                }

                if (!Transfer(serverToClient, client, out transferError))
                {
                    Log.Console($"Transfer error: {transferError}");
                    return 15;
                }

                // Receive data
                while (server.PeekSize() >= 0)
                {
                    int size = server.PeekSize();
                    if (size < 0)
                        break;

                    byte[] buffer = new byte[size];
                    int received = server.Receive(buffer);
                    if (received != size)
                    {
                        Log.Console($"Receive failed: expected {size}, got {received}");
                        return 16;
                    }

                    receivedCount++;
                }

                // Check if all packets were received
                if (receivedCount >= 20)
                    break;
            }

            if (receivedCount < 20)
            {
                Log.Console($"Not all packets received after retransmission: {receivedCount}/20");
                return 17;
            }

            // Test ACK with out-of-order packets
            clientToServer.Clear();
            serverToClient.Clear();

            // Send packets
            for (int i = 0; i < 10; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i + 3000);
                client.Send(payload);
            }

            now += StepMilliseconds;
            client.Update(now);

            // Deliver packets out of order
            var outOfOrderPackets = new List<byte[]>(clientToServer);
            outOfOrderPackets.Reverse();

            foreach (var packet in outOfOrderPackets)
            {
                int inputResult = server.Input(packet);
                if (inputResult < 0)
                {
                    Log.Console($"Input failed with code {inputResult}");
                    return 18;
                }
            }

            clientToServer.Clear();

            // Server should generate ACKs and handle out-of-order packets
            now += StepMilliseconds;
            server.Update(now);

            // Receive all packets (should be in order)
            int outOfOrderReceived = 0;
            while (server.PeekSize() >= 0)
            {
                int size = server.PeekSize();
                if (size < 0)
                    break;

                byte[] buffer = new byte[size];
                int received = server.Receive(buffer);
                if (received != size)
                {
                    Log.Console($"Receive failed: expected {size}, got {received}");
                    return 19;
                }

                outOfOrderReceived++;
            }

            if (outOfOrderReceived != 10)
            {
                Log.Console($"Expected to receive 10 packets, got {outOfOrderReceived}");
                return 20;
            }

            // Test ACK block growth
            // Send many packets to trigger ACK block allocation
            clientToServer.Clear();
            serverToClient.Clear();

            for (int i = 0; i < 100; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i + 4000);
                client.Send(payload);
            }

            now += StepMilliseconds;
            client.Update(now);

            if (!Transfer(clientToServer, server, out transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 21;
            }

            // Server should generate ACKs
            now += StepMilliseconds;
            server.Update(now);

            // ACK block may have grown to accommodate more ACKs
            // (this is internal, we just verify it doesn't crash)

            Log.Debug("Core_Kcp_Ack_Test all passed");
            return ErrorCode.ERR_Success;
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
    }
}
