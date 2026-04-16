namespace ET.Server
{
    public static class WeaponPickupHelper
    {
        /// <summary>
        /// 拾取结果
        /// </summary>
        public const int PickupResultEquipped = 1;
        public const int PickupResultToBag = 2;

        /// <summary>
        /// 一键拾取地面物品。武器且有空槽→装备，否则→放背包。
        /// </summary>
        public static async ETTask<(int Error, int PickupResult)> TryPickupGroundItemAsync(Unit unit, string pointId)
        {
            if (unit == null || unit.IsDisposed || string.IsNullOrWhiteSpace(pointId))
            {
                return (ErrorCode.ERR_Cancel, 0);
            }

            if (!ContainerRuntimeHelper.TryGetPoint(unit.Scene(), pointId, out ECAPointComponent point))
            {
                return (ErrorCode.ERR_ECAPointNotFound, 0);
            }

            if (!ContainerRuntimeHelper.IsPlayerInRange(point, unit))
            {
                return (ErrorCode.ERR_ECAInteractOutOfRange, 0);
            }

            Unit pointUnit = point.GetParent<Unit>();
            if (pointUnit == null)
            {
                return (ErrorCode.ERR_ECAPointNotFound, 0);
            }

            ContainerComponent container = pointUnit.GetComponent<ContainerComponent>();
            if (container == null)
            {
                return (ErrorCode.ERR_Cancel, 0);
            }

            // GroundDrop 默认 Closed，允许直接拾取
            if (container.State != ContainerState.Opened &&
                container.State != ContainerState.Closed)
            {
                return (ErrorCode.ERR_Cancel, 0);
            }

            // 取第一个物品
            int firstSlot = -1;
            ContainerItemEntry firstItem = default;
            foreach (var kv in container.ItemEntries)
            {
                if (container.TryGetItem(kv.Key, out ContainerItemEntry entry))
                {
                    firstSlot = kv.Key;
                    firstItem = entry;
                    break;
                }
            }

            if (firstSlot < 0 || firstItem.ConfigId <= 0)
            {
                return (ErrorCode.ERR_Cancel, 0);
            }

            int configId = firstItem.ConfigId;
            int count = firstItem.Count;
            int pickupResult;

            // 检查是否为武器且有空武器槽
            WeaponConfig weaponConfig = WeaponConfigCategory.Instance.GetOrDefault(configId);
            bool isWeapon = weaponConfig != null;

            if (isWeapon && TryEquipWeaponToEmptySlot(unit, configId))
            {
                pickupResult = PickupResultEquipped;
                container.RemoveItem(firstSlot);
                CleanupEmptyContainer(unit.Scene(), point, container);
            }
            else
            {
                // 走现有 TakeAll 流程（自动放背包、清理容器）
                // 先临时设置容器为 Opened 状态以满足 TakeAll 的前置检查
                int prevState = container.State;
                container.State = ContainerState.Opened;

                EntityRef<Unit> unitRef = unit;
                EntityRef<ContainerComponent> containerRef = container;
                int takeError = await ContainerRuntimeHelper.TakeAll(unit, point);
                unit = unitRef;
                container = containerRef;
                if (takeError != ErrorCode.ERR_Success && takeError != ErrorCode.ERR_ECAContainerBagFull)
                {
                    if (container != null && !container.IsDisposed)
                    {
                        container.State = prevState;
                    }
                    return (takeError, 0);
                }

                pickupResult = PickupResultToBag;
            }

            if (unit == null || unit.IsDisposed)
            {
                return (ErrorCode.ERR_Success, pickupResult);
            }

            Log.Info($"[WeaponPickup] unit={unit.Id}, pointId={pointId}, configId={configId}, result={pickupResult}");

            return (ErrorCode.ERR_Success, pickupResult);
        }

        /// <summary>
        /// 清理空容器（GroundDrop拾取后销毁交互点）
        /// </summary>
        private static void CleanupEmptyContainer(Scene scene, ECAPointComponent point, ContainerComponent container)
        {
            if (container.HasAnyItem())
            {
                return;
            }

            container.State = ContainerState.Empty;
            ECAPointStateHelper.SetState(point, container.State);

            // GroundDrop 清空后销毁交互点
            if (container.OutputMode == ContainerOutputMode.GroundDrop)
            {
                Unit pointUnit = point.GetParent<Unit>();
                if (pointUnit != null)
                {
                    ECAManagerComponent ecaManager = scene?.GetComponent<ECAManagerComponent>();
                    ecaManager?.RemoveECAPoint(point.PointId);
                    pointUnit.Dispose();
                }
            }
        }

        /// <summary>
        /// 尝试将武器装备到空武器槽
        /// </summary>
        private static bool TryEquipWeaponToEmptySlot(Unit unit, int weaponConfigId)
        {
            WeaponComponent weaponComp = unit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                return false;
            }

            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>();

            int targetSlot = 0;
            EquipmentSlotType targetSlotType = EquipmentSlotType.MainHand;

            if (weaponComp.Slot1WeaponId == 0)
            {
                targetSlot = 1;
                targetSlotType = EquipmentSlotType.MainHand;
            }
            else if (weaponComp.Slot2WeaponId == 0)
            {
                targetSlot = 2;
                targetSlotType = EquipmentSlotType.OffHand;
            }
            else
            {
                return false;
            }

            // 创建 Item 并装备
            if (equipComp != null)
            {
                Item weaponItem = equipComp.AddChild<Item>();
                weaponItem.ConfigId = weaponConfigId;
                weaponItem.Count = 1;
                equipComp.EquipItem(weaponItem, targetSlotType);
            }

            // 更新 WeaponComponent
            WeaponConfig config = WeaponConfigCategory.Instance.Get(weaponConfigId);
            if (targetSlot == 1)
            {
                weaponComp.Slot1WeaponId = weaponConfigId;
                weaponComp.Slot1Ammo = config?.MagazineSize ?? 0;
                weaponComp.Slot1Reloading = false;
                weaponComp.SetEffectiveStats(1,
                    config?.AttackRange ?? 0f,
                    config?.AttackIntervalMs ?? 0,
                    config?.MagazineSize ?? 0,
                    config?.ReloadTimeMs ?? 0,
                    config?.Damage ?? 0f,
                    0);
            }
            else
            {
                weaponComp.Slot2WeaponId = weaponConfigId;
                weaponComp.Slot2Ammo = config?.MagazineSize ?? 0;
                weaponComp.Slot2Reloading = false;
                weaponComp.SetEffectiveStats(2,
                    config?.AttackRange ?? 0f,
                    config?.AttackIntervalMs ?? 0,
                    config?.MagazineSize ?? 0,
                    config?.ReloadTimeMs ?? 0,
                    config?.Damage ?? 0f,
                    0);
            }

            if (weaponComp.CurrentSlot == 0)
            {
                weaponComp.CurrentSlot = targetSlot;
            }

            WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit);

            // 广播武器切换
            M2C_SwitchWeapon switchMsg = M2C_SwitchWeapon.Create();
            switchMsg.UnitId = unit.Id;
            switchMsg.SlotIndex = targetSlot;
            switchMsg.WeaponId = weaponConfigId;
            MapMessageHelper.NoticeClient(unit, switchMsg, NoticeType.Broadcast);

            // 广播弹药状态
            M2C_WeaponAmmoState ammoMsg = M2C_WeaponAmmoState.Create();
            ammoMsg.UnitId = unit.Id;
            ammoMsg.Slot1Ammo = weaponComp.Slot1Ammo;
            ammoMsg.Slot2Ammo = weaponComp.Slot2Ammo;
            ammoMsg.Slot1Reloading = weaponComp.Slot1Reloading;
            ammoMsg.Slot2Reloading = weaponComp.Slot2Reloading;
            ammoMsg.Slot1MagazineSize = weaponComp.GetEffectiveMagazineSize(1);
            ammoMsg.Slot2MagazineSize = weaponComp.GetEffectiveMagazineSize(2);
            ammoMsg.Slot1AttackRange = weaponComp.GetEffectiveAttackRange(1);
            ammoMsg.Slot2AttackRange = weaponComp.GetEffectiveAttackRange(2);
            MapMessageHelper.NoticeClient(unit, ammoMsg, NoticeType.Broadcast);

            return true;
        }
    }
}
