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
                await self.Fiber().RemoveFiber(f);
            }
            
            foreach (int fiberId in self.robots.Values)
            {
                Remove(fiberId).Coroutine();
            }
        }

        public static bool GetRobotActorId(this RobotManagerComponent self, string account, out ActorId actorId)
        {
            if (!self.robots.TryGetValue(account, out int fiberId))
            {
                actorId = default;
                return false;
            }
            actorId = new ActorId(AddressSingleton.Instance.InnerAddress, new FiberInstanceId(fiberId, 1));
            return true;
        }

        /// <summary>
        /// 创建机器人，await之后，机器人则登录成功
        /// </summary>
        public static async ETTask<int> NewRobot(this RobotManagerComponent self, SchedulerType schedulerType, string account)
        {
            EntityRef<RobotManagerComponent> selfRef = self;
            int robot = await self.Fiber().CreateFiber(schedulerType, IdGenerater.Instance.GenerateId(), 0, SceneType.Client, account);
            self = selfRef;
            ProcessInnerSender processInnerSender = self.Root().GetComponent<ProcessInnerSender>();
            Robot_LoginRequest robotLoginRequest = Robot_LoginRequest.Create();
            robotLoginRequest.Account = account;
            robotLoginRequest.Password = "";
            await processInnerSender.Call(new FiberInstanceId(robot, 1), robotLoginRequest);
            self = selfRef;
            self.robots.Add(account, robot);
            return robot;
        }
    }
}