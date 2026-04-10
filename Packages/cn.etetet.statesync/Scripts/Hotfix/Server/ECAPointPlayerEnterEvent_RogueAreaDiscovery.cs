using System;

namespace ET.Server
{
    [Event(SceneType.Map)]
    public class ECAPointPlayerEnterEvent_RogueAreaDiscovery : AEvent<Scene, ECAPointPlayerEnterEvent>
    {
        protected override async ETTask Run(Scene scene, ECAPointPlayerEnterEvent args)
        {
            ECAPointComponent point = args.Point;
            Unit player = args.Player;
            RogueAreaDiscoveryPointHelper.TryDiscover(point, player);
            await ETTask.CompletedTask;
        }
    }

    public static class RogueAreaDiscoveryPointHelper
    {
        public static void TryDiscover(ECAPointComponent point, Unit player)
        {
            if (!TryResolveAreaId(point, out int areaId))
            {
                return;
            }

            if (player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return;
            }

            if (RogueEffectQueryHelper.GetAreaDiscoveryGold(player) <= 0)
            {
                return;
            }

            RogueAreaDiscoveryComponent areaDiscovery = player.GetComponent<RogueAreaDiscoveryComponent>() ??
                    player.AddComponent<RogueAreaDiscoveryComponent>();
            areaDiscovery.OnEnterArea(areaId);
        }

        public static bool TryResolveAreaId(ECAPointComponent point, out int areaId)
        {
            areaId = 0;
            if (point == null || point.IsDisposed)
            {
                return false;
            }

            if (FlowParamHelper.TryGetBoolParam(point.Params, ECAPointParamKey.RogueAreaEnabled, out bool enabled) && !enabled)
            {
                return false;
            }

            if (FlowParamHelper.TryGetIntParam(point.Params, ECAPointParamKey.RogueAreaId, out areaId) && areaId > 0)
            {
                return true;
            }

            if (point.PointType != ECAPointType.RangeTrigger || string.IsNullOrWhiteSpace(point.PointId))
            {
                return false;
            }

            if (!point.PointId.StartsWith("rogue_area_", StringComparison.OrdinalIgnoreCase) &&
                !point.PointId.StartsWith("area_", StringComparison.OrdinalIgnoreCase) &&
                !point.PointId.StartsWith("landmark_", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            long hash = point.PointId.GetLongHashCode();
            areaId = (int)(hash & int.MaxValue);
            return areaId > 0;
        }
    }
}
