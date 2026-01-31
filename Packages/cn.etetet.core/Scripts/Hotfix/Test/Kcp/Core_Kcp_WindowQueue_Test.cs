using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp window and queue behavior test.
    /// </summary>
    public class Core_Kcp_WindowQueue_Test : ATestHandler
    {
        private const uint TestConv = 3301;
        private const int StepMilliseconds = 10;
        private const int MaxTicks = 1000;
        private const int MessageCount = 200;
        private const int PayloadSize = 64;
        private const int KcpHeaderSize = (int)KCPBASIC.REVERSED_HEAD;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            var clientToServer = new List<byte[]>(256);
            var serverToClient = new List<byte[]>(64);

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

            if (client.WaitSendCount <= 0)
            {
                Log.Console("WaitSendCount should be > 0 after queued sends");
                return 2;
            }

            uint now = 0;
            int receivedCount = 0;

            for (int tick = 0; tick < MaxTicks && receivedCount < MessageCount; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                if (!Deliver(clientToServer, server, out string error))
                {
                    Log.Console($"Deliver error (c2s): {error}");
                    return 3;
                }

                if (!Deliver(serverToClient, client, out error))
                {
                    Log.Console($"Deliver error (s2c): {error}");
                    return 4;
                }

                while (TryReceive(server, out byte[] data, out error))
                {
                    if (error != null)
                    {
                        Log.Console($"Receive error: {error}");
                        return 5;
                    }

                    if (!ValidatePayload(data, receivedCount))
                    {
                        Log.Console($"Payload validation failed, index: {receivedCount}");
                        return 6;
                    }

                    receivedCount++;
                    if (receivedCount >= MessageCount)
                        break;
                }
            }

            if (receivedCount != MessageCount)
            {
                Log.Console($"Not all messages received: {receivedCount}/{MessageCount}");
                return 7;
            }

            bool waitSendCleared = false;
            for (int tick = 0; tick < MaxTicks; ++tick)
            {
                if (client.WaitSendCount == 0)
                {
                    waitSendCleared = true;
                    break;
                }

                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                if (!Deliver(clientToServer, server, out string error))
                {
                    Log.Console($"Deliver error (c2s) while draining: {error}");
                    return 8;
                }

                if (!Deliver(serverToClient, client, out error))
                {
                    Log.Console($"Deliver error (s2c) while draining: {error}");
                    return 9;
                }
            }

            if (!waitSendCleared)
            {
                Log.Console($"WaitSendCount should be 0 after all acked, actual: {client.WaitSendCount}");
                return 10;
            }

            Log.Debug("Core_Kcp_WindowQueue_Test all passed");
            return ErrorCode.ERR_Success;
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
