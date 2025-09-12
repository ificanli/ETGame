using System;
using System.Runtime.InteropServices;
using MemoryPack;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [MemoryPackable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct FiberInstanceId
    {
        [MemoryPackOrder(0)]
        public int Fiber;
        [MemoryPackOrder(1)]
        public int InstanceId;
        
        public bool Equals(FiberInstanceId other)
        {
            return this.Fiber == other.Fiber && this.InstanceId == other.InstanceId;
        }

        public override bool Equals(object obj)
        {
            return obj is FiberInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Fiber, this.InstanceId);
        }
        
        public FiberInstanceId(int fiber, int instanceId)
        {
            this.Fiber = fiber;
            this.InstanceId = instanceId;
        }
        
        public FiberInstanceId(int fiber)
        {
            this.Fiber = fiber;
            this.InstanceId = 1;
        }

        public static bool operator ==(FiberInstanceId left, FiberInstanceId right)
        {
            return left.Fiber == right.Fiber && left.InstanceId == right.InstanceId;
        }

        public static bool operator !=(FiberInstanceId left, FiberInstanceId right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{this.Fiber}:{this.InstanceId}";
        }
    }
    
    [MemoryPackable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct ActorId
    {
        [MemoryPackOrder(0)]
        public int Process;
        
        [MemoryPackOrder(1)]
        public int Fiber;
        
        public bool Equals(ActorId other)
        {
            return this.Process == other.Process && this.Fiber == other.Fiber && this.InstanceId == other.InstanceId;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Process, this.Fiber, this.InstanceId);
        }

        [MemoryPackOrder(2)]
        public int InstanceId;
        
        public FiberInstanceId FiberInstanceId
        {
            get
            {
                return new FiberInstanceId(this.Fiber, this.InstanceId);
            }
        }
        
        public ActorId(int process, int fiber)
        {
            this.Process = process;
            this.Fiber = fiber;
            this.InstanceId = 1;
        }
        
        public ActorId(int process, int fiber, int instanceId)
        {
            this.Process = process;
            this.Fiber = fiber;
            this.InstanceId = instanceId;
        }
        
        public ActorId(int process, FiberInstanceId fiberInstanceId)
        {
            this.Process = process;
            this.Fiber = fiberInstanceId.Fiber;
            this.InstanceId = fiberInstanceId.InstanceId;
        }
        
        public static bool operator ==(ActorId left, ActorId right)
        {
            return left.InstanceId == right.InstanceId && left.Process == right.Process && left.Fiber == right.Fiber;
        }

        public static bool operator !=(ActorId left, ActorId right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{this.Process}:{this.Fiber}:{this.InstanceId}";
        }
    }
}