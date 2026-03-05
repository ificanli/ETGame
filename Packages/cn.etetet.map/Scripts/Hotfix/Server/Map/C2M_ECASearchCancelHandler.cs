namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_ECASearchCancelHandler : MessageLocationHandler<Unit, C2M_ECASearchCancel>
    {
        protected override async ETTask Run(Unit unit, C2M_ECASearchCancel request)
        {
            if (ContainerRuntimeHelper.TryGetPoint(unit.Scene(), request.PointId, out ECAPointComponent point))
            {
                ContainerRuntimeHelper.CancelSearch(point, unit, notify: true);
            }

            await ETTask.CompletedTask;
        }
    }
}
