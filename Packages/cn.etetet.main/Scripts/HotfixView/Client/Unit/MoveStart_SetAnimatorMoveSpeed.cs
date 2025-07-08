using UnityEngine;

namespace ET.Client
{
	[Event(SceneType.Current)]
	public class MoveStart_SetAnimatorMoveSpeed : AEvent<Scene, MoveStart>
	{
		protected override async ETTask Run(Scene scene, MoveStart a)
		{
			Unit unit = a.Unit;

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
