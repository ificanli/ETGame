using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
	[MessageHandler(SceneType.WOW)]
	public class M2C_StopHandler_ChangeMotionToIdel : MessageHandler<Scene, M2C_Stop>
	{
		protected override async ETTask Run(Scene root, M2C_Stop message)
		{
			Unit unit = root.CurrentScene().GetComponent<UnitComponent>().Get(message.Id);
			if (unit == null)
			{
				return;
			}

			Animator animator = unit.GetComponent<GameObjectComponent>().GameObject.GetComponent<Animator>();
			if (animator != null)
			{
				animator.SetFloat("VerticalSpeed", 0);
			}
			await ETTask.CompletedTask;
		}
	}
}
