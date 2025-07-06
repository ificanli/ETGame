using System;

namespace ET.Server
{
	[Module(ModuleName.Quest)]
	[MessageHandler(SceneType.Map)]
	public class C2M_RobotCase_PrepareData_001_Request_Handler : MessageLocationHandler<Unit, C2M_RobotCase_PrepareData_001_Request, C2M_RobotCase_PrepareData_001_Response>
	{
		protected override async ETTask Run(Unit unit, C2M_RobotCase_PrepareData_001_Request request, C2M_RobotCase_PrepareData_001_Response response)
		{
			await ETTask.CompletedTask;
		}
	}
}