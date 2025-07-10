using UnityEngine;

namespace ET.Client
{
	[Event(SceneType.Current)]
	public class MoveStop_SetAnimatorMoveSpeed : AEvent<Scene, MoveStop>
	{
		protected override async ETTask Run(Scene scene, MoveStop a)
		{
			Unit unit = a.Unit;

			// 动作
			AnimatorComponent animator = unit.GetComponent<AnimatorComponent>();
			if (animator != null)
			{
				animator.SetFloat(MotionType.MoveSpeed.ToString(), 0);
			}

			await ETTask.CompletedTask;
		}
	}
}
