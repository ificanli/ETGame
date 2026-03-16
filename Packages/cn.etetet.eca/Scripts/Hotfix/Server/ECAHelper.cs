using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public static class ECAHelper
    {
        public static float GetHorizontalDistance(float3 a, float3 b)
        {
            float2 delta = new float2(a.x - b.x, a.z - b.z);
            return math.length(delta);
        }

        /// <summary>
        /// 检查玩家是否在 ECA 点范围内
        /// </summary>
        public static void CheckPlayerInRange(Unit player)
        {
            if (player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return;
            }

            Scene scene = player.Scene();
            ECAManagerComponent ecaManager = scene.GetComponent<ECAManagerComponent>();
            if (ecaManager == null) return;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            List<string> stalePointIds = null;
            foreach (KeyValuePair<string, long> kvp in ecaManager.ECAPoints)
            {
                Unit ecaUnit = unitComponent.Get(kvp.Value);
                ECAPointComponent ecaPoint = ecaUnit?.GetComponent<ECAPointComponent>();
                if (ecaPoint == null || ecaPoint.IsDisposed)
                {
                    stalePointIds ??= new List<string>();
                    stalePointIds.Add(kvp.Key);
                    continue;
                }

                if (ecaUnit == null) continue;

                // 交互点只比较水平距离，避免地形高低差导致人在圈内却判定失败
                float distance = GetHorizontalDistance(player.Position, ecaUnit.Position);
                float interactRange = ecaPoint.InteractRange + ECAInteractionModifierHelper.GetInteractRangeBonus(player);
                if (interactRange < 0f)
                {
                    interactRange = 0f;
                }

                bool inRange = distance <= interactRange;
                bool wasInRange = ecaPoint.PlayersInRange.Contains(player.Id);

                if (inRange && !wasInRange)
                {
                    Log.Info(
                        $"[ECADebug][RangeState] player={player.Id}, point={ecaPoint.PointId}, enter=true, distance={distance:F2}, range={interactRange:F2}");
                    // 玩家进入范围
                    ecaPoint.OnPlayerEnter(player).Coroutine();
                }
                else if (!inRange && wasInRange)
                {
                    Log.Info(
                        $"[ECADebug][RangeState] player={player.Id}, point={ecaPoint.PointId}, enter=false, distance={distance:F2}, range={interactRange:F2}");
                    // 玩家离开范围
                    ecaPoint.OnPlayerLeave(player);
                }
            }

            if (stalePointIds == null)
            {
                return;
            }

            foreach (string stalePointId in stalePointIds)
            {
                ecaManager.ECAPoints.Remove(stalePointId);
            }
        }
    }
}
