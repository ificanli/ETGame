using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(PlayerEvacuationComponent))]
    [FriendOf(typeof(PlayerEvacuationComponent))]
    public static partial class PlayerEvacuationComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerEvacuationComponent self, long evacuationPointId, long requiredTime)
        {
            self.EvacuationPointId = evacuationPointId;
            self.RequiredTime = requiredTime;
            self.StartTime = TimeInfo.Instance.ServerNow();
            self.Status = 1;

            self.TimerId = self.Root().TimerComponent.NewFrameTimer(TimerInvokeType.PlayerEvacuationTimer, self);

            Unit player = self.GetParent<Unit>();
            Log.Info($"[PlayerEvacuation] Player {player.Id} started evacuation, time: {requiredTime}ms");
        }

        [EntitySystem]
        private static void Destroy(this PlayerEvacuationComponent self)
        {
            if (self.TimerId != 0)
            {
                long timerId = self.TimerId;
                self.Root().TimerComponent.Remove(ref timerId);
                self.TimerId = 0;
            }
        }

        public static void Update(this PlayerEvacuationComponent self)
        {
            if (self.Status != 1) return;

            Unit player = self.GetParent<Unit>();
            if (player == null || player.IsDisposed)
            {
                self.Dispose();
                return;
            }

            if (!self.IsPlayerInRange(player))
            {
                self.CancelEvacuation();
                return;
            }

            long now = TimeInfo.Instance.ServerNow();
            long elapsed = now - self.StartTime;

            if (elapsed >= self.RequiredTime)
            {
                self.CompleteEvacuation().Coroutine();
            }
        }

        private static bool IsPlayerInRange(this PlayerEvacuationComponent self, Unit player)
        {
            Scene scene = player.Scene();
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null) return false;

            Unit ecaUnit = unitComponent.Get(self.EvacuationPointId);
            if (ecaUnit == null) return false;

            ECAPointComponent ecaPoint = ecaUnit.GetComponent<ECAPointComponent>();
            if (ecaPoint == null) return false;

            float distance = math.distance(player.Position, ecaUnit.Position);
            return distance <= ecaPoint.InteractRange;
        }

        private static async ETTask CompleteEvacuation(this PlayerEvacuationComponent self)
        {
            self.Status = 2;

            Unit player = self.GetParent<Unit>();
            Log.Info($"[PlayerEvacuation] Player {player.Id} evacuation completed, transferring to lobby");

            // 撤离完成，传送到 Map1（大厅）
            await TransferHelper.TransferAtFrameFinish(player, "Map1", 0);
        }

        private static void CancelEvacuation(this PlayerEvacuationComponent self)
        {
            self.Status = 3;

            Unit player = self.GetParent<Unit>();
            Log.Info($"[PlayerEvacuation] Player {player.Id} evacuation cancelled (left range)");

            self.Dispose();
        }
    }
}
