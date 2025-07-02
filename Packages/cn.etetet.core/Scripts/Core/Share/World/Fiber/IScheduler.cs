using System;

namespace ET
{
    internal interface IScheduler
    {
        void AddToScheduler(Fiber fiber);
        void Dispose();
    }
}