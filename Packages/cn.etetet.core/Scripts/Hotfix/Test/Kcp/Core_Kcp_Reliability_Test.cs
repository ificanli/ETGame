using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp reliability test with deterministic loss/dup/reorder.
    /// </summary>
    public class Core_Kcp_Reliability_Test : ATestHandler
    {
        private const uint TestConv = 3001;
        private const int StepMilliseconds = 10;
        private const int MaxTicks = 3000;
        private const int MessageCount = 500;
        private const int PayloadSize = 128;
        private const int DropInterval = 7;
        private const int DuplicateInterval = 11;
        private const int ReorderBatchSize = 5;
        private const int KcpHeaderSize = (int)KCPBASIC.REVERSED_HEAD;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            var clientToServer = new List<byte[]>(256);
            var serverToClient = new List<byte[]>(256);
            var clientToServerNet = new List<byte[]>(256);
            var serverToClientNet = new List<byte[]>(256);

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

            for (int i = 0; i < MessageCount; ++i)
            {
                byte[] payload = CreatePayload(PayloadSize, i);
                int sent = client.Send(payload);
                if (sent != payload.Length)
                {
                    Log.Console($"Send should return full length, index: {i}, sent: {sent}");
                    return 1;
                }
            }

            uint now = 0;
            int receivedCount = 0;
            int c2sIndex = 0;
            int s2cIndex = 0;

            for (int tick = 0; tick < MaxTicks && receivedCount < MessageCount; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                ApplyNetwork(clientToServer, clientToServerNet, ref c2sIndex, DropInterval, DuplicateInterval, ReorderBatchSize);
                ApplyNetwork(serverToClient, serverToClientNet, ref s2cIndex, DropInterval, DuplicateInterval, ReorderBatchSize);

                if (!Deliver(clientToServerNet, server, out string error))
                {
                    Log.Console($"Deliver error (c2s): {error}");
                    return 2;
                }

                if (!Deliver(serverToClientNet, client, out error))
                {
                    Log.Console($"Deliver error (s2c): {error}");
                    return 3;
                }

                while (TryReceive(server, out byte[] data, out error))
                {
                    if (error != null)
                    {
                        Log.Console($"Receive error: {error}");
                        return 4;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"Payload validation failed, index: {receivedCount}");
                        return 5;
                    }

                    receivedCount++;
                    if (receivedCount >= MessageCount)
                        break;
                }
            }

            if (receivedCount != MessageCount)
            {
                Log.Console($"Not all messages received: {receivedCount}/{MessageCount}");
                return 6;
            }

            Log.Debug("Core_Kcp_Reliability_Test all passed");
            return ErrorCode.ERR_Success;
        }

        private static void ApplyNetwork(
            List<byte[]> source,
            List<byte[]> target,
            ref int packetIndex,
            int dropInterval,
            int duplicateInterval,
            int reorderBatchSize)
        {
            if (source.Count == 0)
                return;

            var temp = new List<byte[]>(source.Count * 2);
            for (int i = 0; i < source.Count; ++i)
            {
                packetIndex++;
                if (dropInterval > 0 && packetIndex % dropInterval == 0)
                {
                    continue;
                }

                temp.Add(source[i]);
                if (duplicateInterval > 0 && packetIndex % duplicateInterval == 0)
                {
                    temp.Add(source[i]);
                }
            }

            source.Clear();

            if (reorderBatchSize > 1 && temp.Count >= reorderBatchSize)
            {
                for (int i = 0; i < temp.Count; i += reorderBatchSize)
                {
                    int end = Math.Min(i + reorderBatchSize, temp.Count);
                    for (int left = i, right = end - 1; left < right; ++left, --right)
                    {
                        (temp[left], temp[right]) = (temp[right], temp[left]);
                    }
                }
            }

            target.AddRange(temp);
        }

        private static bool Deliver(List<byte[]> packets, Kcp receiver, out string error)
        {
            for (int i = 0; i < packets.Count; ++i)
            {
                int inputResult = receiver.Input(packets[i]);
                if (inputResult < 0)
                {
                    error = $"Input failed with code {inputResult}";
                    return false;
                }
            }

            packets.Clear();
            error = null;
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
