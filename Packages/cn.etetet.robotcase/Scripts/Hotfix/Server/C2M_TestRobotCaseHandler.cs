using System;

namespace ET.Server
{
	[MessageHandler(SceneType.Map)]
	public class C2M_TestRobotCaseHandler : MessageLocationHandler<Unit, C2M_TestRobotCase, M2C_TestRobotCase>
	{
		protected override async ETTask Run(Unit unit, C2M_TestRobotCase request, M2C_TestRobotCase response)
		{
			response.N = request.N;
			
			// 这是一个通用的测试用例Handler
			// 各个具体的测试用例应该有自己专用的数据准备消息和Handler
			// 例如：C2M_RobotCase_PrepareData_001_Handler, C2M_RobotCase_PrepareData_002_Handler 等
			
			Log.Info($"General robot test case executed with N={request.N}");
			
			await ETTask.CompletedTask;
		}
	}
}