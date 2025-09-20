using System;
using System.Collections.Generic;
using System.Net;


namespace ET.Server
{
	[MessageSessionHandler(SceneType.Realm)]
	public class C2R_LoginHandler : MessageSessionHandler<C2R_Login, R2C_Login>
	{
		protected override async ETTask Run(Session session, C2R_Login request, R2C_Login response)
		{
			EntityRef<Session> sessionRef = session;
			const int UserZone = 2; // 这里一般会有创角，选择区服，demo就不做这个操作了，直接放在3区
			// 随机分配一个Gate
			Scene root = session.Root();
			EntityRef<Scene> rootRef = root;
			ulong hash = (ulong)request.Account.GetLongHashCode();
			
			ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = root.GetComponent<ServiceDiscoveryProxyComponent>();

			List<string> gates = serviceDiscoveryProxyComponent.GetByZoneSceneType(UserZone, SceneType.Gate);
			string gateName = gates[(int)(hash % (ulong)gates.Count)];
			Log.Debug($"gate address: {gateName}");
			
			// 向gate请求一个key,客户端可以拿着这个key连接gate
			R2G_GetLoginKey r2GGetLoginKey = R2G_GetLoginKey.Create();
			r2GGetLoginKey.Account = request.Account;
			ServiceCacheInfo gateServiceInfo = await serviceDiscoveryProxyComponent.GetServiceInfo(gateName);
			response.Address = gateServiceInfo.Metadata[ServiceMetaKey.InnerIPPort];
			
			root = rootRef;
			MessageSender messageSender = root.GetComponent<MessageSender>();
			G2R_GetLoginKey g2RGetLoginKey = (G2R_GetLoginKey) await messageSender.Call(gateServiceInfo.ActorId, r2GGetLoginKey);
			
			response.Key = g2RGetLoginKey.Key;
			response.GateId = g2RGetLoginKey.GateId;
			session = sessionRef;
			CloseSession(session).NoContext();
		}

		private async ETTask CloseSession(Session session)
		{
			EntityRef<Session> sessionRef = session;
			await session.Root().GetComponent<TimerComponent>().WaitAsync(1000);
			session = sessionRef;
			session?.Dispose();
		}
	}
}
