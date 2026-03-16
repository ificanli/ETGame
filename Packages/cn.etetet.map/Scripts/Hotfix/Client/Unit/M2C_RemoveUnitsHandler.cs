namespace ET.Client
{
	[MessageHandler(SceneType.Client)]
	public class M2C_RemoveUnitsHandler: MessageHandler<Scene, M2C_RemoveUnits>
	{
		protected override async ETTask Run(Scene root, M2C_RemoveUnits message)
		{	
			Scene currentScene = root.CurrentScene();
			UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
			if (unitComponent == null)
			{
				return;
			}
			foreach (long unitId in message.Units)
			{
				Unit unit = unitComponent.Get(unitId);
				if (unit != null)
				{
					EventSystem.Instance.Publish(currentScene, new BeforeUnitRemove() {Unit = unit});
				}

				unitComponent.Remove(unitId);
			}

			await ETTask.CompletedTask;
		}
	}
}
