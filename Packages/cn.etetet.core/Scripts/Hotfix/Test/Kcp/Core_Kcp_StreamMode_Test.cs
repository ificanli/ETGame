using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp stream mode test.
    /// </summary>
    public class Core_Kcp_StreamMode_Test : ATestHandler
    {
        private const uint TestConv = 3201;
        private const int StepMilliseconds = 10;
        private const int MaxTicks = 500;
        private const int KcpHeaderSize = (int)KCPBASIC.REVERSED_HEAD;

        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;

            var clientToServer = new List<byte[]>(64);
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
            client.SetStreamMode(1);
            server.SetStreamMode(1);

            byte[] part1 = CreatePayload(120, 1);
            byte[] part2 = CreatePayload(80, 2);
            byte[] part3 = CreatePayload(200, 3);
            byte[] expected = Concat(part1, part2, part3);

            if (client.Send(part1) != part1.Length)
            {
                Log.Console("StreamMode: send part1 failed");
                return 1;
            }

            if (client.Send(part2) != part2.Length)
            {
                Log.Console("StreamMode: send part2 failed");
                return 2;
            }

            if (client.Send(part3) != part3.Length)
            {
                Log.Console("StreamMode: send part3 failed");
                return 3;
            }

            uint now = 0;
            int received = 0;
            var receivedBuffer = new byte[expected.Length];

            for (int tick = 0; tick < MaxTicks && received < expected.Length; ++tick)
            {
                now += (uint)StepMilliseconds;
                client.Update(now);
                server.Update(now);

                if (!Deliver(clientToServer, server, out string error))
                {
                    Log.Console($"StreamMode deliver error (c2s): {error}");
                    return 4;
                }

                if (!Deliver(serverToClient, client, out error))
                {
                    Log.Console($"StreamMode deliver error (s2c): {error}");
                    return 5;
                }

                while (TryReceive(server, out byte[] data, out error))
                {
                    if (error != null)
                    {
                        Log.Console($"StreamMode receive error: {error}");
                        return 6;
                    }

                    Buffer.BlockCopy(data, 0, receivedBuffer, received, data.Length);
                    received += data.Length;
                    if (received >= expected.Length)
                        break;
                }
            }

            if (received != expected.Length)
            {
                Log.Console($"StreamMode: total received length mismatch {received}/{expected.Length}");
                return 7;
            }

            if (!BytesEqual(expected, receivedBuffer))
            {
                Log.Console("StreamMode: received data mismatch");
                return 8;
            }

            Log.Debug("Core_Kcp_StreamMode_Test all passed");
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

        private static byte[] CreatePayload(int size, byte seed)
        {
            var buffer = new byte[size];
            for (int i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = (byte)(seed + i);
            }

            return buffer;
        }

        private static byte[] Concat(byte[] a, byte[] b, byte[] c)
        {
            var buffer = new byte[a.Length + b.Length + c.Length];
            Buffer.BlockCopy(a, 0, buffer, 0, a.Length);
            Buffer.BlockCopy(b, 0, buffer, a.Length, b.Length);
            Buffer.BlockCopy(c, 0, buffer, a.Length + b.Length, c.Length);
            return buffer;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null)
                return false;
            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; ++i)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }
    }
}
