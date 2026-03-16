namespace ET.Server
{
    public static class LogoutHelper
    {
        public static async ETTask Logout(Player player)
        {
            long playerId = player.Id;
            
            Scene root = player.Root();
            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            
            playerComponent.Remove(player);
            
            // 从地图中移除玩家
            G2Map_Logout g2MapLogout = G2Map_Logout.Create();
            await root.GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit).Call(playerId, g2MapLogout);
        }
    }
}