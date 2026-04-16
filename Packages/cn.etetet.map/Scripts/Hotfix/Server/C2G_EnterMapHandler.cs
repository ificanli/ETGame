namespace ET.Server
{
	[MessageSessionHandler(SceneType.Gate)]
	public class C2G_EnterMapHandler : MessageSessionHandler<C2G_EnterMap, G2C_EnterMap>
	{
		protected override async ETTask Run(Session session, C2G_EnterMap request, G2C_EnterMap response)
		{
			Player player = session.GetComponent<SessionPlayerComponent>().Player;
			response.MyId = player.Id;

			// 等到一帧的最后再传送，先让 G2C_EnterMap 返回，否则传送消息可能比 G2C_EnterMap 更早。
			GateMapTransferHelper.TransferPlayerToMap(player, "Home", player.Id, 0, true).Coroutine();
			await ETTask.CompletedTask;
		}
	}
}
