using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 确认起装（Gate 服务器处理）
    /// 验证英雄ID和装备配置，将起装数据存入 Player.LoadoutComponent
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ConfirmLoadoutHandler : MessageSessionHandler<C2G_ConfirmLoadout, G2C_ConfirmLoadout>
    {
        protected override async ETTask Run(Session session, C2G_ConfirmLoadout request, G2C_ConfirmLoadout response)
        {
            // 从 Session 找到对应的 Player
            SessionPlayerComponent sessionPlayer = session.GetComponent<SessionPlayerComponent>();
            if (sessionPlayer?.Player == null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                return;
            }

            Player player = sessionPlayer.Player;
            PlayerStorageComponent storage = player.GetComponent<PlayerStorageComponent>() ?? player.AddComponent<PlayerStorageComponent>();
            LoadoutComponent loadout = player.GetComponent<LoadoutComponent>() ?? player.AddComponent<LoadoutComponent>();

            Log.Info($"C2G_ConfirmLoadout: HeroConfigId={request.HeroConfigId}, MainWeapon={request.MainWeaponConfigId}, SubWeapon={request.SubWeaponConfigId}");

            // 验证英雄配置存在
            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(request.HeroConfigId);
            if (heroConfig == null)
            {
                response.Error = ErrorCode.ERR_LoadoutHeroNotFound;
                response.Message = $"hero config not found: {request.HeroConfigId}";
                return;
            }

            // 验证主武器配置与槽位匹配
            if (request.MainWeaponConfigId > 0)
            {
                int validationError = ValidateWeaponSlot(request.MainWeaponConfigId);
                if (validationError != ErrorCode.ERR_Success)
                {
                    response.Error = validationError;
                    response.Message = validationError == ErrorCode.ERR_LoadoutItemNotFound
                            ? $"main weapon config not found: {request.MainWeaponConfigId}"
                            : $"main weapon slot mismatch: {request.MainWeaponConfigId}";
                    return;
                }
            }

            // 验证副武器配置与槽位匹配
            if (request.SubWeaponConfigId > 0)
            {
                int validationError = ValidateWeaponSlot(request.SubWeaponConfigId);
                if (validationError != ErrorCode.ERR_Success)
                {
                    response.Error = validationError;
                    response.Message = validationError == ErrorCode.ERR_LoadoutItemNotFound
                            ? $"sub weapon config not found: {request.SubWeaponConfigId}"
                            : $"sub weapon slot mismatch: {request.SubWeaponConfigId}";
                    return;
                }
            }

            // 验证护甲配置与槽位匹配（Chest）
            if (request.ArmorConfigId > 0)
            {
                int validationError = ValidateEquipSlot(request.ArmorConfigId, (int)EquipmentSlotType.Chest);
                if (validationError != ErrorCode.ERR_Success)
                {
                    response.Error = validationError;
                    response.Message = validationError == ErrorCode.ERR_LoadoutItemNotFound
                            ? $"armor config not found: {request.ArmorConfigId}"
                            : $"armor slot mismatch: {request.ArmorConfigId}";
                    return;
                }
            }

            if (request.ConsumableConfigIds.Count > 2)
            {
                response.Error = ErrorCode.ERR_LoadoutSlotMismatch;
                response.Message = $"consumable count overflow: {request.ConsumableConfigIds.Count}";
                return;
            }

            foreach (int consumableConfigId in request.ConsumableConfigIds)
            {
                if (consumableConfigId <= 0)
                {
                    continue;
                }

                ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(consumableConfigId);
                EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(consumableConfigId);
                if (itemConfig == null && equipConfig == null)
                {
                    response.Error = ErrorCode.ERR_LoadoutItemNotFound;
                    response.Message = $"consumable config not found: {consumableConfigId}";
                    return;
                }
            }

            if (!TryCommitWarehouseLoadout(storage, loadout, request, out int storageError, out string storageMessage))
            {
                response.Error = storageError;
                response.Message = storageMessage;
                return;
            }

            // 写入 LoadoutComponent
            loadout.HeroConfigId = request.HeroConfigId;
            loadout.MainWeaponConfigId = request.MainWeaponConfigId;
            loadout.SubWeaponConfigId = request.SubWeaponConfigId;
            loadout.ArmorConfigId = request.ArmorConfigId;
            loadout.ConsumableConfigIds.Clear();
            if (request.ConsumableConfigIds != null)
            {
                loadout.ConsumableConfigIds.AddRange(request.ConsumableConfigIds);
            }
            loadout.IsConfirmed = true;

            // 立即通知 Unit 应用起装（Unit 在 Home 地图，通过 Location 消息发送）
            MessageLocationSenderComponent locationSenderComp = player.Scene().GetComponent<MessageLocationSenderComponent>();
            long playerId = player.Id;
            if (locationSenderComp != null)
            {
                MessageLocationSenderOneType locationSender = locationSenderComp.Get(LocationType.Unit);
                A2Map_ApplyLoadoutRequest applyReq = A2Map_ApplyLoadoutRequest.Create();
                applyReq.HeroConfigId = request.HeroConfigId;
                applyReq.MainWeaponConfigId = request.MainWeaponConfigId;
                applyReq.SubWeaponConfigId = request.SubWeaponConfigId;
                applyReq.ArmorConfigId = request.ArmorConfigId;
                await locationSender.Call(playerId, applyReq);
                Log.Info($"C2G_ConfirmLoadout: applied loadout to unit {playerId}");
            }

            await ETTask.CompletedTask;
        }

        private static bool TryCommitWarehouseLoadout(
            PlayerStorageComponent storage,
            LoadoutComponent currentLoadout,
            C2G_ConfirmLoadout request,
            out int error,
            out string message)
        {
            error = ErrorCode.ERR_Success;
            message = string.Empty;

            bool allowInitialSelection = storage.WarehouseItems.Count == 0 && !HasAnyLoadoutItem(currentLoadout);
            if (allowInitialSelection)
            {
                return true;
            }

            Dictionary<int, int> inventory = new(storage.WarehouseItems);
            AddInventoryItem(inventory, currentLoadout.MainWeaponConfigId, 1);
            AddInventoryItem(inventory, currentLoadout.SubWeaponConfigId, 1);
            AddInventoryItem(inventory, currentLoadout.ArmorConfigId, 1);
            foreach (int configId in currentLoadout.ConsumableConfigIds)
            {
                AddInventoryItem(inventory, configId, 1);
            }

            if (!TryConsumeInventoryItem(inventory, request.MainWeaponConfigId, 1) ||
                !TryConsumeInventoryItem(inventory, request.SubWeaponConfigId, 1) ||
                !TryConsumeInventoryItem(inventory, request.ArmorConfigId, 1))
            {
                error = ErrorCode.ERR_LoadoutItemNotFound;
                message = "equip item not found in warehouse";
                return false;
            }

            foreach (int configId in request.ConsumableConfigIds)
            {
                if (!TryConsumeInventoryItem(inventory, configId, 1))
                {
                    error = ErrorCode.ERR_LoadoutItemNotFound;
                    message = $"consumable not found in warehouse: {configId}";
                    return false;
                }
            }

            storage.WarehouseItems.Clear();
            foreach (var kv in inventory)
            {
                if (kv.Value > 0)
                {
                    storage.WarehouseItems[kv.Key] = kv.Value;
                }
            }

            return true;
        }

        private static bool HasAnyLoadoutItem(LoadoutComponent loadout)
        {
            return loadout.HeroConfigId > 0 ||
                    loadout.MainWeaponConfigId > 0 ||
                    loadout.SubWeaponConfigId > 0 ||
                    loadout.ArmorConfigId > 0 ||
                    loadout.ConsumableConfigIds.Count > 0;
        }

        private static void AddInventoryItem(Dictionary<int, int> inventory, int configId, int count)
        {
            if (configId <= 0 || count <= 0)
            {
                return;
            }

            if (inventory.TryGetValue(configId, out int current))
            {
                inventory[configId] = current + count;
            }
            else
            {
                inventory[configId] = count;
            }
        }

        private static bool TryConsumeInventoryItem(Dictionary<int, int> inventory, int configId, int count)
        {
            if (configId <= 0 || count <= 0)
            {
                return true;
            }

            if (!inventory.TryGetValue(configId, out int current) || current < count)
            {
                return false;
            }

            current -= count;
            if (current > 0)
            {
                inventory[configId] = current;
            }
            else
            {
                inventory.Remove(configId);
            }

            return true;
        }

        /// <summary>
        /// 验证武器配置ID是否可用：
        /// 优先兼容 ItemConfig（当前起装流程常用），
        /// 若命中 EquipmentConfig 则按槽位做严格校验。
        /// </summary>
        private static int ValidateWeaponSlot(int configId)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                return ErrorCode.ERR_Success;
            }

            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            if (equipConfig == null)
            {
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            if (equipConfig.EquipSlot != (int)EquipmentSlotType.MainHand)
            {
                return ErrorCode.ERR_LoadoutSlotMismatch;
            }

            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 验证配置ID是否可用：
        /// 优先兼容 ItemConfig（当前起装流程常用），
        /// 若命中 EquipmentConfig 则按 expectedSlot 做严格校验。
        /// </summary>
        private static int ValidateEquipSlot(int configId, int expectedSlot)
        {
            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(configId);
            if (itemConfig != null)
            {
                return ErrorCode.ERR_Success;
            }

            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            if (equipConfig == null)
            {
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            if (equipConfig.EquipSlot != expectedSlot)
            {
                return ErrorCode.ERR_LoadoutSlotMismatch;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
