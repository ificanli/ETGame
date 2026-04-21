using Unity.Mathematics;

namespace ET.Server
{
    internal static class MonsterCombatCommonHelper
    {
        public static bool TryGetInitialContext(BTEnv env, string buffKey, out Unit unit, out Scene root, out ThreatComponent threatComponent, out float unitRadius)
        {
            unit = null;
            root = null;
            threatComponent = null;
            unitRadius = 0f;

            Buff buff = env.GetEntity<Buff>(buffKey);
            unit = buff?.GetOwner();
            if (unit == null || unit.IsDisposed)
            {
                return false;
            }

            root = unit.Root();
            threatComponent = unit.GetComponent<ThreatComponent>();
            unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            return root != null;
        }

        public static bool TryRefreshTarget(Unit unit, ThreatComponent threatComponent, out Unit target)
        {
            target = null;
            if (unit == null || unit.IsDisposed || threatComponent == null)
            {
                return false;
            }

            ThreatInfo threatInfo = threatComponent.GetMaxThreat();
            target = threatInfo?.Unit;
            if (target == null || target.IsDisposed)
            {
                return false;
            }

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            if (targetComponent == null)
            {
                Log.Warning($"[MonsterAI] target component missing, unitId={unit.Id}, threatCount={threatComponent.GetCount()}");
                return false;
            }

            targetComponent.Unit = target;
            targetComponent.Position = target.Position;

            return true;
        }

        public static bool TryGetSpellConfig(long unitId, int spellId, out SpellConfig spellConfig)
        {
            spellConfig = null;
            if (spellId <= 0 || !SpellConfigCategory.Instance.Contain(spellId))
            {
                Log.Warning($"[MonsterAI] combat spell config missing, unitId={unitId}, spellId={spellId}");
                return false;
            }

            spellConfig = SpellConfigCategory.Instance.Get(spellId);
            return spellConfig != null;
        }

        public static bool TryMaintainCastRange(Unit unit, Unit target, float unitRadius, SpellConfig spellConfig, ETCancellationToken cancellationToken)
        {
            if (unit == null || unit.IsDisposed || target == null || target.IsDisposed || spellConfig == null)
            {
                return false;
            }

            float distance = math.distance(unit.Position, target.Position);
            float targetRadius = target.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            float edgeDistance = distance - targetRadius - unitRadius;
            float maxDistance = ResolveMaxDistance(spellConfig.TargetSelector);
            float minDistance = ResolveMinDistance(spellConfig.TargetSelector);

            if (maxDistance > 0f && edgeDistance > maxDistance)
            {
                unit.FindPathMoveToAsync(target.Position).Coroutine(cancellationToken);
                return true;
            }

            if (minDistance > 0f && edgeDistance < minDistance)
            {
                float3 away = unit.Position - target.Position;
                away.y = 0f;
                float3 normalizedAway = math.normalizesafe(away, new float3(1f, 0f, 0f));
                float retreatDistance = minDistance - edgeDistance + 0.6f;
                unit.FindPathMoveToAsync(unit.Position + normalizedAway * retreatDistance).Coroutine(cancellationToken);
                return true;
            }

            return false;
        }

        public static bool HasCastingSpell(Unit unit)
        {
            SpellComponent spellComponent = unit?.GetComponent<SpellComponent>();
            if (spellComponent == null)
            {
                return false;
            }

            Buff current = spellComponent.Current;
            return current != null && !current.IsDisposed;
        }

        public static void FaceTarget(Unit unit, Unit target)
        {
            if (unit == null || unit.IsDisposed || target == null || target.IsDisposed)
            {
                return;
            }

            float3 direction = target.Position - unit.Position;
            direction.y = 0f;
            if (math.lengthsq(direction) <= 0.0001f)
            {
                return;
            }

            unit.Rotation = quaternion.LookRotationSafe(math.normalize(direction), math.up());
        }

        public static bool TryCast(Unit unit, int spellId)
        {
            if (unit == null || unit.IsDisposed || spellId <= 0)
            {
                return false;
            }

            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            Unit target = targetComponent?.Unit;
            bool hasTarget = target != null && !target.IsDisposed;
            float unitRadius = unit.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f;
            float targetRadius = hasTarget ? (target.NumericComponent?.GetAsFloat(NumericType.Radius) ?? 0f) : 0f;
            float distance = hasTarget ? math.distance(unit.Position, target.Position) : -1f;
            float edgeDistance = hasTarget ? distance - targetRadius - unitRadius : -1f;

            unit.Stop(0);
            int ret = SpellHelper.Cast(unit, spellId);
            if (ret != 0)
            {
                Log.Warning(
                    $"[MonsterAI] cast failed, unitId={unit.Id}, spellId={spellId}, ret={ret}, reason={DescribeSpellCastRet(ret)}, " +
                    $"targetId={(hasTarget ? target.Id : 0)}, targetType={(hasTarget ? target.UnitType.ToString() : "None")}, " +
                    $"targetComponentMissing={(targetComponent == null)}, distance={distance:F3}, edgeDistance={edgeDistance:F3}, " +
                    $"unitPos={unit.Position}, unitForward={unit.Forward}, threatCount={unit.GetComponent<ThreatComponent>()?.GetCount() ?? 0}");
                return false;
            }

            Log.Info(
                $"[MonsterAI] cast success, unitId={unit.Id}, spellId={spellId}, targetId={(hasTarget ? target.Id : 0)}, " +
                $"distance={distance:F3}, edgeDistance={edgeDistance:F3}, unitPos={unit.Position}");
            return true;
        }

        public static bool IsLowHp(Unit unit, int hpPermille)
        {
            NumericComponent numeric = unit?.NumericComponent;
            if (numeric == null)
            {
                return false;
            }

            float maxHp = numeric.GetAsFloat(NumericType.MaxHP);
            if (maxHp <= 0f)
            {
                return false;
            }

            float hp = numeric.GetAsFloat(NumericType.HP);
            return hp * 1000f <= maxHp * hpPermille;
        }

        private static float ResolveMaxDistance(TargetSelector targetSelector)
        {
            if (targetSelector == null || targetSelector.MaxDistance <= 0)
            {
                return 0f;
            }

            return targetSelector is TargetSelectorPosition
                ? targetSelector.MaxDistance
                : targetSelector.MaxDistance / 1000f;
        }

        private static float ResolveMinDistance(TargetSelector targetSelector)
        {
            if (targetSelector == null || targetSelector.MinDistance <= 0)
            {
                return 0f;
            }

            return targetSelector is TargetSelectorPosition
                ? targetSelector.MinDistance
                : targetSelector.MinDistance / 1000f;
        }

        private static string DescribeSpellCastRet(int ret)
        {
            return ret switch
            {
                0 => "OK",
                TextConstDefine.SpellCast_DistanceNotEnought => "DistanceNotEnough",
                TextConstDefine.SpellCast_NotSelectTarget => "NotSelectTarget",
                TextConstDefine.SpellCast_TargetTooFar => "TargetTooFar",
                TextConstDefine.SpellCast_SpellInCD => "SpellInCD",
                TextConstDefine.SpellCast_MPNotEnought => "MPNotEnough",
                TextConstDefine.SpellCast_HPNotEnought => "HPNotEnough",
                TextConstDefine.SpellCast_TargetNotInFrontOfCaster => "TargetNotInFrontOfCaster",
                _ => $"Unknown({ret})",
            };
        }
    }
}
