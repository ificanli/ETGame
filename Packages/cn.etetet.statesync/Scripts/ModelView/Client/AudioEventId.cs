namespace ET.Client
{
    /// <summary>
    /// 音频事件 ID 常量。key 同时用作占位音效的缓存键和未来真实资源的加载路径。
    /// </summary>
    public static class AudioEventId
    {
        // ── BGM ──
        public const string BgmLobby  = "Audio/BGM/bgm_lobby";
        public const string BgmBattle = "Audio/BGM/bgm_battle";
        public const string BgmEvac   = "Audio/BGM/bgm_evac";

        // ── 武器 SFX ──
        public const string SfxShotgunFire         = "Audio/SFX/weapon_shotgun_fire";
        public const string SfxSmgFire             = "Audio/SFX/weapon_smg_fire";
        public const string SfxAutoRifleFire       = "Audio/SFX/weapon_autorifle_fire";
        public const string SfxSniperRifleFire     = "Audio/SFX/weapon_sniperrifle_fire";
        public const string SfxPistolFire          = "Audio/SFX/weapon_pistol_fire";
        public const string SfxGrenadeLauncherFire = "Audio/SFX/weapon_grenadelauncher_fire";
        public const string SfxRayGunFire          = "Audio/SFX/weapon_raygun_fire";
        public const string SfxWeaponReload    = "Audio/SFX/weapon_reload";
        public const string SfxWeaponHit       = "Audio/SFX/weapon_hit";
        public const string SfxWeaponSwitched  = "Audio/SFX/weapon_switched";
        public const string SfxEmptyGun        = "Audio/SFX/weapon_empty";

        // ── 战斗反馈 ──
        public const string SfxPlayerDeath     = "Audio/SFX/player_death";
        public const string SfxEnemyKill       = "Audio/SFX/enemy_kill";
        public const string SfxPlayerHurt      = "Audio/SFX/player_hurt";
        public const string SfxFootstep        = "Audio/SFX/footstep";
        public const string SfxLowHpWarning    = "Audio/SFX/low_hp_warning";

        // ── 肉鸽 ──
        public const string SfxLevelUp         = "Audio/SFX/level_up";
        public const string SfxChoicePopup     = "Audio/SFX/choice_popup";
        public const string SfxChoiceConfirm   = "Audio/SFX/choice_confirm";
        public const string SfxReroll          = "Audio/SFX/reroll";

        // ── UI / 交互 ──
        public const string SfxUiClick         = "Audio/SFX/ui_click";
        public const string SfxContainerOpen   = "Audio/SFX/container_open";
        public const string SfxItemPickup      = "Audio/SFX/item_pickup";
        public const string SfxRareItemPickup  = "Audio/SFX/rare_item_pickup";
        public const string SfxPanelOpen       = "Audio/SFX/panel_open";

        // ── 撤离结算 ──
        public const string SfxEvacActivate    = "Audio/SFX/evac_activate";
        public const string SfxEvacSuccess     = "Audio/SFX/evac_success";
        public const string SfxSettleSuccess   = "Audio/SFX/settle_success";
        public const string SfxSettleFail      = "Audio/SFX/settle_fail";

        /// <summary>
        /// 根据 WeaponTypeId 返回对应的开火音效 key
        /// </summary>
        public static string GetFireSoundByWeaponType(int weaponTypeId)
        {
            return weaponTypeId switch
            {
                (int)WeaponType.Shotgun           => SfxShotgunFire,
                (int)WeaponType.SMG               => SfxSmgFire,
                (int)WeaponType.AutoRifle         => SfxAutoRifleFire,
                (int)WeaponType.SniperRifle       => SfxSniperRifleFire,
                (int)WeaponType.Pistol            => SfxPistolFire,
                (int)WeaponType.GrenadeLauncher   => SfxGrenadeLauncherFire,
                (int)WeaponType.RayGun            => SfxRayGunFire,
                _                                 => SfxAutoRifleFire,
            };
        }
    }
}
