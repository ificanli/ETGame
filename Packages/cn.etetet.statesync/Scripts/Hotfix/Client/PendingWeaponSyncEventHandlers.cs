namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterUnitCreate_ApplyPendingWeaponSync : AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            PendingWeaponSyncComponent pending = scene.GetComponent<PendingWeaponSyncComponent>();
            if (pending == null)
            {
                return;
            }

            pending.TryApply(scene.Root(), args.Unit);
            await ETTask.CompletedTask;
        }
    }

    [Event(SceneType.Current)]
    public class BeforeUnitRemove_ClearPendingWeaponSync : AEvent<Scene, BeforeUnitRemove>
    {
        protected override async ETTask Run(Scene scene, BeforeUnitRemove args)
        {
            PendingWeaponSyncComponent pending = scene.GetComponent<PendingWeaponSyncComponent>();
            if (pending == null)
            {
                return;
            }

            Unit unit = args.Unit;
            if (unit != null && !unit.IsDisposed)
            {
                pending.RemovePending(unit.Id);
            }

            await ETTask.CompletedTask;
        }
    }
}
