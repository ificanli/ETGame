using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp basic functionality test.
    /// Covers send/receive, fragmentation, and basic options.
    /// </summary>
    public class Core_Kcp_Function_Test : ATestHandler
    {
        private const uint TestConv = 1001;
        private const int StepMilliseconds = 10;
        private const int MaxTicks = 500;
        private const int SmallPayloadSize = 64;
        private const int LargePayloadSize = 8192;
        private const int TestMtu = 470;
        private const int TestWindowSize = 256;
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

            if (!client.IsSet || !server.IsSet)
            {
                Log.Console("Kcp should be initialized for both endpoints");
                return 1;
            }

            if (client.ConversationId != TestConv || server.ConversationId != TestConv)
            {
                Log.Console("ConversationId should match on both endpoints");
                return 2;
            }

            if (client.PeekSize() != -1 || server.PeekSize() != -1)
            {
                Log.Console("PeekSize on empty queue should return -1");
                return 3;
            }

            if (client.SetMtu(TestMtu) != 0)
            {
                Log.Console("SetMtu should succeed for valid mtu");
                return 4;
            }

            if (client.MaximumTransmissionUnit != TestMtu)
            {
                Log.Console($"MaximumTransmissionUnit should be {TestMtu}");
                return 5;
            }

            if (server.SetMtu(TestMtu) != 0)
            {
                Log.Console("SetMtu should succeed for server endpoint");
                return 6;
            }

            if (server.MaximumTransmissionUnit != TestMtu)
            {
                Log.Console($"Server MaximumTransmissionUnit should be {TestMtu}");
                return 7;
            }

            client.SetWindowSize(TestWindowSize, TestWindowSize);
            if (client.SendWindowSize != TestWindowSize || client.ReceiveWindowSize != TestWindowSize)
            {
                Log.Console("Window size should be updated after SetWindowSize");
                return 8;
            }

            client.SetNoDelay(1, StepMilliseconds, 2, 1);
            if (client.NoDelay != 1)
            {
                Log.Console("NoDelay should be 1 after SetNoDelay");
                return 9;
            }

            uint now = 0;

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

            bool WaitForMessage(Kcp receiver, byte[] expected, int maxTicks, out string error)
            {
                for (int i = 0; i < maxTicks; ++i)
                {
                    now += (uint)StepMilliseconds;
                    client.Update(now);
                    server.Update(now);

                    if (!Transfer(clientToServer, server, out error))
                        return false;
                    if (!Transfer(serverToClient, client, out error))
                        return false;

                    if (TryReceive(receiver, out byte[] data, out error))
                    {
                        if (error != null)
                            return false;
                        if (!BytesEqual(data, expected))
                        {
                            error = "Payload mismatch";
                            return false;
                        }

                        return true;
                    }
                }

                error = "Timed out waiting for message";
                return false;
            }

            byte[] smallPayload = CreatePayload(SmallPayloadSize, 1);
            if (client.Send(smallPayload) != smallPayload.Length)
            {
                Log.Console("Send should return the full payload length for small packet");
                return 10;
            }

            if (!WaitForMessage(server, smallPayload, MaxTicks, out string smallError))
            {
                Log.Console($"Small payload receive failed: {smallError}");
                return 11;
            }
            Log.Debug("Small payload send/receive passed");

            byte[] largePayload = CreatePayload(LargePayloadSize, 2);
            if (server.Send(largePayload) != largePayload.Length)
            {
                Log.Console("Send should return the full payload length for large packet");
                return 12;
            }

            if (!WaitForMessage(client, largePayload, MaxTicks, out string largeError))
            {
                Log.Console($"Large payload receive failed: {largeError}");
                return 13;
            }
            Log.Debug("Large payload send/receive passed");

            Log.Debug("Core_Kcp_Function_Test all passed");
            return ErrorCode.ERR_Success;
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
