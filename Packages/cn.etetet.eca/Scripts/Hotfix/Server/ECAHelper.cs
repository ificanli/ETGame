using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public static class ECAHelper
    {
        /// <summary>
        /// 检查玩家是否在 ECA 点范围内
        /// </summary>
        public static void CheckPlayerInRange(Unit player)
        {
            Scene scene = player.Scene();
            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
            if (ecaManager == null) return;

            List<ECAPointComponent> ecaPoints = ecaManager.GetAllECAPoints();

            foreach (ECAPointComponent ecaPoint in ecaPoints)
            {
                Unit ecaUnit = ecaPoint.GetParent<Unit>();
                if (ecaUnit == null) continue;

                // 计算距离
                float distance = math.distance(player.Position, ecaUnit.Position);
                bool inRange = distance <= ecaPoint.InteractRange;
                bool wasInRange = ecaPoint.PlayersInRange.Contains(player.Id);

                if (ecaPoint.PointId == "100")
                {
                    Log.Info(
                        $"[ECADebug][RangeProbe100] player={player.Id}, playerPos=({player.Position.x:F2},{player.Position.y:F2},{player.Position.z:F2}), pointPos=({ecaUnit.Position.x:F2},{ecaUnit.Position.y:F2},{ecaUnit.Position.z:F2}), distance={distance:F2}, range={ecaPoint.InteractRange:F2}, inRange={inRange}, wasInRange={wasInRange}");
                }

                if (inRange && !wasInRange)
                {
                    Log.Info(
                        $"[ECADebug][RangeState] player={player.Id}, point={ecaPoint.PointId}, enter=true, distance={distance:F2}, range={ecaPoint.InteractRange:F2}");
                    // 玩家进入范围
                    ecaPoint.OnPlayerEnter(player).Coroutine();
                }
                else if (!inRange && wasInRange)
                {
                    Log.Info(
                        $"[ECADebug][RangeState] player={player.Id}, point={ecaPoint.PointId}, enter=false, distance={distance:F2}, range={ecaPoint.InteractRange:F2}");
                    // 玩家离开范围
                    ecaPoint.OnPlayerLeave(player);
                }
            }
        }
    }
}
