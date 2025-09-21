using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ET
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IdStruct
    {
        public ushort Process;  // 16bit
        public uint Time;    // 32bit
        public uint Value;   // 16bit

        public long ToLong()
        {
            ulong result = 0;
            result |= this.Process;
            result <<= 32;
            result |= this.Time;
            result <<= 16;
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
            this.Value = (uint) (result & IdGenerater.Mask16bit);
            result >>= 16;
            this.Time = (uint) result & IdGenerater.Mask32bit;
            result >>= 32;
            this.Process = (ushort) (result & IdGenerater.Mask16bit);
        }

        public override string ToString()
        {
            return $"process: {this.Process}, time: {this.Time}, value: {this.Value}";
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InstanceIdStruct
    {
        public uint Time;  // 32bit
        public uint Value; // 32bit

        public long ToLong()
        {
            ulong result = 0;
            result |= this.Time;
            result <<= 32;
            result |= this.Value;
            return (long) result;
        }

        public InstanceIdStruct(uint time, uint value)
        {
            this.Time = time;
            this.Value = value;
        }

        public InstanceIdStruct(long id)
        {
            ulong result = (ulong) id; 
            this.Value = (uint)(result & uint.MaxValue);
            result >>= 32;
            this.Time = (uint)(result & uint.MaxValue);
        }

        public override string ToString()
        {
            return $"time: {this.Time}, value: {this.Value}";
        }
    }

    public class IdGenerater: Singleton<IdGenerater>, ISingletonAwake
    {
        public const uint Mask32bit = 0xffffffff;
        public const uint Mask16bit = 0xffff;
        
        private long epoch2025;
        
        private uint value;
        private int instanceIdValue;
        
        public void Awake()
        {
            long epoch1970tick = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000;
            this.epoch2025 = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / 10000 - epoch1970tick;
        }

        private uint TimeSince2025()
        {
            uint a = (uint)((TimeInfo.Instance.FrameTime - this.epoch2025) / 1000);
            return a;
        }
        
        public long GenerateId()
        {
            uint time = TimeSince2025();
            uint v = 0;
            // 这里必须加锁
            lock (this)
            {
                if (++this.value == Mask32bit)
                {
                    this.value = 0;
                }
                v = this.value;
            }

            // 因为改成了服务发现，支持一个进程多个副本，比如gate只需要配一个进程，可以支持多个,同一个Replica是同一个进程Id
            if (Options.Instance.Process * 1000 > 50000)
            {
                throw new Exception("Process is too large");
            }
            
            IdStruct idStruct = new(time, (ushort)(Options.Instance.Process * 1000 + Options.Instance.ReplicaIndex), v);
            return idStruct.ToLong();
        }
        
        public long GenerateInstanceId()
        {
            uint time = this.TimeSince2025();
            uint v = (uint)Interlocked.Add(ref this.instanceIdValue, 1);
            InstanceIdStruct instanceIdStruct = new(time, v);
            return instanceIdStruct.ToLong();
        }
    }
}