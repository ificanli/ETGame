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

            // 验证英雄配置存在
            HeroConfig heroConfig = HeroConfigCategory.Instance.GetOrDefault(request.HeroConfigId);
            if (heroConfig == null)
            {
                response.Error = ErrorCode.ERR_LoadoutHeroNotFound;
                response.Message = $"hero config not found: {request.HeroConfigId}";
                return;
            }

            // 验证主武器配置且槽位匹配（EquipSlot 必须等于 MainHand=6）
            if (request.MainWeaponConfigId > 0)
            {
                int err = ValidateEquipSlot(request.MainWeaponConfigId, (int)EquipmentSlotType.MainHand);
                if (err != ErrorCode.ERR_Success)
                {
                    response.Error = err;
                    response.Message = $"main weapon slot mismatch, configId={request.MainWeaponConfigId}";
                    return;
                }
            }

            // 验证副武器配置且槽位匹配（EquipSlot 必须等于 OffHand=7）
            if (request.SubWeaponConfigId > 0)
            {
                int err = ValidateEquipSlot(request.SubWeaponConfigId, (int)EquipmentSlotType.OffHand);
                if (err != ErrorCode.ERR_Success)
                {
                    response.Error = err;
                    response.Message = $"sub weapon slot mismatch, configId={request.SubWeaponConfigId}";
                    return;
                }
            }

            // 验证护甲配置且槽位匹配（EquipSlot 必须等于 Chest=2/Armor=2）
            if (request.ArmorConfigId > 0)
            {
                int err = ValidateEquipSlot(request.ArmorConfigId, (int)EquipmentSlotType.Chest);
                if (err != ErrorCode.ERR_Success)
                {
                    response.Error = err;
                    response.Message = $"armor slot mismatch, configId={request.ArmorConfigId}";
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

            await ETTask.CompletedTask;
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
