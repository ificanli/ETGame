using System;
using UnityEngine;

namespace ET.Client
{
	[EntitySystemOf(typeof(AnimatorComponent))]
	public static partial class AnimatorComponentSystem
	{
		[EntitySystem]
		private static void Destroy(this AnimatorComponent self)
		{
			self.animationClips = null;
			self.Parameter = null;
			self.Animator = null;
		}
			
		[EntitySystem]
		private static void Awake(this AnimatorComponent self)
		{
			Unit unit = self.GetParent<Unit>();
			GameObject go = unit?.GetComponent<GameObjectComponent>()?.GameObject;
			if (go == null)
			{
				return;
			}

			Animator animator = go.GetComponent<Animator>();
			if (animator == null)
			{
				animator = go.GetComponentInChildren<Animator>(true);
			}

			if (animator == null)
			{
				Log.Warning($"[Animator] animator not found, unitId={unit?.Id ?? 0}, prefab={unit?.Config()?.Name}");
				return;
			}

			if (animator.runtimeAnimatorController == null)
			{
				Log.Warning($"[Animator] runtimeAnimatorController missing, unitId={unit?.Id ?? 0}, prefab={unit?.Config()?.Name}, animatorGo={animator.gameObject.name}");
				return;
			}

			if (animator.runtimeAnimatorController.animationClips == null)
			{
				Log.Warning($"[Animator] animation clips missing, unitId={unit?.Id ?? 0}, prefab={unit?.Config()?.Name}, animatorGo={animator.gameObject.name}");
				return;
			}
			self.Animator = animator;
			foreach (AnimationClip animationClip in animator.runtimeAnimatorController.animationClips)
			{
				self.animationClips[animationClip.name] = animationClip;
			}
			foreach (AnimatorControllerParameter animatorControllerParameter in animator.parameters)
			{
				self.Parameter.Add(animatorControllerParameter.name);
			}

			// 视图异步创建时，Speed 事件可能先于 Animator 准备完成，这里补一次当前速度。
			float currentSpeed = unit?.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
			self.SetFloat(nameof(MotionType.MoveSpeed), currentSpeed);
		}
		
		[EntitySystem]
		private static void Update(this AnimatorComponent self)
		{
			if (self.Animator == null)
			{
				return;
			}

			if (self.isStop)
			{
				return;
			}

			if (self.MotionType == MotionType.None)
			{
				return;
			}

			try
			{
				self.Animator.SetFloat("MotionSpeed", self.MontionSpeed);

				self.Animator.SetTrigger(self.MotionType.ToString());

				self.MontionSpeed = 1;
				self.MotionType = MotionType.None;
			}
			catch (Exception ex)
			{
				throw new Exception($"动作播放失败: {self.MotionType}", ex);
			}
		}

		public static bool HasParameter(this AnimatorComponent self, string parameter)
		{
			if (self == null || self.Animator == null || self.Parameter == null || string.IsNullOrEmpty(parameter))
			{
				return false;
			}

			return self.Parameter.Contains(parameter);
		}

		public static void PlayInTime(this AnimatorComponent self, MotionType motionType, float time)
		{
			AnimationClip animationClip;
			if (!self.animationClips.TryGetValue(motionType.ToString(), out animationClip))
			{
				throw new Exception($"找不到该动作: {motionType}");
			}

			float motionSpeed = animationClip.length / time;
			if (motionSpeed < 0.01f || motionSpeed > 1000f)
			{
				Log.Error($"motionSpeed数值异常, {motionSpeed}, 此动作跳过");
				return;
			}
			self.MotionType = motionType;
			self.MontionSpeed = motionSpeed;
		}

		public static void Play(this AnimatorComponent self, MotionType motionType, float motionSpeed = 1f)
		{
			if (!self.HasParameter(motionType.ToString()))
			{
				return;
			}
			self.MotionType = motionType;
			self.MontionSpeed = motionSpeed;
		}

		public static float AnimationTime(this AnimatorComponent self, MotionType motionType)
		{
			AnimationClip animationClip;
			if (!self.animationClips.TryGetValue(motionType.ToString(), out animationClip))
			{
				throw new Exception($"找不到该动作: {motionType}");
			}
			return animationClip.length;
		}

		public static void PauseAnimator(this AnimatorComponent self)
		{
			if (self.isStop)
			{
				return;
			}
			self.isStop = true;

			if (self.Animator == null)
			{
				return;
			}
			self.stopSpeed = self.Animator.speed;
			self.Animator.speed = 0;
		}

		public static void RunAnimator(this AnimatorComponent self)
		{
			if (!self.isStop)
			{
				return;
			}

			self.isStop = false;

			if (self.Animator == null)
			{
				return;
			}
			self.Animator.speed = self.stopSpeed;
		}

		public static void SetBool(this AnimatorComponent self, string name, bool state)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetBool(name, state);
		}

		public static void SetFloat(this AnimatorComponent self, string name, float state)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetFloat(name, state);
		}

		public static void SetInt(this AnimatorComponent self, string name, int value)
		{
			if (!self.HasParameter(name))
			{
				return;
			}

			self.Animator.SetInteger(name, value);
		}

		public static void SetTrigger(this AnimatorComponent self, string name)
		{
			if (!self.HasParameter(name))
			{
				return;
			}
			self.Animator.SetTrigger(name);
		}

		public static void SetAnimatorSpeed(this AnimatorComponent self, float speed)
		{
			if (self?.Animator == null)
			{
				return;
			}

			self.stopSpeed = self.Animator.speed;
			self.Animator.speed = speed;
		}

		public static void ResetAnimatorSpeed(this AnimatorComponent self)
		{
			if (self?.Animator == null)
			{
				return;
			}

			self.Animator.speed = self.stopSpeed;
		}
	}
}
