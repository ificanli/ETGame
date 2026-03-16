using Unity.Mathematics;

namespace ET.Server
{
    public static class TacticalItemHelper
    {
        public static int ValidateUse(Unit unit, Item item, TacticalItemConfig config, float3 targetPosition)
        {
            if (unit == null || unit.IsDisposed || item == null || item.IsDisposed || config == null)
            {
                return ErrorCode.ERR_TacticalItemConfigMissing;
            }

            if (item.Count <= 0)
            {
                return ErrorCode.ERR_ItemNotEnough;
            }

            if (config.BuffConfigId <= 0)
            {
                return ErrorCode.ERR_TacticalBuffConfigMissing;
            }

            if (BuffConfigCategory.Instance.Get(config.BuffConfigId) == null)
            {
                return ErrorCode.ERR_TacticalBuffConfigMissing;
            }

            TacticalCastMode castMode = config.GetTacticalCastMode();
            if (castMode == TacticalCastMode.Position || castMode == TacticalCastMode.MinimapPoint)
            {
                if (config.MaxDistance > 0)
                {
                    float distanceInMm = math.distance(unit.Position, targetPosition) * 1000f;
                    if (distanceInMm > config.MaxDistance)
                    {
                        return ErrorCode.ERR_TacticalCastOutOfRange;
                    }
                }
            }

            return ErrorCode.ERR_Success;
        }

        public static int Use(Unit unit, Item item, TacticalItemConfig config, float3 targetPosition)
        {
            int error = ValidateUse(unit, item, config, targetPosition);
            if (error != ErrorCode.ERR_Success)
            {
                return error;
            }

            if (config.GetTacticalCastMode() != TacticalCastMode.Self)
            {
                TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
                if (targetComponent == null)
                {
                    return ErrorCode.ERR_TacticalTargetComponentMissing;
                }

                targetComponent.Position = targetPosition;
            }

            BuffHelper.CreateBuff(unit, unit.Id, IdGenerater.Instance.GenerateId(), config.BuffConfigId, null);

            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                return ErrorCode.ERR_ItemUseFailed;
            }

            if (!ItemHelper.RemoveItemById(itemComponent, item.Id, ItemChangeReason.UseItem))
            {
                return ErrorCode.ERR_ItemUseFailed;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
