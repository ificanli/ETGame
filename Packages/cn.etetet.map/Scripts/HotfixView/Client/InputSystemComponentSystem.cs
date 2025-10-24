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

            self.InputSystem.Player.Look.performed += self.Look;
            self.InputSystem.Player.Jump.started += self.Jump;
            self.InputSystem.Player.SelectTarget.canceled += self.SelectTarget;
            self.InputSystem.Player.ChangeTarget.canceled += self.ChangeTarget;
            self.InputSystem.Player.Spell.started += self.CastSpell;
            self.InputSystem.Player.PetAttack.started += self.PetAttack;
        }

        [EntitySystem]
        private static void Update(this InputSystemComponent self)
        {
            if (self.InputSystem.Player.Move.IsPressed())
            {
                if (TimeInfo.Instance.ClientNow() - self.PressTime > 100)
                {
                    self.PressTime = TimeInfo.Instance.ClientNow();
                    Vector2 v = self.InputSystem.Player.Move.ReadValue<Vector2>();
                    self.Move(v);
                }
            }
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

        private static void Move(this InputSystemComponent self, Vector2 v)
        {
            if (self.IsJumping)
            {
                return;
            }

            if (v.magnitude < 0.001f)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            v = v.normalized * 2;

            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            Vector3 eulerAngles = cinemachineComponent.Follow.rotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;

            Vector3 rotV = Quaternion.Euler(eulerAngles) * new float3(v.x, 0, v.y);
            float3 targetPos = new float3(rotV) + unit.Position;

            unit.MoveToAsync(targetPos).NoContext();
        }

        // 鼠标左键点击目标，设置主角的目标
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
            
            UnitClickHelper.Click(self.Root(), targetUnit.Id).NoContext();
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

            int spellConfigId = (keyControl.keyCode - Key.Digit1) * 10 + 100000;

            EventSystem.Instance.Publish(self.Scene(), new OnSpellTrigger() { Unit = self.GetParent<Unit>(), SpellConfigId = spellConfigId });
        }
    }
}