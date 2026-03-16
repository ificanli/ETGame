namespace ET.Client
{
    [EntitySystemOf(typeof(ECAPointViewRuntimeComponent))]
    public static partial class ECAPointViewRuntimeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAPointViewRuntimeComponent self)
        {
            self.PointViewMarkers.Clear();
            self.AppliedStates.Clear();
            self.BindingsBuilt = false;
        }
    }
}
