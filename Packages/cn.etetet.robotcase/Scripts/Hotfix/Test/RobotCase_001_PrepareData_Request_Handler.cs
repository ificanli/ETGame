using System;
using ET.Server;

namespace ET.Test
{
	[MessageHandler(SceneType.Map)]
	public class RobotCase_001_PrepareData_Request_Handler : MessageLocationHandler<Unit, RobotCase_001_PrepareData_Request, RobotCase_001_PrepareData_Response>
	{
		protected override async ETTask Run(Unit unit, RobotCase_001_PrepareData_Request request, RobotCase_001_PrepareData_Response response)
		{
			await ETTask.CompletedTask;
		}
	}
}