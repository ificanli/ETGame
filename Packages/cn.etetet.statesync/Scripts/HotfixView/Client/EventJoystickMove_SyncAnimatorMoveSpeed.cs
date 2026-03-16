namespace ET.Client
{
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// 摇杆移动同步后，刷新客户端单位的移动动画速度。
    /// </summary>
    [Event(SceneType.Client)]
    public class EventJoystickMove_SyncAnimatorMoveSpeed : AEvent<Scene, EventJoystickMoveSynced>
    {
        protected override async ETTask Run(Scene root, EventJoystickMoveSynced args)
        {
            Scene currentScene = root.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            Unit unit = unitComponent?.Get(args.UnitId);
            if (unit == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            AnimatorComponent animator = unit.GetComponent<AnimatorComponent>();
            if (animator != null)
            {
                animator.SetFloat(nameof(MotionType.MoveSpeed), args.MoveSpeed);

                Animator unityAnimator = animator.Animator;
                float runtimeMoveSpeed = 0f;
                bool hasWeapon = false;
                string currentClips = string.Empty;
                string nextClips = string.Empty;
                int currentStateHash = 0;
                int nextStateHash = 0;

                if (unityAnimator != null)
                {
                    if (animator.HasParameter(nameof(MotionType.MoveSpeed)))
                    {
                        runtimeMoveSpeed = unityAnimator.GetFloat(nameof(MotionType.MoveSpeed));
                    }

                    if (animator.HasParameter("HasWeapon"))
                    {
                        hasWeapon = unityAnimator.GetBool("HasWeapon");
                    }

                    currentStateHash = unityAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash;
                    nextStateHash = unityAnimator.GetNextAnimatorStateInfo(0).shortNameHash;
                    currentClips = GetClipNames(unityAnimator.GetCurrentAnimatorClipInfo(0));
                    nextClips = GetClipNames(unityAnimator.GetNextAnimatorClipInfo(0));
                }

                Log.Info($"[JoystickAnimTrace][Animator] unitId={args.UnitId}, argMoveSpeed={args.MoveSpeed:F3}, runtimeMoveSpeed={runtimeMoveSpeed:F3}, hasWeapon={hasWeapon}, currentStateHash={currentStateHash}, nextStateHash={nextStateHash}, currentClips={currentClips}, nextClips={nextClips}");
            }

            await ETTask.CompletedTask;
        }

        private static string GetClipNames(AnimatorClipInfo[] clipInfos)
        {
            if (clipInfos == null || clipInfos.Length == 0)
            {
                return "[]";
            }

            StringBuilder stringBuilder = new StringBuilder("[");
            for (int i = 0; i < clipInfos.Length; ++i)
            {
                if (i > 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append(clipInfos[i].clip != null ? clipInfos[i].clip.name : "null");
                stringBuilder.Append('@');
                stringBuilder.Append(clipInfos[i].weight.ToString("F3"));
            }

            stringBuilder.Append(']');
            return stringBuilder.ToString();
        }
    }
}
