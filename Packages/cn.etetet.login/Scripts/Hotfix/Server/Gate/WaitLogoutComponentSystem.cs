namespace ET.Server
{
    [EntitySystemOf(typeof(WaitLogoutComponent))]
    public static partial class WaitLogoutComponentSystem
    {
        [EntitySystem]
        private static void Awake(this WaitLogoutComponent self)
        {
            self.WaitLogout().NoContext();
        }

        private static async ETTask WaitLogout(this WaitLogoutComponent self)
        {
            EntityRef<WaitLogoutComponent> selfRef = self;
            await self.Root().GetComponent<TimerComponent>().WaitAsync(60 * 1000);

            self = selfRef;
            if (self == null)
            {
                return;
            }

            Player player = self.GetParent<Player>();
            await LogoutHelper.Logout(player);
        }
    }
}