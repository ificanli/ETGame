using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp buffer management test.
    /// Tests send/receive buffer and queue behavior.
    /// </summary>
    public class Core_Kcp_Buffer_Management_Test : ATestHandler
    {
        private const uint TestConv = 4001;
        private const int StepMilliseconds = 10;
        private const int PayloadSize = 64;
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
            client.SetWindowSize(128, 128);
            server.SetWindowSize(128, 128);

            // Basic send/receive roundtrip
            int sendCount = 50;
            for (int i = 0; i < sendCount; ++i)
            {
                byte[] payload = new byte[PayloadSize];
                payload[0] = (byte)i;
                int sent = client.Send(payload);
                if (sent != payload.Length)
                {
                    Log.Console($"Send failed at index {i}");
                    return 1;
                }
            }

            uint now = 0;
            client.Update(now);

            for (int i = 0; i < clientToServer.Count; ++i)
            {
                int inputResult = server.Input(clientToServer[i]);
                if (inputResult < 0)
                {
                    Log.Console($"Input failed with code {inputResult}");
                    return 2;
                }
            }
            clientToServer.Clear();
            server.Update(now);

            int receivedCount = 0;
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
                    return 3;
                }

                receivedCount++;
                if (receivedCount >= sendCount)
                    break;
            }

            if (receivedCount != sendCount)
            {
                Log.Console($"Receive count mismatch: {receivedCount}/{sendCount}");
                return 4;
            }

            // Window size API
            client.SetWindowSize(64, 64);

            // Stream mode API: total bytes should match even if merged
            client.SetStreamMode(1);
            byte[] streamData1 = new byte[100];
            byte[] streamData2 = new byte[100];
            byte[] streamData3 = new byte[100];
            client.Send(streamData1);
            client.Send(streamData2);
            client.Send(streamData3);

            now += StepMilliseconds;
            client.Update(now);
            for (int i = 0; i < clientToServer.Count; ++i)
            {
                int inputResult = server.Input(clientToServer[i]);
                if (inputResult < 0)
                {
                    Log.Console($"Input failed with code {inputResult}");
                    return 6;
                }
            }
            clientToServer.Clear();
            server.Update(now);

            int totalReceived = 0;
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
                    return 7;
                }
                totalReceived += received;
            }

            if (totalReceived != 300)
            {
                Log.Console($"Stream mode total size mismatch: {totalReceived}/300");
                return 8;
            }

            // NoDelay API
            client.SetNoDelay(1, 0, 2, 1);
            if (client.NoDelay != 1)
            {
                Log.Console("SetNoDelay should update NoDelay");
                return 9;
            }

            Log.Debug("Core_Kcp_Buffer_Management_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
