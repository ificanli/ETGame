using System;
using System.IO;

namespace ET
{
    public static class MessageSerializeHelper
    {
        public static byte[] Serialize(MessageObject message)
        {
            return MemoryPackHelper.Serialize(message);
        }

        public static void Serialize(MessageObject message, MemoryBuffer stream)
        {
            MemoryPackHelper.Serialize(message, stream);
        }
		
        public static MessageObject Deserialize(Type type, byte[] bytes, int index, int count)
        {
            object o = ObjectPool.Fetch(type);
            MemoryPackHelper.Deserialize(type, bytes, index, count, ref o);
            return o as MessageObject;
        }

        public static MessageObject Deserialize(Type type, MemoryBuffer stream)
        {
            object o = ObjectPool.Fetch(type);
            MemoryPackHelper.Deserialize(type, stream, ref o);
            return o as MessageObject;
        }
        
        public static ushort MessageToStream(MemoryBuffer stream, MessageObject message, int headOffset = 0)
        {
            ushort opcode = OpcodeType.Instance.GetOpcode(message.GetType());
            stream.Seek(headOffset + Packet.OpcodeLength, SeekOrigin.Begin);
            stream.SetLength(headOffset + Packet.OpcodeLength);
            
            stream.GetBuffer().WriteTo(headOffset, opcode);
            
            MessageSerializeHelper.Serialize(message, stream);
            
            stream.Seek(0, SeekOrigin.Begin);
            return opcode;
        }
        
        public static (ushort, MemoryBuffer) ToMemoryBuffer(AService service, FiberInstanceId fiberInstanceId, object message)
        {
            MemoryBuffer memoryBuffer = service.Fetch();
            ushort opcode = 0;
            switch (service.ServiceType)
            {
                case ServiceType.Inner:
                {
                    opcode = MessageToStream(memoryBuffer, (MessageObject)message, Packet.FiberInstanceIdLength);
                    memoryBuffer.GetBuffer().WriteTo(0, fiberInstanceId);
                    break;
                }
                case ServiceType.Outer:
                {
                    opcode = MessageToStream(memoryBuffer, (MessageObject)message);
                    break;
                }
            }
            
            return (opcode, memoryBuffer);
        }
        
        public static (FiberInstanceId, object) ToMessage(AService service, MemoryBuffer memoryStream)
        {
            object message = null;
            FiberInstanceId fiberInstanceId = default;
            switch (service.ServiceType)
            {
                case ServiceType.Outer:
                {
                    memoryStream.Seek(Packet.OpcodeLength, SeekOrigin.Begin);
                    ushort opcode = BitConverter.ToUInt16(memoryStream.GetBuffer(), 0);
                    Type type = OpcodeType.Instance.GetType(opcode);
                    message = Deserialize(type, memoryStream);
                    break;
                }
                case ServiceType.Inner:
                {
                    memoryStream.Seek(Packet.FiberInstanceIdLength + Packet.OpcodeLength, SeekOrigin.Begin);
                    byte[] buffer = memoryStream.GetBuffer();
                    fiberInstanceId.Fiber = BitConverter.ToInt32(buffer, Packet.FiberInstanceIdIndex);
                    fiberInstanceId.InstanceId = BitConverter.ToInt32(buffer, Packet.FiberInstanceIdIndex + 4);
                    ushort opcode = BitConverter.ToUInt16(buffer, Packet.FiberInstanceIdLength);
                    
                    Type type = OpcodeType.Instance.GetType(opcode);
                    message = Deserialize(type, memoryStream);
                    break;
                }
            }
            
            return (fiberInstanceId, message);
        }
    }
}