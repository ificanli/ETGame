namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_ContainerCloseHandler : MessageLocationHandler<Unit, C2M_ContainerClose>
    {
        protected override async ETTask Run(Unit unit, C2M_ContainerClose request)
        {
            if (ContainerRuntimeHelper.TryGetPoint(unit.Scene(), request.PointId, out ECAPointComponent point))
            {
                ContainerRuntimeHelper.CloseContainer(point, unit);
            }

            await ETTask.CompletedTask;
        }
    }
}
