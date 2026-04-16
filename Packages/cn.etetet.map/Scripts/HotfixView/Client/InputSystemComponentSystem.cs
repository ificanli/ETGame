using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ET.Client
{
    [EntitySystemOf(typeof(InputSystemComponent))]
    public static partial class InputSystemComponentSystem
    {
        private const int ContinuousMoveSyncIntervalMs = 50; // 20Hz 上报
        private const float ImmediateDirectionChangeDot = 0.9238795f;

        [EntitySystem]
        private static void Destroy(this InputSystemComponent self)
        {
            self.InputSystem?.Dispose();
        }

        [EntitySystem]
        private static void Awake(this InputSystemComponent self)
        {
            self.CinemachineComponent = self.GetParent<Unit>().GetComponent<CinemachineComponent>();

            self.InputSystem = new InputSystem();
            self.InputSystem.Player.Enable();
            self.KeyboardMoveInput = Vector2.zero;
            self.JoystickMoveInput = Vector2.zero;
            self.LastSentMoveInput = Vector2.zero;
            self.LastMoveSendTime = 0;
            self.MoveInputSequence = 0;
            self.LastAcknowledgedMoveInputSequence = 0;
            self.LastInputTraceLogTime = 0;

            self.InputSystem.Player.Jump.started += self.Jump;
            self.InputSystem.Player.SelectTarget.canceled += self.SelectTarget;
            self.InputSystem.Player.ChangeTarget.canceled += self.ChangeTarget;
            self.InputSystem.Player.Spell.started += self.CastSpell;
            self.InputSystem.Player.PetAttack.started += self.PetAttack;
        }

        [EntitySystem]
        private static void Update(this InputSystemComponent self)
        {
            Vector2 keyboardInput = self.InputSystem.Player.Move.ReadValue<Vector2>();
            if (keyboardInput.sqrMagnitude < 0.000001f)
            {
                keyboardInput = Vector2.zero;
            }

            if ((keyboardInput - self.KeyboardMoveInput).sqrMagnitude > 0.000001f)
            {
                self.KeyboardMoveInput = keyboardInput;
                self.PressTime = TimeInfo.Instance.ClientNow();
                if (self.JoystickMoveInput.sqrMagnitude < 0.000001f)
                {
                    self.SyncDirectionalMove(true);
                }
            }

            if (self.JoystickMoveInput.sqrMagnitude < 0.000001f &&
                keyboardInput != Vector2.zero &&
                TimeInfo.Instance.ClientNow() - self.PressTime > 16)
            {
                self.PressTime = TimeInfo.Instance.ClientNow();
                self.SyncDirectionalMove(false);
            }

            if (self.JoystickMoveInput.sqrMagnitude > 0.000001f)
            {
                self.SyncDirectionalMove(false);
            }

            self.SyncLocalPredictionState();
        }

        public static void SetJoystickMoveInput(this InputSystemComponent self, Vector2 joystickInput, bool forceSync)
        {
            if (joystickInput.sqrMagnitude < 0.000001f)
            {
                joystickInput = Vector2.zero;
            }

            self.JoystickMoveInput = joystickInput;
            self.SyncDirectionalMove(forceSync);
        }

        private static void SyncDirectionalMove(this InputSystemComponent self, bool forceSync)
        {
            Vector2 worldDirection = self.GetSelectedWorldMoveDirection();
            if (!self.ShouldSendDirectionalMove(worldDirection, forceSync, out string sendReason))
            {
                return;
            }

            self.LastSentMoveInput = worldDirection;
            self.LastMoveSendTime = TimeInfo.Instance.ClientNow();
            self.SendDirectionalMove(worldDirection, sendReason);
        }

        private static Vector2 GetSelectedMoveInput(this InputSystemComponent self)
        {
            Vector2 selectedInput = self.JoystickMoveInput.sqrMagnitude > 0.000001f
                ? self.JoystickMoveInput
                : self.KeyboardMoveInput;

            if (self.IsJumping)
            {
                return Vector2.zero;
            }

            return selectedInput;
        }

        private static Vector2 GetSelectedWorldMoveDirection(this InputSystemComponent self)
        {
            return self.ToWorldMoveDirection(self.GetSelectedMoveInput());
        }

        private static string GetSelectedMoveSource(this InputSystemComponent self)
        {
            if (self.IsJumping)
            {
                return "jump-stop";
            }

            if (self.JoystickMoveInput.sqrMagnitude > 0.000001f)
            {
                return "joystick";
            }

            if (self.KeyboardMoveInput.sqrMagnitude > 0.000001f)
            {
                return "keyboard";
            }

            return "idle";
        }

        private static Vector2 ToWorldMoveDirection(this InputSystemComponent self, Vector2 input)
        {
            if (input.sqrMagnitude < 0.000001f)
            {
                return Vector2.zero;
            }

            input = input.normalized;
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            if (cinemachineComponent == null || cinemachineComponent.IsDisposed || cinemachineComponent.Follow == null)
            {
                return input;
            }

            Vector3 eulerAngles = cinemachineComponent.Follow.rotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;

            Vector3 rotV = Quaternion.Euler(eulerAngles) * new float3(input.x, 0, input.y);
            Vector2 worldDirection = new Vector2(rotV.x, rotV.z);
            if (worldDirection.sqrMagnitude < 0.000001f)
            {
                return Vector2.zero;
            }

            return worldDirection.normalized;
        }

        private static bool ShouldSendDirectionalMove(this InputSystemComponent self, Vector2 worldDirection, bool forceSync, out string sendReason)
        {
            sendReason = forceSync ? "force" : string.Empty;
            if (forceSync)
            {
                return true;
            }

            bool currentStopped = worldDirection.sqrMagnitude < 0.000001f;
            bool previousStopped = self.LastSentMoveInput.sqrMagnitude < 0.000001f;
            if (currentStopped)
            {
                sendReason = "stop";
                return !previousStopped;
            }

            if (previousStopped)
            {
                sendReason = "start";
                return true;
            }

            bool sameDirection = (worldDirection - self.LastSentMoveInput).sqrMagnitude < 0.000001f;
            if (!sameDirection)
            {
                float directionDot = Vector2.Dot(worldDirection.normalized, self.LastSentMoveInput.normalized);
                if (directionDot <= ImmediateDirectionChangeDot)
                {
                    sendReason = "sharp-turn";
                    return true;
                }
            }

            if (TimeInfo.Instance.ClientNow() - self.LastMoveSendTime >= ContinuousMoveSyncIntervalMs)
            {
                sendReason = sameDirection ? "hold" : "coalesced-turn";
                return true;
            }

            return false;
        }

        private static void SendDirectionalMove(this InputSystemComponent self, Vector2 worldDirection, string sendReason)
        {
            ClientSenderComponent sender = self.Root().GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            uint inputSequence = ++self.MoveInputSequence;
            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;

            // 客户端权威移动：上报当前位置（而非方向）
            C2M_MoveState msg = C2M_MoveState.Create();
            msg.PosX = unit.Position.x;
            msg.PosY = unit.Position.y;
            msg.PosZ = unit.Position.z;
            msg.DirX = worldDirection.x;
            msg.DirZ = worldDirection.y;
            msg.Speed = worldDirection.sqrMagnitude > 0.000001f ? speed : 0f;
            msg.Sequence = inputSequence;
            msg.ClientTimeMs = TimeInfo.Instance.ClientNow();
            sender.Send(msg);
        }

        // 每帧将当前输入方向和速度写入插值组件
        private static void SyncLocalPredictionState(this InputSystemComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            UnitViewInterpolationComponent interpolation = unit.GetComponent<UnitViewInterpolationComponent>();
            if (interpolation == null || interpolation.IsDisposed)
            {
                // 没有插值组件时的降级路径：直接移动 unit.Position
                Vector2 worldDirection = self.GetSelectedWorldMoveDirection();
                float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
                if (worldDirection.sqrMagnitude < 0.000001f || speed < 0.01f || Time.deltaTime <= 0f)
                {
                    return;
                }

                float3 direction = new float3(worldDirection.x, 0f, worldDirection.y);
                unit.Position += direction * speed * Time.deltaTime;
                TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
                if (turnComponent == null || !turnComponent.IsTurning())
                {
                    unit.Rotation = quaternion.LookRotation(direction, math.up());
                }
                return;
            }

            if (unit.IsMyUnit())
            {
                Vector2 worldDirection = self.GetSelectedWorldMoveDirection();
                float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
                bool hasInput = worldDirection.sqrMagnitude > 0.000001f && speed > 0.01f;
                if (hasInput)
                {
                    float3 dir = new float3(worldDirection.x, 0f, worldDirection.y);
                    interpolation.SetLocalMoveInput(dir, speed);
                }
                else
                {
                    interpolation.SetLocalMoveInput(float3.zero, 0f);
                }
            }
            else
            {
                // 远端单位：方向和速度由 ChangePosition 链路设置，这里不需要额外操作
            }
        }

        // ========== 非移动输入处理（保持原样） ==========

        private static void SelectTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            // 场景直点单位交互已废弃，统一改为按钮触发。
        }

        private static void ChangeTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {
        }

        private static void Jump(this InputSystemComponent self, InputAction.CallbackContext context)
        {
        }

        private static void PetAttack(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            C2M_PetAttack c2MPetAttack = C2M_PetAttack.Create();
            c2MPetAttack.UnitId = self.GetParent<Unit>().GetComponent<TargetComponent>().Unit.Id;
            self.Root().GetComponent<ClientSenderComponent>().Send(c2MPetAttack);
        }

        private static void CastSpell(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            if (context.control is not KeyControl keyControl)
            {
                return;
            }

            if (keyControl.keyCode != Key.Digit1)
            {
                return;
            }

            MainPanelComponent mainPanel = self.Root().YIUIMgr().GetPanel<MainPanelComponent>();
            ActionBarComponent actionBar = mainPanel?.UIActionBar;
           // int spellConfigId = actionBar?.UISlot12?.u_DataId?.GetValue() ?? 0;
           int spellConfigId = 0;
           //todo 更新actionBar的数据，读表，用自己的slot
           if (spellConfigId <= 0)
            {
                Log.Warning("[Input] cast spell skipped: action bar skill not bound");
                return;
            }

            EventSystem.Instance.Publish(self.Scene(), new OnSpellTrigger
            {
                Unit = self.GetParent<Unit>(),
                SpellConfigId = spellConfigId,
            });
        }
    }
}
