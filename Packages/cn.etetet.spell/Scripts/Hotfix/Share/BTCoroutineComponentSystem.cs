namespace ET
{
    [EntitySystemOf(typeof(BTCoroutineComponent))]
    public static partial class BTCoroutineComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BTCoroutineComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this BTCoroutineComponent self)
        {
        }

        public static void AddAI(this BTCoroutineComponent self, Buff buff)
        {
            Buff oldTop = self.GetTopInvalid();
            oldTop?.GetComponent<BuffTickComponent>().Stop();

            self.AIStack.Push(buff);
            buff.GetComponent<BuffTickComponent>().Start();
        }

        public static void ResumeAI(this BTCoroutineComponent self)
        {
            Buff buff = self.GetTopInvalid();
            buff?.GetComponent<BuffTickComponent>().Start();
        }

        private static Buff GetTopInvalid(this BTCoroutineComponent self)
        {
            while (self.AIStack.Count > 0)
            {
                Buff buff = self.AIStack.Peek();
                if (buff == null)
                {
                    self.AIStack.Pop();
                    continue;
                }
                break;
            }
            
            if (self.AIStack.Count == 0)
            {
                return null;
            }

            return self.AIStack.Peek();
        }
    }
}
