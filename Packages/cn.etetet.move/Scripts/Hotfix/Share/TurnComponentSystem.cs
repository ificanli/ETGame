using System;
using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(TurnComponent))]
    public static partial class TurnComponentSystem
    {
        [Invoke(TimerInvokeType.TurnTimer)]
        public class TurnTimer: ATimer<TurnComponent>
        {
            protected override void Run(TurnComponent self)
            {
                try
                {
                    self.UpdateTurn();
                }
                catch (Exception e)
                {
                    Log.Error($"turn timer error: {self.Id}\n{e}");
                }
            }
        }
        
        
        [EntitySystem]
        private static void Awake(this TurnComponent self)
        {
        }

        private static void UpdateTurn(this TurnComponent self)
        {
            long runTime = TimeInfo.Instance.ServerNow() - self.StartTime;
            self.GetParent<Unit>().Rotation = math.slerp(self.From, self.To, runTime * 1f / self.TurnTime);
            if (runTime < self.TurnTime)
            {
                return;
            }

            self.GetParent<Unit>().Rotation = self.To;
            self.Stop();
        }
        
        public static void Turn(this TurnComponent self, quaternion to, int turnTime)
        {
            self.StartTime = TimeInfo.Instance.ServerNow();
            self.From = self.GetParent<Unit>().Rotation;
            self.To = to;
            self.TurnTime = turnTime;
            TimerComponent timerComponent = self.Root().GetComponent<TimerComponent>();
            timerComponent.Remove(ref self.timer);
            self.timer = timerComponent.NewFrameTimer(TimerInvokeType.TurnTimer, self);
        }

        public static void Stop(this TurnComponent self)
        {
            self.Root().GetComponent<TimerComponent>().Remove(ref self.timer);
        }
    }
}