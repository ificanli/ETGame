namespace ET.Client
{
    [EntitySystemOf(typeof(ECAInteractClientComponent))]
    public static partial class ECAInteractClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAInteractClientComponent self)
        {
            self.InRangePointIds.Clear();
            self.FocusPointId = null;
            self.SearchingPointId = null;
            self.SearchState = ContainerSearchState.Idle;
            self.SearchRemainMs = 0;
            self.OpenContainerPointId = null;
            self.ContainerOutputMode = ContainerOutputMode.ContainerPanel;
            self.ContainerItems.Clear();
        }
    }
}
