using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    public class BTCreateBuffEffectHandler: ABTHandler<BTCreateBuffEffect>
    {
        private const string LINE_REGION_TYPE_NAME = "DTT.AreaOfEffectRegions.LineRegion";

        protected override int Run(BTCreateBuffEffect node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (unit == null || unit.IsDisposed || buff == null || buff.IsDisposed || node.Effect == null || string.IsNullOrEmpty(node.Effect.Name))
            {
                return 1;
            }

            CreateBuffEffectAsync(unit, buff, node).Coroutine();

            return 0;
        }

        private static async ETTask CreateBuffEffectAsync(Unit unit, Buff buff, BTCreateBuffEffect node)
        {
            EntityRef<Buff> buffRef = buff;
            EntityRef<Unit> unitRef = unit;
            GameObject go = await unit.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(node.Effect.Name);

            unit = unitRef;
            buff = buffRef;
            if (go == null || unit == null || unit.IsDisposed || buff == null || buff.IsDisposed)
            {
                return;
            }

            GameObject effect = EffectUnitHelper.Create(unit, node.BindPoint, go, !node.FollowUnit, node.Duration);
            if (effect == null)
            {
                return;
            }

            ApplyEffectOffset(node, effect);
            ConfigureLineRegion(node, buff, effect);

            buff.AddComponent<BuffGameObjectComponent>().GameObjects.Add(effect);
        }

        private static void ApplyEffectOffset(BTCreateBuffEffect node, GameObject effect)
        {
            Vector3 localPositionOffset = new Vector3(node.LocalPositionOffsetX, node.LocalPositionOffsetY, node.LocalPositionOffsetZ);
            Vector3 localEulerAnglesOffset = new Vector3(node.LocalEulerAnglesOffsetX, node.LocalEulerAnglesOffsetY, node.LocalEulerAnglesOffsetZ);
            if (localPositionOffset == Vector3.zero && localEulerAnglesOffset == Vector3.zero)
            {
                return;
            }

            Transform effectTransform = effect.transform;
            if (node.FollowUnit)
            {
                effectTransform.localPosition += localPositionOffset;
                effectTransform.localRotation *= Quaternion.Euler(localEulerAnglesOffset);
                return;
            }

            Quaternion baseRotation = effectTransform.rotation;
            effectTransform.position += baseRotation * localPositionOffset;
            effectTransform.rotation = baseRotation * Quaternion.Euler(localEulerAnglesOffset);
        }

        private static void ConfigureLineRegion(BTCreateBuffEffect node, Buff buff, GameObject effect)
        {
            Component lineRegion = FindLineRegionComponent(effect);
            if (lineRegion == null)
            {
                return;
            }

            if (node.SyncLineRegionLengthFromSpellTarget && TryGetSpellTargetPosition(buff, out float3 targetPosition))
            {
                float lineLength = CalculateLineLength(node, effect.transform.position, targetPosition);
                SetLineRegionFloat(lineRegion, "Length", Mathf.Max(lineLength, 0.01f));
            }

            if (node.AnimateLineRegionFillByDuration)
            {
                LineRegionFillProgressDriver driver = effect.GetComponent<LineRegionFillProgressDriver>();
                if (driver == null)
                {
                    driver = effect.AddComponent<LineRegionFillProgressDriver>();
                }

                driver.Init(lineRegion, node.Duration);
            }
        }

        private static Component FindLineRegionComponent(GameObject effect)
        {
            foreach (Component component in effect.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().FullName == LINE_REGION_TYPE_NAME)
                {
                    return component;
                }
            }

            return null;
        }

        private static void SetLineRegionFloat(Component lineRegion, string propertyName, float value)
        {
            lineRegion.GetType().GetProperty(propertyName)?.SetValue(lineRegion, value);
        }

        private static float CalculateLineLength(BTCreateBuffEffect node, Vector3 effectPosition, float3 targetPosition)
        {
            float3 start = effectPosition;
            start.y = 0f;
            targetPosition.y = 0f;

            float rawDistance = math.distance(start, targetPosition);
            if (node.LineRegionMaxLength > 0f)
            {
                rawDistance = math.min(rawDistance, node.LineRegionMaxLength);
            }

            return math.max(0f, rawDistance - math.max(0f, node.LineRegionLengthOffset));
        }

        private static bool TryGetSpellTargetPosition(Buff buff, out float3 targetPosition)
        {
            Buff parentBuff = buff.GetParentBuff();
            if (parentBuff != null && !parentBuff.IsDisposed)
            {
                SpellTargetComponent parentSpellTargetComponent = parentBuff.GetBuffData().GetComponent<SpellTargetComponent>();
                if (parentSpellTargetComponent != null)
                {
                    targetPosition = parentSpellTargetComponent.Position;
                    return true;
                }
            }

            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
            if (spellTargetComponent != null)
            {
                targetPosition = spellTargetComponent.Position;
                return true;
            }

            targetPosition = float3.zero;
            return false;
        }
    }
}
