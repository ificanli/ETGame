using UnityEngine;

namespace ET.Client
{
	[MessageHandler(SceneType.WOW)]
	public class M2C_PathfindingResultHandler_ChangeMotionToMove : MessageHandler<Scene, M2C_PathfindingResult>
	{
		protected override async ETTask Run(Scene root, M2C_PathfindingResult message)
		{
			Unit unit = root.CurrentScene().GetComponent<UnitComponent>().Get(message.Id);

			float speed = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Speed);
			
			// 动作
			Animator animator = unit.GetComponent<GameObjectComponent>().GameObject.GetComponent<Animator>();
			if (animator != null)
			{
				animator.SetFloat("VerticalSpeed", speed);
			}

			await ETTask.CompletedTask;
		}
	}
}
