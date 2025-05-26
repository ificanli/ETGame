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
        public void Debug(Fiber fiber, object msg)
        {
            Type type = msg.GetType();
            if (this.ignore.Contains(type))
            {
                return;
            }
            fiber.Log.Debug($"{fiber.Root.Name,-10} {fiber.Root.SceneType}: {msg}");
        }
    }
}