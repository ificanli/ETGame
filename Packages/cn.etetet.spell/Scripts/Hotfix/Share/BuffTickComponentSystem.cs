namespace ET
{
    [EntitySystemOf(typeof(BuffTickComponent))]
    public static partial class BuffTickComponentSystem
    {
        [Invoke(TimerInvokeType.BuffTickTimer)]
        public class BuffTickTimer: ATimer<BuffTickComponent>
        {
            protected override void Run(BuffTickComponent self)
            {
                self.OnTick();
            }
        }

        [EntitySystem]
        private static void Awake(this BuffTickComponent self)
        {
            Buff buff = self.GetParent<Buff>();
            EffectServerBuffTick effect = buff.GetConfig().GetEffect<EffectServerBuffTick>();
            if (effect == null)
            {
                Log.Error($"buff set tick but not found EffectServerBuffTick: {buff.ConfigId}");
                return;
            }
            self.Override = effect.Override;

            if (self.Override)
            {
                Unit unit = buff.Parent.GetParent<Unit>();
                self.UnitId = unit.Id;

                AIComponent aiComponent = unit.GetComponent<AIComponent>() ?? unit.AddComponent<AIComponent>();
                aiComponent.AddAI(buff);
            }
            
            self.Start();
        }
        
        [EntitySystem]
        private static void Destroy(this BuffTickComponent self)
        {
            self.Stop();
            
            if (self.Override)
            {
                Unit unit = self.Scene()?.GetComponent<UnitComponent>()?.Get(self.UnitId);
                unit?.GetComponent<AIComponent>()?.ResumeAI();
            }
        }

        public static void OnTick(this BuffTickComponent self)
        {
            Buff buff = self.GetParent<Buff>();
            if (buff == null)
            {
                return;
            }

            EffectServerBuffTick effect = buff.GetConfig().GetEffect<EffectServerBuffTick>();
            if (effect != null)
            {
                Unit unit = buff.Parent.GetParent<Unit>();
                using BTEnv env = BTEnv.Create(buff.Scene(), unit.Id);
                env.AddEntity(effect.Buff, buff);
                env.AddEntity(effect.Unit, unit);
                BTHelper.RunTree(effect, env);
            }
        }
    }

    public static class BuffTickComponentHelper
    {
        public static void Start(this BuffTickComponent self)
        {
            Buff buff = self.GetParent<Buff>();
            int tickTime = buff.TickTime;
            if (tickTime <= 0)
            {
                return;
            }
            TimerComponent timerComponent = buff.Root().GetComponent<TimerComponent>();
            self.TimerId = timerComponent.NewRepeatedTimer(tickTime, TimerInvokeType.BuffTickTimer, self);
        }

        public static void Stop(this BuffTickComponent self)
        {
            Scene root = self.Root();
            if (root == null)
            {
                return;
            }
            root.GetComponent<TimerComponent>()?.Remove(ref self.TimerId);

            self.CancellationToken?.Cancel();
            self.CancellationToken = null;
            self.Current = 0;
            self.HashCode = 0;
        }
    }
}
