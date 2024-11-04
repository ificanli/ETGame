using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ET.Client
{
    [EntitySystemOf(typeof(InputSystemComponent))]
    public static partial class InputSystemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this InputSystemComponent self)
        {
            self.CinemachineComponent = self.GetParent<Unit>().GetComponent<CinemachineComponent>();
            
            self.InputSystem = new InputSystem();
            self.InputSystem.Player.Enable();

            self.InputSystem.Player.Look.performed += self.Look;
            self.InputSystem.Player.Jump.performed += self.Jump;
        }
        
        [EntitySystem]
        private static void Update(this InputSystemComponent self)
        {
            if (self.InputSystem.Player.Move.IsPressed())
            {
                if (TimeInfo.Instance.FrameTime - self.PressTime > 100)
                {
                    self.PressTime = TimeInfo.Instance.FrameTime;
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

        private static void Jump(this InputSystemComponent self, InputAction.CallbackContext context)
        {
            
        }
    }
}