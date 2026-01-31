using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ET.Test
{
    /// <summary>
    /// Kcp performance test.
    /// Measures throughput while validating order and payload.
    /// </summary>
    public class Core_Kcp_Performance_Test : ATestHandler
    {
        private const uint TestConv = 2001;
        private const int StepMilliseconds = 10;
        private const int PayloadSize = 256;
        private const int MeasureMilliseconds = 5000;
        private const int MessagesPerTick = 200;
        private const int DrainMaxTicks = 3000;
        private const int KcpHeaderSize = (int)KCPBASIC.REVERSED_HEAD;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            var clientToServer = new List<byte[]>(256);
            var serverToClient = new List<byte[]>(16);

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

            uint now = 0;
            int sentCount = 0;
            int receivedCount = 0;
            int receivedInWindow = 0;
            int windowTicks = MeasureMilliseconds / StepMilliseconds;
            if (windowTicks <= 0)
            {
                Log.Console("MeasureMilliseconds must be greater than StepMilliseconds");
                return 1;
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

            bool TryReceive(Kcp receiver, out byte[] data, out string error)
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

            for (int tick = 0; tick < windowTicks; ++tick)
            {
                for (int i = 0; i < MessagesPerTick; ++i)
                {
                    byte[] payload = CreatePayload(PayloadSize, sentCount);
                    int sent = client.Send(payload);
                    if (sent != payload.Length)
                    {
                        Log.Console($"Send should return full length, index: {sentCount}, sent: {sent}");
                        return 1;
                    }

                    sentCount++;
                }

                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                if (!Transfer(clientToServer, server, out string transferError))
                {
                    Log.Console($"Transfer error: {transferError}");
                    return 2;
                }

                if (!Transfer(serverToClient, client, out transferError))
                {
                    Log.Console($"Transfer error: {transferError}");
                    return 3;
                }

                while (TryReceive(server, out byte[] data, out string receiveError))
                {
                    if (receiveError != null)
                    {
                        Log.Console($"Receive error: {receiveError}");
                        return 4;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"Payload validation failed, index: {receivedCount}");
                        return 5;
                    }

                    receivedCount++;
                    receivedInWindow++;
                }
            }

            for (int tick = 0; tick < DrainMaxTicks && receivedCount < sentCount; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                if (!Transfer(clientToServer, server, out string transferError))
                {
                    Log.Console($"Transfer error: {transferError}");
                    return 6;
                }

                if (!Transfer(serverToClient, client, out transferError))
                {
                    Log.Console($"Transfer error: {transferError}");
                    return 7;
                }

                while (TryReceive(server, out byte[] data, out string receiveError))
                {
                    if (receiveError != null)
                    {
                        Log.Console($"Receive error: {receiveError}");
                        return 8;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"Payload validation failed, index: {receivedCount}");
                        return 9;
                    }

                    receivedCount++;
                }
            }

            if (receivedCount != sentCount)
            {
                Log.Console($"Not all messages received: {receivedCount}/{sentCount}");
                return 10;
            }

            long windowBytes = (long)receivedInWindow * PayloadSize;
            double seconds = MeasureMilliseconds / 1000.0;
            double pps = seconds > 0 ? receivedInWindow / seconds : 0;
            double mbPerSec = seconds > 0 ? windowBytes / (1024.0 * 1024.0) / seconds : 0;
            Log.Console($"Kcp performance({MeasureMilliseconds}ms): recv {receivedInWindow} msgs, {windowBytes} bytes, {pps:F0} pps, {mbPerSec:F2} MB/s");

            Log.Debug("Core_Kcp_Performance_Test all passed");
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
