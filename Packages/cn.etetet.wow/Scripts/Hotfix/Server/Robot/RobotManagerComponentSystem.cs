namespace ET.Server
{
    [EntitySystemOf(typeof(RobotManagerComponent))]
    public static partial class RobotManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RobotManagerComponent self)
        {
        }
        
        [EntitySystem]
        private static void Destroy(this RobotManagerComponent self)
        {
            async ETTask Remove(int f)
            {
                await FiberManager.Instance.Remove(f);
            }
            
            foreach (int fiberId in self.robots.Values)
            {
                Remove(fiberId).NoContext();
            }
        }

        public static bool GetRobotActorId(this RobotManagerComponent self, string account, out ActorId actorId)
        {
            if (!self.robots.TryGetValue(account, out int fiberId))
            {
                actorId = default;
                return false;
            }
            actorId = new ActorId(self.Fiber().Process, fiberId);
            return true;
        }

        public static async ETTask NewRobot(this RobotManagerComponent self, string account)
        {
            EntityRef<RobotManagerComponent> selfRef = self;
            int robot = await FiberManager.Instance.Create(SchedulerType.ThreadPool, self.Zone(), SceneType.Robot, account);
            self = selfRef;
            self.robots.Add(account, robot);
        }
    }
}