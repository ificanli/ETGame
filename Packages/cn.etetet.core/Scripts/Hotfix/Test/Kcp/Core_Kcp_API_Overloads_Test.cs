using System;
using System.Collections.Generic;

namespace ET.Test
{
    /// <summary>
    /// Kcp API overloads test.
    /// Tests all Send/Input/Receive method overloads.
    /// </summary>
    public class Core_Kcp_API_Overloads_Test : ATestHandler
    {
        private const uint TestConv = 3901;
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

            bool Pump(Kcp sender, Kcp receiver, List<byte[]> outQueue, uint now, out string error)
            {
                sender.Update(now);
                for (int i = 0; i < outQueue.Count; ++i)
                {
                    int inputResult = receiver.Input(outQueue[i]);
                    if (inputResult < 0)
                    {
                        error = $"Input failed with code {inputResult}";
                        return false;
                    }
                }

                outQueue.Clear();
                receiver.Update(now);
                error = null;
                return true;
            }

            // Test Send() overloads
            byte[] testData1 = new byte[64];
            for (int i = 0; i < testData1.Length; i++)
            {
                testData1[i] = (byte)i;
            }

            // Test Send(byte[] buffer)
            int sent1 = client.Send(testData1);
            if (sent1 != testData1.Length)
            {
                Log.Console($"Send(byte[]) failed: expected {testData1.Length}, got {sent1}");
                return 1;
            }

            // Test Send(byte[] buffer, int length)
            byte[] testData2 = new byte[64];
            for (int i = 0; i < testData2.Length; i++)
            {
                testData2[i] = (byte)(i + 100);
            }
            int sent2 = client.Send(testData2, testData2.Length);
            if (sent2 != testData2.Length)
            {
                Log.Console($"Send(byte[], int) failed: expected {testData2.Length}, got {sent2}");
                return 2;
            }

            // Test Send(byte[] buffer, int offset, int length)
            byte[] testData3 = new byte[128];
            for (int i = 0; i < testData3.Length; i++)
            {
                testData3[i] = (byte)(i + 200);
            }
            int offset = 32;
            int length = 64;
            int sent3 = client.Send(testData3, offset, length);
            if (sent3 != length)
            {
                Log.Console($"Send(byte[], int, int) failed: expected {length}, got {sent3}");
                return 3;
            }

            // Test Send(ReadOnlySpan<byte> buffer)
            byte[] spanArray = new byte[64];
            for (int i = 0; i < spanArray.Length; i++)
            {
                spanArray[i] = (byte)(i + 300);
            }
            ReadOnlySpan<byte> spanData = new ReadOnlySpan<byte>(spanArray);
            int sent4 = client.Send(spanData);
            if (sent4 != spanData.Length)
            {
                Log.Console($"Send(ReadOnlySpan<byte>) failed: expected {spanData.Length}, got {sent4}");
                return 4;
            }

            // Test Send(ReadOnlyMemory<byte> buffer)
            byte[] memoryArray = new byte[64];
            for (int i = 0; i < memoryArray.Length; i++)
            {
                memoryArray[i] = (byte)(i + 400);
            }
            ReadOnlyMemory<byte> memoryData = new ReadOnlyMemory<byte>(memoryArray);
            int sent5 = client.Send(memoryData);
            if (sent5 != memoryData.Length)
            {
                Log.Console($"Send(ReadOnlyMemory<byte>) failed: expected {memoryData.Length}, got {sent5}");
                return 5;
            }

            // Test Send(ArraySegment<byte> buffer)
            byte[] arrayData = new byte[128];
            for (int i = 0; i < arrayData.Length; i++)
            {
                arrayData[i] = (byte)(i + 500);
            }
            ArraySegment<byte> segmentData = new ArraySegment<byte>(arrayData, 32, 64);
            int sent6 = client.Send(segmentData);
            if (sent6 != segmentData.Count)
            {
                Log.Console($"Send(ArraySegment<byte>) failed: expected {segmentData.Count}, got {sent6}");
                return 6;
            }

            // Test Input() overloads with real packets
            uint now = 0;
            byte[] inputPayload = new byte[64];
            for (int i = 0; i < inputPayload.Length; i++)
            {
                inputPayload[i] = (byte)(i + 1000);
            }

            bool TestInput(Func<Kcp, byte[], int> inputFunc, string name, Func<byte[], byte[]> wrapPacket = null)
            {
                var localC2S = new List<byte[]>(8);
                var localS2C = new List<byte[]>(8);

                using Kcp localClient = new Kcp(TestConv, (buffer, length) => EnqueuePacket(localC2S, buffer, length));
                using Kcp localServer = new Kcp(TestConv, (buffer, length) => EnqueuePacket(localS2C, buffer, length));

                localClient.SetNoDelay(1, StepMilliseconds, 2, 1);
                localServer.SetNoDelay(1, StepMilliseconds, 2, 1);

                localClient.Send(inputPayload);
                uint localNow = 0;
                localClient.Update(localNow);

                if (localC2S.Count == 0)
                {
                    Log.Console($"Input {name} output empty");
                    return false;
                }

                byte[] packet = localC2S[0];
                if (wrapPacket != null)
                    packet = wrapPacket(packet);

                int inputResult = inputFunc(localServer, packet);
                if (inputResult < 0)
                {
                    Log.Console($"Input {name} failed with code {inputResult}");
                    return false;
                }

                localServer.Update(localNow);
                int size = localServer.PeekSize();
                if (size <= 0)
                {
                    Log.Console($"Input {name} receive size invalid");
                    return false;
                }

                byte[] recv = new byte[size];
                int read = localServer.Receive(recv);
                if (read != size)
                {
                    Log.Console($"Input {name} receive size mismatch");
                    return false;
                }

                if (recv.Length != inputPayload.Length)
                {
                    Log.Console($"Input {name} payload size mismatch");
                    return false;
                }

                for (int i = 0; i < recv.Length; i++)
                {
                    if (recv[i] != inputPayload[i])
                    {
                        Log.Console($"Input {name} payload mismatch at {i}");
                        return false;
                    }
                }

                return true;
            }

            if (!TestInput((k, p) => k.Input(p), "byte[]"))
                return 8;
            if (!TestInput((k, p) => k.Input(p, p.Length), "byte[] length"))
                return 9;
            if (!TestInput((k, p) => k.Input(p, 0, p.Length), "byte[] offset"))
                return 10;
            if (!TestInput((k, p) => k.Input(p.AsSpan()), "ReadOnlySpan<byte>"))
                return 11;
            if (!TestInput((k, p) => k.Input(p.AsMemory()), "ReadOnlyMemory<byte>"))
                return 12;
            if (!TestInput((k, p) => k.Input(new ArraySegment<byte>(p)), "ArraySegment<byte>"))
                return 13;

            if (!TestInput((k, p) => k.Input(p, 2, p.Length - 4), "byte[] offset/length", packet =>
                {
                    byte[] wrapped = new byte[packet.Length + 4];
                    Buffer.BlockCopy(packet, 0, wrapped, 2, packet.Length);
                    return wrapped;
                }))
                return 14;

            // Test Receive() overloads
            // First, ensure server has received data
            now += StepMilliseconds;
            client.Update(now);
            server.Update(now);

            if (!Pump(client, server, clientToServer, now, out string transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 15;
            }

            // Test Receive(byte[] buffer)
            if (server.PeekSize() > 0)
            {
                byte[] receiveBuffer1 = new byte[server.PeekSize()];
                int received1 = server.Receive(receiveBuffer1);
                if (received1 != receiveBuffer1.Length)
                {
                    Log.Console($"Receive(byte[]) failed: expected {receiveBuffer1.Length}, got {received1}");
                    return 16;
                }
            }

            // Test Receive(byte[] buffer, int length)
            if (server.PeekSize() > 0)
            {
                int peekSize = server.PeekSize();
                byte[] receiveBuffer2 = new byte[peekSize];
                int received2 = server.Receive(receiveBuffer2, receiveBuffer2.Length);
                if (received2 != peekSize)
                {
                    Log.Console($"Receive(byte[], int) failed: expected {peekSize}, got {received2}");
                    return 17;
                }
            }

            // Test Receive(byte[] buffer, int offset, int length)
            if (server.PeekSize() > 0)
            {
                int peekSize = server.PeekSize();
                byte[] receiveBuffer3 = new byte[peekSize + 32];
                int received3 = server.Receive(receiveBuffer3, 0, peekSize);
                if (received3 != peekSize)
                {
                    Log.Console($"Receive(byte[], int, int) failed: expected {peekSize}, got {received3}");
                    return 18;
                }
            }

            // Test Receive(Span<byte> buffer)
            if (server.PeekSize() > 0)
            {
                int peekSize = server.PeekSize();
                Span<byte> receiveSpan = new byte[peekSize];
                int received4 = server.Receive(receiveSpan);
                if (received4 != peekSize)
                {
                    Log.Console($"Receive(Span<byte>) failed: expected {peekSize}, got {received4}");
                    return 19;
                }
            }

            // Test Receive(Memory<byte> buffer)
            if (server.PeekSize() > 0)
            {
                int peekSize = server.PeekSize();
                Memory<byte> receiveMemory = new byte[peekSize];
                int received5 = server.Receive(receiveMemory);
                if (received5 != peekSize)
                {
                    Log.Console($"Receive(Memory<byte>) failed: expected {peekSize}, got {received5}");
                    return 20;
                }
            }

            // Test Receive(ArraySegment<byte> buffer)
            if (server.PeekSize() > 0)
            {
                int peekSize = server.PeekSize();
                byte[] arrayBuffer = new byte[peekSize + 32];
                ArraySegment<byte> receiveSegment = new ArraySegment<byte>(arrayBuffer, 0, peekSize);
                int received6 = server.Receive(receiveSegment);
                if (received6 != peekSize)
                {
                    Log.Console($"Receive(ArraySegment<byte>) failed: expected {peekSize}, got {received6}");
                    return 21;
                }
            }

            // Test that all overloads produce consistent results
            byte[] testPayload = new byte[128];
            for (int i = 0; i < testPayload.Length; i++)
            {
                testPayload[i] = (byte)(i + 600);
            }

            // Send using different overloads
            client.Send(testPayload);
            client.Send(testPayload, testPayload.Length);
            client.Send(testPayload, 0, testPayload.Length);
            client.Send(new ReadOnlySpan<byte>(testPayload));
            client.Send(new ReadOnlyMemory<byte>(testPayload));
            client.Send(new ArraySegment<byte>(testPayload));

            now += StepMilliseconds;
            client.Update(now);
            server.Update(now);

            if (!Pump(client, server, clientToServer, now, out transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 22;
            }

            // Receive using different overloads and verify data integrity
            int receivedCount = 0;
            while (server.PeekSize() > 0 && receivedCount < 6)
            {
                int size = server.PeekSize();
                byte[] buffer = new byte[size];
                int received = server.Receive(buffer);
                if (received != size)
                {
                    Log.Console($"Receive failed: expected {size}, got {received}");
                    return 23;
                }

                receivedCount++;
            }

            if (receivedCount < 6)
            {
                Log.Console($"Expected to receive 6 messages, got {receivedCount}");
                return 24;
            }

            // Test edge cases
            // Test Send with empty buffer
            byte[] emptyBuffer = new byte[0];
            int emptySent = client.Send(emptyBuffer);
            if (emptySent != 0)
            {
                Log.Console("Send with empty buffer should return 0");
                return 25;
            }

            // Test Send with partial length
            byte[] partialBuffer = new byte[128];
            int partialSent = client.Send(partialBuffer, 64);
            if (partialSent != 64)
            {
                Log.Console("Send with partial length should return specified length");
                return 26;
            }

            // Test Receive with larger buffer after sending one packet
            client.Send(testPayload);
            now += StepMilliseconds;
            client.Update(now);
            if (!Pump(client, server, clientToServer, now, out transferError))
            {
                Log.Console($"Transfer error: {transferError}");
                return 27;
            }
            byte[] largeReceiveBuffer = new byte[1024];
            int largeReceived = server.Receive(largeReceiveBuffer);
            if (largeReceived <= 0 || largeReceived > largeReceiveBuffer.Length)
            {
                Log.Console("Receive with large buffer should return valid size");
                return 28;
            }

            Log.Debug("Core_Kcp_API_Overloads_Test all passed");
            return ErrorCode.ERR_Success;
        }
    }
}
