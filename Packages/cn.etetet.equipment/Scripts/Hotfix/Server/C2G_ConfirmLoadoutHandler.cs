using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 确认起装（Gate 服务器处理）
    /// 当前阶段只负责写入正式当前携带态并标记可进局，不再在这里整套扣仓库或立即同步 Map 预览 Unit。
    /// </summary>
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_ConfirmLoadoutHandler : MessageSessionHandler<C2G_ConfirmLoadout, G2C_ConfirmLoadout>
    {
        protected override async ETTask Run(Session session, C2G_ConfirmLoadout request, G2C_ConfirmLoadout response)
        {
            SessionPlayerComponent sessionPlayer = session.GetComponent<SessionPlayerComponent>();
            if (sessionPlayer?.Player == null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                return;
            }

            Player player = sessionPlayer.Player;
            EntityRef<Session> sessionRef = session;
            EntityRef<Player> playerRef = player;

            using (await session.Root().CoroutineLockComponent.Wait(CoroutineLockType.Loadout, player.Id))
            {
                session = sessionRef;
                player = playerRef;
                if (session == null || player == null)
                {
                    response.Error = ErrorCode.ERR_ConnectGateKeyError;
                    return;
                }

                LoadoutComponent loadout = player.GetComponent<LoadoutComponent>() ?? player.AddComponent<LoadoutComponent>();

                if (!TryValidateRequest(request, response, out var finalBagItems, out var finalSecureItems))
                {
                    return;
                }

                LoadoutStateHelper.ApplyConfirmedSnapshot(loadout, request, finalBagItems, finalSecureItems);
                PlayerStorageComponent storage = player.GetComponent<PlayerStorageComponent>() ?? player.AddComponent<PlayerStorageComponent>();
                LoadoutOperationHelper.PushStateChanged(player, loadout, storage);
                EventSystem.Instance.Publish(session.Root(), new PlayerLoadoutConfirmed
                {
                    PlayerId = player.Id,
                    HeroConfigId = request.HeroConfigId,
                    MainWeaponConfigId = request.MainWeaponConfigId,
                    SubWeaponConfigId = request.SubWeaponConfigId,
                    ArmorConfigId = request.ArmorConfigId,
                    ConsumableConfigIds = new List<int>(request.ConsumableConfigIds),
                    BackpackConfigId = request.BackpackConfigId,
                    BagWidth = request.BagWidth,
                    BagHeight = request.BagHeight,
                    CarriedBagItems = new List<LoadoutGridItemInfo>(finalBagItems),
                });
                response.Message = "success";
            }

            await ETTask.CompletedTask;
        }

        private static bool TryValidateRequest(
            C2G_ConfirmLoadout request,
            G2C_ConfirmLoadout response,
            out List<LoadoutGridItemInfo> finalBagItems,
            out List<LoadoutGridItemInfo> finalSecureItems)
        {
            finalBagItems = null;
            finalSecureItems = null;

            TryApplyConfiguredBackpackSize(request);

            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(request.HeroConfigId);
            if (heroConfig == null)
            {
                response.Error = ErrorCode.ERR_LoadoutHeroNotFound;
                response.Message = $"hero config not found: {request.HeroConfigId}";
                return false;
            }

            if (request.MainWeaponConfigId > 0)
            {
                int validationError = LoadoutStateHelper.ValidateWeaponSlot(request.MainWeaponConfigId);
                if (validationError != ErrorCode.ERR_Success)
                {
                    response.Error = validationError;
                    response.Message = $"main weapon invalid: {request.MainWeaponConfigId}";
                    return false;
                }
            }

            if (request.SubWeaponConfigId > 0)
            {
                int validationError = LoadoutStateHelper.ValidateWeaponSlot(request.SubWeaponConfigId);
                if (validationError != ErrorCode.ERR_Success)
                {
                    response.Error = validationError;
                    response.Message = $"sub weapon invalid: {request.SubWeaponConfigId}";
                    return false;
                }
            }

            if (request.ArmorConfigId > 0)
            {
                int validationError = LoadoutStateHelper.ValidateEquipSlot(request.ArmorConfigId, (int)EquipmentSlotType.Chest);
                if (validationError != ErrorCode.ERR_Success)
                {
                    response.Error = validationError;
                    response.Message = $"armor invalid: {request.ArmorConfigId}";
                    return false;
                }
            }

            if (request.BackpackConfigId > 0)
            {
                int validationError = LoadoutStateHelper.ValidateBackpackSlot(request.BackpackConfigId);
                if (validationError != ErrorCode.ERR_Success)
                {
                    response.Error = validationError;
                    response.Message = $"backpack invalid: {request.BackpackConfigId}";
                    return false;
                }
            }

            if (request.ConsumableConfigIds.Count > 2)
            {
                response.Error = ErrorCode.ERR_LoadoutSlotMismatch;
                response.Message = $"consumable count overflow: {request.ConsumableConfigIds.Count}";
                return false;
            }

            foreach (int consumableConfigId in request.ConsumableConfigIds)
            {
                int validationError = LoadoutStateHelper.ValidateItemExists(consumableConfigId);
                if (validationError != ErrorCode.ERR_Success)
                {
                    response.Error = validationError;
                    response.Message = $"consumable invalid: {consumableConfigId}";
                    return false;
                }
            }

            if (!LoadoutStateHelper.TryBuildValidatedGridItems(request.FinalBagItems, request.BagWidth, request.BagHeight, out finalBagItems))
            {
                response.Error = ErrorCode.ERR_LoadoutGridInvalid;
                response.Message = "final bag layout invalid";
                return false;
            }

            if (!LoadoutStateHelper.TryBuildValidatedGridItems(request.FinalSecureItems, request.SecureWidth, request.SecureHeight, out finalSecureItems))
            {
                response.Error = ErrorCode.ERR_LoadoutGridInvalid;
                response.Message = "final secure layout invalid";
                return false;
            }

            if (request.BackpackConfigId <= 0 && (request.BagWidth > 0 || request.BagHeight > 0 || finalBagItems.Count > 0))
            {
                response.Error = ErrorCode.ERR_LoadoutStateConflict;
                response.Message = "bag layout exists without backpack";
                return false;
            }

            if (request.BackpackConfigId > 0 && (request.BagWidth <= 0 || request.BagHeight <= 0))
            {
                response.Error = ErrorCode.ERR_LoadoutGridInvalid;
                response.Message = "backpack size invalid";
                return false;
            }

            response.Error = ErrorCode.ERR_Success;
            return true;
        }

        private static void TryApplyConfiguredBackpackSize(C2G_ConfirmLoadout request)
        {
            if (request.BackpackConfigId <= 0)
            {
                return;
            }

            ItemConfig itemConfig = ItemConfigCategory.Instance.GetOrDefault(request.BackpackConfigId);
            if (itemConfig == null)
            {
                return;
            }

            if (itemConfig.BackpackWidth <= 0 || itemConfig.BackpackHeight <= 0)
            {
                return;
            }

            request.BagWidth = itemConfig.BackpackWidth;
            request.BagHeight = itemConfig.BackpackHeight;
        }
    }
}
