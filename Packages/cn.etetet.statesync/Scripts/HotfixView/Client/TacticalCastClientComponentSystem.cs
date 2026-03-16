using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(TacticalCastClientComponent))]
    public static partial class TacticalCastClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this TacticalCastClientComponent self)
        {
            self.ResetState();
        }

        [EntitySystem]
        private static void Destroy(this TacticalCastClientComponent self)
        {
            self.ResetState();
        }

        public static void BeginCast(this TacticalCastClientComponent self, long itemId, int itemConfigId, TacticalItemConfig config)
        {
            self.IsCasting = true;
            self.ItemId = itemId;
            self.ItemConfigId = itemConfigId;
            self.TacticalConfigId = config?.Id ?? 0;
            self.CastMode = config != null ? config.GetTacticalCastMode() : TacticalCastMode.Self;
            self.PendingTargetPosition = float3.zero;
        }

        public static void CompleteCast(this TacticalCastClientComponent self, float3 targetPosition)
        {
            self.PendingTargetPosition = targetPosition;
            self.IsCasting = false;
        }

        public static void CancelCast(this TacticalCastClientComponent self)
        {
            self.ResetState();
        }

        private static void ResetState(this TacticalCastClientComponent self)
        {
            self.IsCasting = false;
            self.ItemId = 0;
            self.ItemConfigId = 0;
            self.TacticalConfigId = 0;
            self.CastMode = TacticalCastMode.Self;
            self.PendingTargetPosition = float3.zero;
        }
    }
}
