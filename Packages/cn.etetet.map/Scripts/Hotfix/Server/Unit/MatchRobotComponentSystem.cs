namespace ET.Server
{
    [EntitySystemOf(typeof(MatchRobotComponent))]
    public static partial class MatchRobotComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MatchRobotComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this MatchRobotComponent self)
        {
            if (self.AutoLevelTimerId != 0)
            {
                self.GetParent<Unit>()?.Root()?.TimerComponent?.Remove(ref self.AutoLevelTimerId);
                self.AutoLevelTimerId = 0;
            }
        }
    }
}
