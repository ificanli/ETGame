using System;

namespace ET
{
    [EntitySystemOf(typeof(AIComponent))]
    [FriendOf(typeof(AIComponent))]
    public static partial class AIComponentSystem
    {
        [Invoke(TimerInvokeType.AITimer)]
        public class AITimer: ATimer<AIComponent>
        {
            protected override void Run(AIComponent self)
            {
                try
                {
                    self?.Check();
                }
                catch (Exception e)
                {
                    Log.Error($"ai error: {self.Id}\n{e}");
                }
            }
        }
    
        [EntitySystem]
        private static void Awake(this AIComponent self, AIRoot aiRoot)
        {
            self.AIRoot = aiRoot;
            self.Start();
        }
        
        [EntitySystem]
        private static void Awake(this AIComponent self, int ai)
        {
            try
            {
                AIConfig aiRoot = AIConfigCategory.Instance.Get(ai);
                self.AIRoot = aiRoot.Root;
                self.Start();
            }
            catch (Exception e)
            {
                throw new Exception($"ai error: {ai}", e);
            }

        }

        [EntitySystem]
        private static void Destroy(this AIComponent self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                root.GetComponent<TimerComponent>()?.Remove(ref self.Timer);    
            }
            self.CancellationToken?.Cancel();
            self.CancellationToken = null;
            self.Current = 0;
        }

        private static void Check(this AIComponent self)
        {
            Fiber fiber = self.Fiber();
            if (self.Parent == null)
            {
                fiber.Root.GetComponent<TimerComponent>().Remove(ref self.Timer);
                return;
            }

            AIRoot aiRoot = self.AIRoot;
            if (aiRoot != null)
            {
                using BTEnv env = BTEnv.Create(self.Scene());
                env.AddEntity(aiRoot.Unit, self.GetParent<Unit>());
                BTDispatcher.Instance.Handle(self.AIRoot, env);
            }
        }
        
        public static void Start(this AIComponent self)
        {
            if (self.Timer != 0)
            {
                return;
            }
            self.Timer = self.Root().GetComponent<TimerComponent>().NewRepeatedTimer(1000, TimerInvokeType.AITimer, self);
        }
        
        public static void Stop(this AIComponent self)
        {
            if (self.Timer == 0)
            {
                return;
            }
            
            self.Cancel();
            self.Root().GetComponent<TimerComponent>().Remove(ref self.Timer);
        }

        public static void Cancel(this AIComponent self)
        {
            self.CancellationToken?.Cancel();
            self.Current = 0;
            self.CancellationToken = null;
        }
    }
} 