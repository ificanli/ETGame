using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>
    /// 战术视野辅助工具。
    /// 负责根据眼和侦查效果，刷新地图层的额外观察关系。
    /// </summary>
    public static class TacticalVisionHelper
    {
        public static void Refresh(TacticalVisionComponent self)
        {
            Scene scene = self.GetParent<Scene>();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            ExtraUnitVisibilityComponent extraVisibility = scene?.GetComponent<ExtraUnitVisibilityComponent>();
            if (unitComponent == null || extraVisibility == null)
            {
                return;
            }

            extraVisibility.CleanupStaleRelations();

            List<Unit> players = new List<Unit>();
            List<Unit> wards = new List<Unit>();
            List<Unit> units = new List<Unit>();

            foreach (Unit unit in unitComponent.Children.Values)
            {
                if (unit == null || unit.IsDisposed)
                {
                    continue;
                }

                units.Add(unit);

                if (unit.UnitType == UnitType.Player)
                {
                    players.Add(unit);
                }

                if (unit.GetComponent<WardComponent>() != null)
                {
                    wards.Add(unit);
                }
            }

            HashSet<long> activeViewerIds = new HashSet<long>();
            foreach (Unit player in players)
            {
                activeViewerIds.Add(player.Id);

                HashSet<long> targetIds = BuildTargets(player, players, wards, units, extraVisibility);
                extraVisibility.ResetViewerTargets(player, targetIds);
            }

            extraVisibility.ClearInactiveViewers(activeViewerIds);
        }

        private static HashSet<long> BuildTargets(
            Unit player,
            List<Unit> players,
            List<Unit> wards,
            List<Unit> units,
            ExtraUnitVisibilityComponent extraVisibility)
        {
            HashSet<long> targetIds = new HashSet<long>();
            CampComponent playerCamp = player.GetComponent<CampComponent>();
            if (playerCamp == null)
            {
                return targetIds;
            }

            AppendFriendlyPlayers(player, players, playerCamp.CampId, targetIds);
            AppendSharedAoiTargets(player, players, playerCamp.CampId, extraVisibility, targetIds);

            foreach (Unit ward in wards)
            {
                WardComponent wardComponent = ward.GetComponent<WardComponent>();
                if (wardComponent == null || wardComponent.IsDisposed)
                {
                    continue;
                }

                if (wardComponent.CampId == playerCamp.CampId)
                {
                    targetIds.Add(ward.Id);
                    AppendEnemyUnitsInWardRange(player, ward, wardComponent, units, targetIds);
                    continue;
                }

                if (IsWardDetectedByCamp(players, playerCamp.CampId, ward))
                {
                    targetIds.Add(ward.Id);
                }
            }

            return targetIds;
        }

        private static void AppendFriendlyPlayers(Unit player, List<Unit> players, int campId, HashSet<long> targetIds)
        {
            foreach (Unit otherPlayer in players)
            {
                if (otherPlayer == null || otherPlayer.IsDisposed || otherPlayer.Id == player.Id)
                {
                    continue;
                }

                CampComponent otherCamp = otherPlayer.GetComponent<CampComponent>();
                if (otherCamp == null || otherCamp.CampId != campId)
                {
                    continue;
                }

                targetIds.Add(otherPlayer.Id);
            }
        }

        private static void AppendSharedAoiTargets(
            Unit player,
            List<Unit> players,
            int campId,
            ExtraUnitVisibilityComponent extraVisibility,
            HashSet<long> targetIds)
        {
            foreach (Unit sourcePlayer in players)
            {
                if (sourcePlayer == null || sourcePlayer.IsDisposed)
                {
                    continue;
                }

                CampComponent sourceCamp = sourcePlayer.GetComponent<CampComponent>();
                if (sourceCamp == null || sourceCamp.CampId != campId)
                {
                    continue;
                }

                AOIEntity sourceAoi = sourcePlayer.GetComponent<AOIEntity>();
                if (sourceAoi == null || sourceAoi.IsDisposed)
                {
                    continue;
                }

                foreach (AOIEntity targetAoi in sourceAoi.GetSeeUnits().Values)
                {
                    if (targetAoi == null || targetAoi.IsDisposed)
                    {
                        continue;
                    }

                    Unit target = targetAoi.Unit;
                    if (target == null || target.IsDisposed || target.Id == player.Id)
                    {
                        continue;
                    }

                    if (target.GetComponent<WardComponent>() != null)
                    {
                        continue;
                    }

                    if (target.UnitType == UnitType.Player && CampHelper.IsFriendly(player, target))
                    {
                        continue;
                    }

                    if (extraVisibility != null && extraVisibility.ShouldConcealTargetFromViewer(sourcePlayer, target))
                    {
                        continue;
                    }

                    targetIds.Add(target.Id);
                }
            }
        }

        private static bool IsWardDetectedByCamp(List<Unit> players, int campId, Unit ward)
        {
            foreach (Unit player in players)
            {
                if (player == null || player.IsDisposed)
                {
                    continue;
                }

                CampComponent playerCamp = player.GetComponent<CampComponent>();
                if (playerCamp == null || playerCamp.CampId != campId)
                {
                    continue;
                }

                float detectorRadius = GetDetectorRadius(player);
                if (detectorRadius <= 0f)
                {
                    continue;
                }

                if (math.distance(player.Position, ward.Position) <= detectorRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendEnemyUnitsInWardRange(Unit player, Unit ward, WardComponent wardComponent, List<Unit> units, HashSet<long> targetIds)
        {
            foreach (Unit target in units)
            {
                if (target == null || target.IsDisposed)
                {
                    continue;
                }

                if (target.Id == player.Id || target.Id == ward.Id)
                {
                    continue;
                }

                if (target.GetComponent<WardComponent>() != null)
                {
                    continue;
                }

                if (!CampHelper.IsEnemy(player, target))
                {
                    continue;
                }

                if (math.distance(ward.Position, target.Position) > wardComponent.VisionRadius)
                {
                    continue;
                }

                targetIds.Add(target.Id);
            }
        }

        private static float GetDetectorRadius(Unit player)
        {
            BuffComponent buffComponent = player.GetComponent<BuffComponent>();
            if (buffComponent == null)
            {
                return 0f;
            }

            float maxRadius = 0f;
            foreach (Buff buff in buffComponent.Children.Values)
            {
                if (buff == null || buff.IsDisposed)
                {
                    continue;
                }

                DetectorComponent detectorComponent = buff.GetComponent<DetectorComponent>();
                if (detectorComponent == null || detectorComponent.IsDisposed)
                {
                    continue;
                }

                if (detectorComponent.RevealRadius > maxRadius)
                {
                    maxRadius = detectorComponent.RevealRadius;
                }
            }

            return maxRadius;
        }
    }
}
