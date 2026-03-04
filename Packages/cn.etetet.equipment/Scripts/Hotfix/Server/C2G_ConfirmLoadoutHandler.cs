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

            Log.Info($"C2G_ConfirmLoadout: HeroConfigId={request.HeroConfigId}, MainWeapon={request.MainWeaponConfigId}, SubWeapon={request.SubWeaponConfigId}");

            // 验证英雄配置存在
            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(request.HeroConfigId);
            if (heroConfig == null)
            {
                response.Error = ErrorCode.ERR_LoadoutHeroNotFound;
                response.Message = $"hero config not found: {request.HeroConfigId}";
                return;
            }

            // 验证主武器配置存在
            if (request.MainWeaponConfigId > 0)
            {
                EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(request.MainWeaponConfigId);
                if (equipConfig == null)
                {
                    response.Error = ErrorCode.ERR_LoadoutItemNotFound;
                    response.Message = $"main weapon config not found: {request.MainWeaponConfigId}";
                    return;
                }
            }

            // 验证副武器配置存在
            if (request.SubWeaponConfigId > 0)
            {
                EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(request.SubWeaponConfigId);
                if (equipConfig == null)
                {
                    response.Error = ErrorCode.ERR_LoadoutItemNotFound;
                    response.Message = $"sub weapon config not found: {request.SubWeaponConfigId}";
                    return;
                }
            }

            // 验证护甲配置存在
            if (request.ArmorConfigId > 0)
            {
                EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(request.ArmorConfigId);
                if (equipConfig == null)
                {
                    response.Error = ErrorCode.ERR_LoadoutItemNotFound;
                    response.Message = $"armor config not found: {request.ArmorConfigId}";
                    return;
                }
            }

            // 写入 LoadoutComponent
            LoadoutComponent loadout = player.GetComponent<LoadoutComponent>() ?? player.AddComponent<LoadoutComponent>();
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

        /// <summary>
        /// 验证武器配置ID的槽位类型（射击游戏中武器都是 MainHand=6）
        /// </summary>
        private static int ValidateWeaponSlot(int configId)
        {
            EquipmentConfig equipConfig = EquipmentConfigCategory.Instance.GetOrDefault(configId);
            if (equipConfig == null)
            {
                return ErrorCode.ERR_LoadoutItemNotFound;
            }

            // 武器必须是 MainHand=6
            if (equipConfig.EquipSlot != (int)EquipmentSlotType.MainHand)
            {
                return ErrorCode.ERR_LoadoutSlotMismatch;
            }

            return ErrorCode.ERR_Success;
        }

        /// <summary>
        /// 验证装备配置ID的槽位类型是否匹配
        /// </summary>
        private static int ValidateEquipSlot(int configId, int expectedSlot)
        {
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
