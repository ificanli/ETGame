namespace ET.Server
{
    [EntitySystemOf(typeof(MatchCopyContextComponent))]
    public static partial class MatchCopyContextComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MatchCopyContextComponent self)
        {
            self.HumanPlayerIds.Clear();
            self.EnteredHumanPlayerIds.Clear();
            self.RobotPlayerIds.Clear();
            self.PlayerTeamIds.Clear();
            self.RobotsSpawned = false;
        }

        [EntitySystem]
        private static void Destroy(this MatchCopyContextComponent self)
        {
            self.HumanPlayerIds.Clear();
            self.EnteredHumanPlayerIds.Clear();
            self.RobotPlayerIds.Clear();
            self.PlayerTeamIds.Clear();
        }
    }
}
