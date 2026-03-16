using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ET.Client
{
    [EntitySystemOf(typeof(InputSystemComponent))]
    public static partial class InputSystemComponentSystem
    {
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

            //self.InputSystem.Player.Look.performed += self.Look;
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
                TimeInfo.Instance.ClientNow() - self.PressTime > 50)
            {
                self.PressTime = TimeInfo.Instance.ClientNow();
                self.SyncDirectionalMove(false);
            }

            self.SyncLocalPredictionState();
        }

        private static void Look(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            if (!Mouse.current.rightButton.isPressed)
            {
                return;
            }

            Vector2 v = context.ReadValue<Vector2>();
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            cinemachineComponent.RotationFollow(v);
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
            if (!forceSync && (worldDirection - self.LastSentMoveInput).sqrMagnitude < 0.000001f)
            {
                return;
            }

            self.LastSentMoveInput = worldDirection;
            self.SendDirectionalMove(worldDirection);
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

        private static void SendDirectionalMove(this InputSystemComponent self, Vector2 worldDirection)
        {
            ClientSenderComponent sender = self.Root().GetComponent<ClientSenderComponent>();
            if (sender == null)
            {
                return;
            }

            C2M_JoystickInput msg = C2M_JoystickInput.Create();
            msg.DirX = worldDirection.x;
            msg.DirZ = worldDirection.y;
            sender.Send(msg);
        }

        private static void SyncLocalPredictionState(this InputSystemComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            Vector2 worldDirection = self.GetSelectedWorldMoveDirection();
            float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;

            UnitViewInterpolationComponent interpolationComponent = unit.GetComponent<UnitViewInterpolationComponent>();
            if (interpolationComponent != null)
            {
                if (worldDirection.sqrMagnitude < 0.000001f || speed < 0.01f)
                {
                    interpolationComponent.SetPredictionMotion(float3.zero, 0f);
                    return;
                }

                float3 predictedDirection = new float3(worldDirection.x, 0f, worldDirection.y);
                interpolationComponent.SetPredictionMotion(predictedDirection, speed);
                return;
            }

            if (worldDirection.sqrMagnitude < 0.000001f || speed < 0.01f || Time.deltaTime <= 0f)
            {
                return;
            }

            float3 direction = new float3(worldDirection.x, 0f, worldDirection.y);
            float3 predictedDelta = direction * speed * Time.deltaTime;
            unit.Position += predictedDelta;

            TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
            if (turnComponent == null || !turnComponent.IsTurning())
            {
                unit.Rotation = quaternion.LookRotation(direction, math.up());
            }
        }

        private static void SelectTarget(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            const float maxDistance = 1000.0f;
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, maxDistance, LayerMask.GetMask("Unit")))
            {
                return;
            }
            GameObject clickedObject = hit.collider.gameObject;

            GameObjectEntityRef gameObjectEntityRef = clickedObject.GetComponent<GameObjectEntityRef>();
            if (gameObjectEntityRef is null)
            {
                return;
            }

            if (gameObjectEntityRef.Entity is not Unit targetUnit)
            {
                return;
            }
            
            UnitClickHelper.Click(self.Root(), targetUnit.Id).Coroutine();
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
            int spellConfigId = actionBar?.UISlot12?.u_DataId?.GetValue() ?? 0;
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
