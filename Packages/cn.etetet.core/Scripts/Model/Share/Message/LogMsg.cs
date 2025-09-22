using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ET
{
    public class LogMsg: Singleton<LogMsg>, ISingletonAwake
    {
        private readonly HashSet<Type> ignore = new();

        public void Awake()
        {
        }

        public void AddIgnore(Type type)
        {
            this.ignore.Add(type);
        }

        [Conditional("DEBUG")]
        public void Send(Fiber fiber, object msg)
        {
            if (msg is IMessageWrapper messageWrapper)
            {
                msg = messageWrapper.GetMessageObject();
            }

            Type type = msg.GetType();
            if (this.ignore.Contains(type))
            {
                return;
            }
            fiber.Log.Debug($"Send: {msg}");
        }
        
        [Conditional("DEBUG")]
        public void Recv(Fiber fiber, object msg)
        {
            if (msg is IMessageWrapper messageWrapper)
            {
                msg = messageWrapper.GetMessageObject();
            }
            
            Type type = msg.GetType();
            if (this.ignore.Contains(type))
            {
                return;
            }
            fiber.Log.Debug($"Recv: {msg}");
        }
    }
}