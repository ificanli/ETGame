using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 战斗相机调参组件，挂在 Virtual Camera 上，通过 Inspector 调整移动前视和锁定偏移。
    /// </summary>
    [EnableClass]
    [DisallowMultipleComponent]
    public class CombatCameraTuningView : MonoBehaviour
    {
        [Header("移动前视")]
        [Tooltip("移动时前视偏移占基础 CameraDistance 的比例")]
        public float MoveOffsetDistanceRatio = 0.35f;

        [Tooltip("是否启用移动前视偏移")]
        public bool EnableMoveOffset = true;

        [Header("锁定目标")]
        [Tooltip("锁定目标时目标偏移占基础 CameraDistance 的比例")]
        public float LockOffsetDistanceRatio = 0.5f;

        [Tooltip("是否启用锁定目标偏移")]
        public bool EnableLockOffset = true;

        [Tooltip("锁定目标时，移动前视偏移保留比例")]
        public float LockedMoveOffsetMultiplier = 0.4f;

        [Header("平滑")]
        [Tooltip("Follow 偏移收敛速度，数值越大镜头跟得越紧")]
        public float OffsetFollowSpeed = 18f;
    }
}
