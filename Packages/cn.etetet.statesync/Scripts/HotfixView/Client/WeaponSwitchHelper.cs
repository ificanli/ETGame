namespace ET.Client
{
    /// <summary>
    /// 武器切换辅助类（客户端）
    /// </summary>
    public static class WeaponSwitchHelper
    {
        /// <summary>
        /// 请求切换武器槽位
        /// </summary>
        public static void SwitchWeapon(Scene root, int slotIndex)
        {
            Scene currentScene = root.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null)
            {
                Log.Error(
                    $"[WeaponSwitchTrace][ClientSend] clientNow={TimeInfo.Instance.ClientNow()}, serverNow={TimeInfo.Instance.ServerNow()}, unitId=0, requestedSlot={slotIndex}, result=my_unit_missing");
                Log.Error("WeaponSwitchHelper: Cannot find my unit");
                return;
            }

            WeaponComponent weaponComp = myUnit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                Log.Warning(
                    $"[WeaponSwitchTrace][ClientSend] clientNow={TimeInfo.Instance.ClientNow()}, serverNow={TimeInfo.Instance.ServerNow()}, unitId={myUnit.Id}, requestedSlot={slotIndex}, result=weapon_component_missing");
                return;
            }

            if (slotIndex != 1 && slotIndex != 2)
            {
                Log.Warning(
                    $"[WeaponSwitchTrace][ClientSend] clientNow={TimeInfo.Instance.ClientNow()}, serverNow={TimeInfo.Instance.ServerNow()}, unitId={myUnit.Id}, requestedSlot={slotIndex}, result=invalid_slot");
                return;
            }

            int currentSlot = weaponComp.CurrentSlot;
            int weaponId = weaponComp.GetWeaponId(slotIndex);
            if (weaponId == 0)
            {
                Log.Warning(
                    $"[WeaponSwitchTrace][ClientSend] clientNow={TimeInfo.Instance.ClientNow()}, serverNow={TimeInfo.Instance.ServerNow()}, unitId={myUnit.Id}, requestedSlot={slotIndex}, currentSlot={currentSlot}, result=slot_empty");
                return;
            }

            if (slotIndex == currentSlot)
            {
                Log.Info(
                    $"[WeaponSwitchTrace][ClientSend] clientNow={TimeInfo.Instance.ClientNow()}, serverNow={TimeInfo.Instance.ServerNow()}, unitId={myUnit.Id}, requestedSlot={slotIndex}, currentSlot={currentSlot}, weaponId={weaponId}, result=already_current_slot");
                return;
            }

            PendingWeaponSyncComponent pending = currentScene?.GetComponent<PendingWeaponSyncComponent>();
            if (pending == null && currentScene != null)
            {
                pending = currentScene.AddComponent<PendingWeaponSyncComponent>();
            }

            bool predicted = pending != null && pending.PredictSwitch(root, myUnit, slotIndex);
            C2M_SwitchWeapon message = C2M_SwitchWeapon.Create();
            message.SlotIndex = slotIndex;

            root.GetComponent<ClientSenderComponent>().Send(message);

            Log.Info(
                $"[WeaponSwitchTrace][ClientSend] clientNow={TimeInfo.Instance.ClientNow()}, serverNow={TimeInfo.Instance.ServerNow()}, unitId={myUnit.Id}, requestedSlot={slotIndex}, currentSlot={currentSlot}, predicted={predicted}, predictedCurrentSlot={weaponComp.CurrentSlot}, slot1WeaponId={weaponComp.Slot1WeaponId}, slot2WeaponId={weaponComp.Slot2WeaponId}, slot1Ammo={weaponComp.Slot1Ammo}, slot2Ammo={weaponComp.Slot2Ammo}");
        }
    }
}
