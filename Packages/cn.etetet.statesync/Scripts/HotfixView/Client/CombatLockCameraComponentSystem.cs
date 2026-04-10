using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 管理本地玩家基于当前战斗目标的相机偏移。
    /// </summary>
    [EntitySystemOf(typeof(CombatLockCameraComponent))]
    public static partial class CombatLockCameraComponentSystem
    {
        private const float DefaultMoveOffsetDistanceRatio = 0.35f;
        private const float DefaultLockOffsetDistanceRatio = 0.5f;
        private const float DefaultLockedMoveOffsetMultiplier = 0.4f;
        private const float DefaultOffsetFollowSpeed = 18f;

        [EntitySystem]
        private static void Awake(this CombatLockCameraComponent self)
        {
            self.CinemachineComponent = self.GetParent<Unit>().GetComponent<CinemachineComponent>();
            self.CurrentTargetUnitId = 0;
            self.CurrentMoveOffset = Vector3.zero;
            self.CurrentLockOffset = Vector3.zero;
            self.CurrentWorldOffset = Vector3.zero;
        }

        [EntitySystem]
        private static void Destroy(this CombatLockCameraComponent self)
        {
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            cinemachineComponent?.ResetFollowOffset();
            self.CinemachineComponent = default;
            self.CurrentTargetUnitId = 0;
            self.CurrentMoveOffset = Vector3.zero;
            self.CurrentLockOffset = Vector3.zero;
            self.CurrentWorldOffset = Vector3.zero;
        }

        [EntitySystem]
        private static void Update(this CombatLockCameraComponent self)
        {
            Unit owner = self.GetParent<Unit>();
            if (owner == null || owner.IsDisposed || !owner.IsMyUnit())
            {
                return;
            }

            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            if (cinemachineComponent == null || cinemachineComponent.IsDisposed)
            {
                cinemachineComponent = owner.GetComponent<CinemachineComponent>();
                self.CinemachineComponent = cinemachineComponent;
            }

            if (cinemachineComponent == null || cinemachineComponent.IsDisposed)
            {
                self.CurrentTargetUnitId = 0;
                self.CurrentMoveOffset = Vector3.zero;
                self.CurrentLockOffset = Vector3.zero;
                self.CurrentWorldOffset = Vector3.zero;
                return;
            }

            CombatCameraTuningView tuningView = self.GetTuningView(cinemachineComponent);
            self.CurrentMoveOffset = self.CalculateMoveOffset(cinemachineComponent, owner, tuningView);
            Unit target = self.ResolveLockedTarget(owner);
            if (target == null)
            {
                self.CurrentTargetUnitId = 0;
                self.CurrentLockOffset = Vector3.zero;
            }
            else
            {
                self.CurrentTargetUnitId = target.Id;
                self.CurrentMoveOffset = self.ApplyLockedMoveOffsetMultiplier(tuningView, self.CurrentMoveOffset);
                self.CurrentLockOffset = self.CalculateLockOffset(cinemachineComponent, owner, target, tuningView);
            }

            Vector3 desiredWorldOffset = self.CurrentMoveOffset + self.CurrentLockOffset;
            self.CurrentWorldOffset = self.MoveTowardsOffset(tuningView, desiredWorldOffset);
            cinemachineComponent.SetFollowOffset(self.CurrentWorldOffset);
        }

        private static CombatCameraTuningView GetTuningView(this CombatLockCameraComponent self, CinemachineComponent cinemachineComponent)
        {
            return cinemachineComponent?.VirtualCamera != null
                ? cinemachineComponent.VirtualCamera.GetComponent<CombatCameraTuningView>()
                : null;
        }

        private static Unit ResolveLockedTarget(this CombatLockCameraComponent self, Unit owner)
        {
            TargetComponent targetComponent = owner.GetComponent<TargetComponent>();
            Unit target = targetComponent?.Unit;
            if (self.IsValidLockedTarget(owner, target))
            {
                return target;
            }

            CombatIndicatorComponent combatIndicatorComponent = owner.GetComponent<CombatIndicatorComponent>();
            if (combatIndicatorComponent == null || combatIndicatorComponent.CurrentTargetUnitId == 0)
            {
                return null;
            }

            UnitComponent unitComponent = owner.Scene()?.GetComponent<UnitComponent>();
            target = unitComponent?.Get(combatIndicatorComponent.CurrentTargetUnitId);
            return self.IsValidLockedTarget(owner, target) ? target : null;
        }

        private static bool IsValidLockedTarget(this CombatLockCameraComponent self, Unit owner, Unit target)
        {
            if (owner == null || target == null || owner.IsDisposed || target.IsDisposed)
            {
                return false;
            }

            if (owner.Id == target.Id)
            {
                return false;
            }

            if (!CampHelper.IsEnemy(owner, target))
            {
                return false;
            }

            NumericComponent numericComponent = target.NumericComponent;
            return numericComponent == null || numericComponent.GetAsFloat(NumericType.HP) > 0f;
        }

        private static Vector3 CalculateMoveOffset(this CombatLockCameraComponent self, CinemachineComponent cinemachineComponent, Unit owner,
            CombatCameraTuningView tuningView)
        {
            if (tuningView != null && !tuningView.EnableMoveOffset)
            {
                return Vector3.zero;
            }

            Vector3 moveDirection = self.ResolveMoveDirection(owner, out float moveSpeed);
            if (moveDirection.sqrMagnitude <= 0.000001f || moveSpeed <= 0.01f)
            {
                return Vector3.zero;
            }

            float capabilityMoveSpeed = owner.NumericComponent?.GetAsFloat(NumericType.Speed) ?? moveSpeed;
            if (capabilityMoveSpeed <= 0.01f)
            {
                return Vector3.zero;
            }

            float speedRatio = Mathf.Clamp01(moveSpeed / capabilityMoveSpeed);
            float moveOffsetDistanceRatio = tuningView != null ? tuningView.MoveOffsetDistanceRatio : DefaultMoveOffsetDistanceRatio;
            float maxMoveOffsetDistance = cinemachineComponent.BaseDistance > 0f
                ? cinemachineComponent.BaseDistance * Mathf.Max(0f, moveOffsetDistanceRatio)
                : moveSpeed;
            return moveDirection * (maxMoveOffsetDistance * speedRatio);
        }

        private static Vector3 ResolveMoveDirection(this CombatLockCameraComponent self, Unit owner, out float moveSpeed)
        {
            moveSpeed = 0f;

            UnitViewInterpolationComponent interpolationComponent = owner.GetComponent<UnitViewInterpolationComponent>();
            if (interpolationComponent != null)
            {
                Vector3 direction = interpolationComponent.LocalMoveDirection;
                moveSpeed = interpolationComponent.LocalMoveSpeed;
                if (direction.sqrMagnitude > 0.000001f && moveSpeed > 0.01f)
                {
                    return direction.normalized;
                }
            }

            JoystickMoveAuthorityStateComponent authorityState = owner.GetComponent<JoystickMoveAuthorityStateComponent>();
            if (authorityState == null || authorityState.LastSpeed <= 0.01f)
            {
                moveSpeed = 0f;
                return Vector3.zero;
            }

            Vector3 authorityDirection = new Vector3(authorityState.LastDirection.x, 0f, authorityState.LastDirection.z);
            if (authorityDirection.sqrMagnitude <= 0.000001f)
            {
                moveSpeed = 0f;
                return Vector3.zero;
            }

            moveSpeed = authorityState.LastSpeed;
            return authorityDirection.normalized;
        }

        private static Vector3 CalculateLockOffset(this CombatLockCameraComponent self, CinemachineComponent cinemachineComponent, Unit owner, Unit target,
            CombatCameraTuningView tuningView)
        {
            if (tuningView != null && !tuningView.EnableLockOffset)
            {
                return Vector3.zero;
            }

            Vector3 ownerPosition = self.GetUnitWorldPosition(owner);
            Vector3 targetPosition = self.GetUnitWorldPosition(target);
            Vector3 flatDelta = targetPosition - ownerPosition;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude <= 0.000001f)
            {
                return Vector3.zero;
            }

            Vector3 midpointOffset = (ownerPosition + targetPosition) * 0.5f - ownerPosition;
            midpointOffset.y = 0f;
            float lockOffsetDistanceRatio = tuningView != null ? tuningView.LockOffsetDistanceRatio : DefaultLockOffsetDistanceRatio;
            float maxOffsetDistance = cinemachineComponent.BaseDistance > 0f
                ? cinemachineComponent.BaseDistance * Mathf.Max(0f, lockOffsetDistanceRatio)
                : midpointOffset.magnitude;
            return Vector3.ClampMagnitude(midpointOffset, maxOffsetDistance);
        }

        private static Vector3 ApplyLockedMoveOffsetMultiplier(this CombatLockCameraComponent self, CombatCameraTuningView tuningView, Vector3 moveOffset)
        {
            float lockedMoveOffsetMultiplier = tuningView != null
                ? tuningView.LockedMoveOffsetMultiplier
                : DefaultLockedMoveOffsetMultiplier;
            return moveOffset * Mathf.Clamp01(lockedMoveOffsetMultiplier);
        }

        private static Vector3 MoveTowardsOffset(this CombatLockCameraComponent self, CombatCameraTuningView tuningView, Vector3 desiredWorldOffset)
        {
            float offsetFollowSpeed = tuningView != null ? tuningView.OffsetFollowSpeed : DefaultOffsetFollowSpeed;
            if (offsetFollowSpeed <= 0f)
            {
                return desiredWorldOffset;
            }

            return Vector3.MoveTowards(self.CurrentWorldOffset, desiredWorldOffset, offsetFollowSpeed * Time.deltaTime);
        }

        private static Vector3 GetUnitWorldPosition(this CombatLockCameraComponent self, Unit unit)
        {
            Transform transform = unit.GetComponent<GameObjectComponent>()?.Transform;
            return transform != null ? transform.position : (Vector3)unit.Position;
        }
    }
}
