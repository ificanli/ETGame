using Unity.Mathematics;

namespace ET.Client
{
	[MessageHandler(SceneType.Client)]
	public class M2C_TurnHandler : MessageHandler<Scene, M2C_Turn>
	{
		protected override async ETTask Run(Scene root, M2C_Turn message)
		{
			Unit unit = root.CurrentScene()?.GetComponent<UnitComponent>().Get(message.UnitId);
			if (unit == null)
			{
				return;
			}

			TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
			turnComponent.Turn(message.Rotation, message.TurnTime);
			await ETTask.CompletedTask;
		}
	}
}
