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
			AnimatorComponent animator = unit.GetComponent<AnimatorComponent>();
			if (animator != null)
			{
				animator.SetFloat(MotionType.MoveSpeed.ToString(), speed);
			}

			await ETTask.CompletedTask;
		}
	}
}
