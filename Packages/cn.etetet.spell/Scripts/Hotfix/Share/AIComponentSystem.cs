namespace ET
{
    [EntitySystemOf(typeof(AIComponent))]
    public static partial class AIComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AIComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this AIComponent self)
        {
        }

        public static void AddAI(this AIComponent self, Buff buff)
        {
            Buff oldTop = self.GetTopInvalid();
            oldTop?.GetComponent<BuffTickComponent>().Stop();

            self.AIStack.Push(buff);
            buff.GetComponent<BuffTickComponent>().Start();
        }

        public static void ResumeAI(this AIComponent self)
        {
            Buff buff = self.GetTopInvalid();
            buff?.GetComponent<BuffTickComponent>().Start();
        }

        private static Buff GetTopInvalid(this AIComponent self)
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
