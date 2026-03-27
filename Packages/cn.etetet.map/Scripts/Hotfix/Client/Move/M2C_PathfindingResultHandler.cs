namespace ET.Client
{
	[MessageHandler(SceneType.Client)]
	public class M2C_PathfindingResultHandler : MessageHandler<Scene, M2C_PathfindingResult>
	{
		protected override async ETTask Run(Scene root, M2C_PathfindingResult message)
		{
			Scene currentScene = root.CurrentScene();
			if (currentScene == null || currentScene.IsDisposed)
			{
				Log.Warning($"[PathfindingResult] skip because current scene missing: unitId={message.Id}");
				return;
			}

			UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
			Unit unit = unitComponent?.Get(message.Id);
			if (unit == null || unit.IsDisposed)
			{
				Log.Warning($"[PathfindingResult] skip because unit missing: unitId={message.Id}");
				return;
			}

			NumericComponent numericComponent = unit.NumericComponent;
			MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
			if (numericComponent == null || moveComponent == null)
			{
				Log.Warning($"[PathfindingResult] skip because component missing: unitId={message.Id}, numericNull={numericComponent == null}, moveNull={moveComponent == null}");
				return;
			}

			float speed = numericComponent.GetAsFloat(NumericType.Speed);

			await moveComponent.MoveToAsync(message.Points, speed, 200);
		}
	}
}
