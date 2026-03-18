namespace ET.Server
{
    [Event(SceneType.Map)]
    public class UnitWeaponReloadCompleted_RogueReloadFirstShots : AEvent<Scene, UnitWeaponReloadCompleted>
    {
        protected override async ETTask Run(Scene scene, UnitWeaponReloadCompleted args)
        {
            RogueReloadFirstShotsEventHelper.TryActivate(args.Unit);
            await ETTask.CompletedTask;
        }
    }

    public static class RogueReloadFirstShotsEventHelper
    {
        public static bool TryActivate(Unit unit)
        {
            RogueReloadFirstShotsStateComponent stateComponent = unit?.GetComponent<RogueReloadFirstShotsStateComponent>();
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player || stateComponent == null)
            {
                return false;
            }

            stateComponent.ActivateOnReload();
            return true;
        }
    }
}
