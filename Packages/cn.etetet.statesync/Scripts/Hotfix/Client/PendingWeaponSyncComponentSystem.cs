namespace ET.Client
{
    [EntitySystemOf(typeof(PendingWeaponSyncComponent))]
    public static partial class PendingWeaponSyncComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PendingWeaponSyncComponent self)
        {
            self.SwitchStates.Clear();
            self.AmmoStates.Clear();
        }

        [EntitySystem]
        private static void Destroy(this PendingWeaponSyncComponent self)
        {
            self.SwitchStates.Clear();
            self.AmmoStates.Clear();
        }

        public static void CacheSwitch(this PendingWeaponSyncComponent self, M2C_SwitchWeapon message)
        {
            self.SwitchStates[message.UnitId] = new PendingWeaponSwitchState
            {
                UnitId = message.UnitId,
                SlotIndex = message.SlotIndex,
                WeaponId = message.WeaponId,
            };
        }

        public static void CacheAmmo(this PendingWeaponSyncComponent self, M2C_WeaponAmmoState message)
        {
            self.AmmoStates[message.UnitId] = new PendingWeaponAmmoState
            {
                UnitId = message.UnitId,
                Slot1Ammo = message.Slot1Ammo,
                Slot2Ammo = message.Slot2Ammo,
                Slot1Reloading = message.Slot1Reloading,
                Slot2Reloading = message.Slot2Reloading,
                Slot1MagazineSize = message.Slot1MagazineSize,
                Slot2MagazineSize = message.Slot2MagazineSize,
                Slot1AttackRange = message.Slot1AttackRange,
                Slot2AttackRange = message.Slot2AttackRange,
            };
        }

        public static bool TryApply(this PendingWeaponSyncComponent self, Scene root, long unitId)
        {
            Scene currentScene = self.GetParent<Scene>();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            Unit unit = unitComponent?.Get(unitId);
            if (unit == null)
            {
                return false;
            }

            return self.TryApply(root, unit);
        }

        public static bool TryApply(this PendingWeaponSyncComponent self, Scene root, Unit unit)
        {
            if (root == null || unit == null || unit.IsDisposed)
            {
                return false;
            }

            long unitId = unit.Id;
            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            bool applied = false;

            if (self.SwitchStates.TryGetValue(unitId, out PendingWeaponSwitchState switchState))
            {
                self.SwitchStates.Remove(unitId);
                weaponComponent = ApplySwitch(root, unit, weaponComponent, switchState);
                applied = true;
            }

            if (self.AmmoStates.TryGetValue(unitId, out PendingWeaponAmmoState ammoState))
            {
                self.AmmoStates.Remove(unitId);
                ApplyAmmo(root, unit, weaponComponent, ammoState);
                applied = true;
            }

            return applied;
        }

        public static bool PredictSwitch(this PendingWeaponSyncComponent self, Scene root, Unit unit, int slotIndex)
        {
            if (root == null || unit == null || unit.IsDisposed)
            {
                return false;
            }

            WeaponComponent weaponComponent = unit.GetComponent<WeaponComponent>();
            if (weaponComponent == null)
            {
                return false;
            }

            int weaponId = weaponComponent.GetWeaponId(slotIndex);
            if (weaponId == 0)
            {
                return false;
            }

            PendingWeaponSwitchState state = new PendingWeaponSwitchState
            {
                UnitId = unit.Id,
                SlotIndex = slotIndex,
                WeaponId = weaponId,
            };

            ApplySwitch(root, unit, weaponComponent, state, "ClientPredictApply");
            return true;
        }

        public static void RemovePending(this PendingWeaponSyncComponent self, long unitId)
        {
            self.SwitchStates.Remove(unitId);
            self.AmmoStates.Remove(unitId);
        }

        private static WeaponComponent ApplySwitch(Scene root, Unit unit, WeaponComponent weaponComponent, PendingWeaponSwitchState state, string traceStage = "ClientApply")
        {
            long clientNow = TimeInfo.Instance.ClientNow();
            if (weaponComponent == null)
            {
                int slot1WeaponId = state.SlotIndex == 1 ? state.WeaponId : 0;
                int slot2WeaponId = state.SlotIndex == 2 ? state.WeaponId : 0;
                weaponComponent = unit.AddComponent<WeaponComponent, int, int>(slot1WeaponId, slot2WeaponId);
                Log.Info($"[WeaponInitTrace][PendingSwitch] created client WeaponComponent, unitId={state.UnitId}, slot1={slot1WeaponId}, slot2={slot2WeaponId}");
            }
            else
            {
                if (state.SlotIndex == 1)
                {
                    weaponComponent.Slot1WeaponId = state.WeaponId;
                }
                else if (state.SlotIndex == 2)
                {
                    weaponComponent.Slot2WeaponId = state.WeaponId;
                }
            }

            weaponComponent.SwitchWeapon(state.SlotIndex);
            Log.Info(
                $"[WeaponSwitchTrace][{traceStage}] clientNow={clientNow}, serverNow={TimeInfo.Instance.ServerNow()}, unitId={state.UnitId}, slot={state.SlotIndex}, weaponId={state.WeaponId}, currentSlot={weaponComponent.CurrentSlot}, slot1WeaponId={weaponComponent.Slot1WeaponId}, slot2WeaponId={weaponComponent.Slot2WeaponId}, slot1Ammo={weaponComponent.Slot1Ammo}, slot2Ammo={weaponComponent.Slot2Ammo}");

            EventSystem.Instance.Publish(root, new EventWeaponSwitched
            {
                Scene = root,
                UnitId = state.UnitId,
                SlotIndex = state.SlotIndex,
                WeaponId = state.WeaponId,
            });

            Log.Info($"[WeaponInitTrace][PendingSwitch] applied unitId={state.UnitId}, slot={state.SlotIndex}, weaponId={state.WeaponId}, currentSlot={weaponComponent.CurrentSlot}, slot1={weaponComponent.Slot1WeaponId}, slot2={weaponComponent.Slot2WeaponId}");
            return weaponComponent;
        }

        private static void ApplyAmmo(Scene root, Unit unit, WeaponComponent weaponComponent, PendingWeaponAmmoState state)
        {
            if (weaponComponent == null)
            {
                weaponComponent = unit.AddComponent<WeaponComponent, int, int>(0, 0);
            }

            // 检测当前槽位是否刚进入换弹状态
            int currentSlot = weaponComponent.CurrentSlot;
            bool wasReloading = currentSlot == 1 ? weaponComponent.Slot1Reloading : weaponComponent.Slot2Reloading;
            bool nowReloading = currentSlot == 1 ? state.Slot1Reloading : state.Slot2Reloading;

            weaponComponent.Slot1Ammo = state.Slot1Ammo;
            weaponComponent.Slot2Ammo = state.Slot2Ammo;
            weaponComponent.Slot1Reloading = state.Slot1Reloading;
            weaponComponent.Slot2Reloading = state.Slot2Reloading;
            weaponComponent.Slot1EffectiveMagazineSize = state.Slot1MagazineSize;
            weaponComponent.Slot2EffectiveMagazineSize = state.Slot2MagazineSize;
            weaponComponent.Slot1EffectiveAttackRange = state.Slot1AttackRange;
            weaponComponent.Slot2EffectiveAttackRange = state.Slot2AttackRange;

            // 换弹状态变化时，发布事件供 HotfixView 层驱动动画
            if (wasReloading != nowReloading)
            {
                EventSystem.Instance.Publish(root, new EventWeaponReloadStateChanged
                {
                    Scene = root,
                    UnitId = state.UnitId,
                    IsReloading = nowReloading,
                });
            }

            EventSystem.Instance.Publish(root, new EventWeaponAmmoChanged
            {
                Scene = root,
                UnitId = state.UnitId,
            });

            Log.Info($"[WeaponInitTrace][PendingAmmo] applied unitId={state.UnitId}, slot1Ammo={state.Slot1Ammo}, slot2Ammo={state.Slot2Ammo}, reloading={nowReloading}");
        }
    }
}
