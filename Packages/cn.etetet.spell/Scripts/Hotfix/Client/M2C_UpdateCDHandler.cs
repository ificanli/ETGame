using Unity.Mathematics;

namespace ET.Client
{
	[MessageHandler(SceneType.Client)]
	public class M2C_UpdateCDHandler : MessageHandler<Scene, M2C_UpdateCD>
	{
		protected override async ETTask Run(Scene root, M2C_UpdateCD message)
		{
			Unit unit = root.CurrentScene()?.GetComponent<UnitComponent>().Get(message.UnitId);
			if (unit == null)
			{
				return;
			}

			SpellComponent spellComponent = unit.GetComponent<SpellComponent>();
			spellComponent.UpdateCD(message.SpellConfigId, message.Time);
			await ETTask.CompletedTask;
		}
	}
}
