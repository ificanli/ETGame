using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 监听武器切换事件，更新WeaponBar UI
    /// </summary>
    [Event(SceneType.Client)]
    public class EventWeaponSwitched_RefreshWeaponBar : AEvent<Scene, EventWeaponSwitched>
    {
        protected override async ETTask Run(Scene scene, EventWeaponSwitched args)
        {
            Scene root = scene.Root();
            if (root == null || root.IsDisposed)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 获取MainPanel
            MainPanelComponent mainPanel = root.YIUIMgr()?.GetPanel<MainPanelComponent>();
            if (mainPanel == null)
            {
                Log.Warning("[EventWeaponSwitched] MainPanelComponent not found");
                return;
            }

            // 获取WeaponBar
            WeaponBarComponent weaponBar = mainPanel.UIWeaponBar;
            if (weaponBar == null)
            {
                Log.Warning("[EventWeaponSwitched] WeaponBarComponent not found");
                return;
            }

            // 获取当前场景和玩家Unit
            Scene currentScene = root.CurrentScene();
            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null)
            {
                Log.Warning("[EventWeaponSwitched] My unit not found");
                return;
            }

            // 获取武器组件
            WeaponComponent weaponComp = myUnit.GetComponent<WeaponComponent>();
            if (weaponComp == null)
            {
                Log.Warning("[EventWeaponSwitched] WeaponComponent not found");
                return;
            }

            // 刷新武器栏显示
            long refreshBeginClientNow = TimeInfo.Instance.ClientNow();
            weaponBar.RefreshWeaponBar(weaponComp);
            long refreshDoneClientNow = TimeInfo.Instance.ClientNow();
            Log.Info(
                $"[WeaponSwitchTrace][UIRefresh] clientNow={refreshDoneClientNow}, serverNow={TimeInfo.Instance.ServerNow()}, unitId={args.UnitId}, slot={args.SlotIndex}, currentSlot={weaponComp.CurrentSlot}, slot1WeaponId={weaponComp.Slot1WeaponId}, slot2WeaponId={weaponComp.Slot2WeaponId}, slot1Ammo={weaponComp.Slot1Ammo}, slot2Ammo={weaponComp.Slot2Ammo}, costMs={refreshDoneClientNow - refreshBeginClientNow}");

            Log.Info($"[EventWeaponSwitched] WeaponBar refreshed: slot={args.SlotIndex}");

            await ETTask.CompletedTask;
        }
    }
}
