using System.Collections.Generic;

namespace ET
{
    // Unit 上的 AI 调度组件：管理 Buff AI 栈，保证同一时间只运行一个 Buff AI
    [ComponentOf(typeof(Unit))]
    public class BTCoroutineComponent: Entity, IAwake, IDestroy
    {
        public readonly Stack<EntityRef<Buff>> AIStack = new();
    }
}
