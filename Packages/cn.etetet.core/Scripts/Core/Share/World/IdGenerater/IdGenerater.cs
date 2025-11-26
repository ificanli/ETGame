using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IdStruct
    {
        public ushort Process;  // 16bit
        public uint Time;    // 28bit  可用大约8年
        public uint Value;   // 20bit

        public long ToLong()
        {
            ulong result = 0;
            result |= this.Process;
            result <<= 28;
            result |= this.Time;
            result <<= 20;
            result |= this.Value;
            return (long) result;
        }

        public IdStruct(uint time, ushort process, uint value)
        {
            this.Process = process;
            this.Time = time;
            this.Value = value;
        }

        public IdStruct(long id)
        {
            ulong result = (ulong) id;
            this.Value = (uint) (result & IdGenerater.Mask20bit);
            result >>= 20;
            this.Time = (uint) (result & IdGenerater.Mask28bit);
            result >>= 28;
            this.Process = (ushort) (result & IdGenerater.Mask16bit);
        }

        public override string ToString()
        {
            return $"process: {this.Process}, time: {this.Time}, value: {this.Value}";
        }
    }

    public class IdGenerater: Singleton<IdGenerater>, ISingletonAwake
    {
        public const uint Mask32bit = 0xffffffff;
        public const uint Mask28bit = 0xfffffff;  // 28bit mask: 268,435,455
        public const uint Mask20bit = 0xfffff;    // 20bit mask: 1,048,575
        public const uint Mask16bit = 0xffff;
        
        private long epoch2025;
        
        private uint value;
        private uint second; // 秒
        
        public void Awake()
        {
            long epoch1970tick = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
            this.epoch2025 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970tick;
        }

        private uint TimeSince2025()
        {
            uint a = (uint)((TimeInfo.Instance.ClientNow() - this.epoch2025) / 1000);
            return a;
        }
        
        public long GenerateId()
        {
            uint time = TimeSince2025();
            uint v = 0;
            uint s = 0;
            // 这里必须加锁
            lock (this)
            {
                if (time > this.second)
                {
                    this.value = 0;
                    this.second = time;
                }
                else
                {
                    ++this.value;
                    if (this.value == IdGenerater.Mask20bit)  
                    {
                        ++this.second; // 借用下一秒的id
                        this.value = 0;
                    }
                }
                s = this.second;
                v = this.value;
            }

            IdStruct idStruct = new(s, this.GetProcessReplicaIndex(), v);
            return idStruct.ToLong();
        }

        public ushort GetProcessReplicaIndex()
        {
            // 因为改成了服务发现，支持一个进程多个副本，比如gate只需要配一个进程，可以支持多个,同一个Replica是同一个进程Id
            // 需要预留出来Replica数量的进程Id
            if (Options.Instance.Process > 50000)
            {
                throw new Exception($"Process is too large: {Options.Instance.Process}");
            }
            return (ushort)(Options.Instance.Process + Options.Instance.ReplicaIndex);
        }
    }
}