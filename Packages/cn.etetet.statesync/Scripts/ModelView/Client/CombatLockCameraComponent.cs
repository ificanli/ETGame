using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 本地玩家基于当前战斗目标的战斗相机偏移状态。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class CombatLockCameraComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public EntityRef<CinemachineComponent> CinemachineComponent;
        public long CurrentTargetUnitId;
        public Vector3 CurrentMoveOffset;
        public Vector3 CurrentLockOffset;
        public Vector3 CurrentWorldOffset;
    }
}
