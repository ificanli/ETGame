using System;

namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class C2M_TransferMapHandler : MessageLocationHandler<Unit, C2M_TransferMap, M2C_TransferMap>
	{
		protected override async ETTask Run(Unit unit, C2M_TransferMap request, M2C_TransferMap response)
		{
			string currentMap = unit.Scene().Name;
			string toMap = null;
			if (currentMap == "Map1")
			{
				toMap = "Map2";
			}
			else
			{
				toMap = "Map1";
			}
			
			TransferHelper.TransferAtFrameFinish(unit, toMap).NoContext();
			
			await ETTask.CompletedTask;
		}
	}
}