using System;
using System.Runtime.InteropServices;
using MemoryPack;

namespace ET
{
    [MemoryPackable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct Address: IEquatable<Address>
    {
        [MemoryPackOrder(0)]
        public int IP;
        [MemoryPackOrder(1)]
        public int Port;
        
        public bool Equals(Address other)
        {
            return this.IP == other.IP && this.Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            return obj is Address other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.IP, this.Port);
        }
        
        public Address(int ip, int port)
        {
            this.IP = ip;
            this.Port = port;
        }
        
        public Address(long ipPort)
        {
            this.IP = (int)(ipPort >> 32);
            this.Port = (int)(ipPort & 0xffff);
        }

        public static bool operator ==(Address left, Address right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Address left, Address right)
        {
            return !(left == right);
        }

        public long ToLong()
        {
            return ((long)this.IP << 32) | (uint)this.Port;
        }

        public override string ToString()
        {
            return $"{this.IP}:{this.Port}";
        }
    }
    
    [MemoryPackable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public partial struct FiberInstanceId: IEquatable<FiberInstanceId>
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
            return left.Equals(right);
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
    public partial struct ActorId: IEquatable<ActorId>
    {
        [MemoryPackOrder(0)]
        public Address Address;
        
        [MemoryPackOrder(1)]
        public FiberInstanceId FiberInstanceId;
        
        
        public ActorId(Address address, FiberInstanceId fiberInstanceId)
        {
            this.Address = address;
            this.FiberInstanceId = fiberInstanceId;
        }
        
        public bool Equals(ActorId other)
        {
            return this.Address == other.Address && this.FiberInstanceId == other.FiberInstanceId;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Address.GetHashCode(), this.FiberInstanceId.GetHashCode());
        }
        
        public static bool operator ==(ActorId left, ActorId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorId left, ActorId right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{this.Address}:{this.FiberInstanceId}";
        }
    }
}