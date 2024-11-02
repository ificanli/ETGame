using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ET.Client
{
    [EntitySystemOf(typeof(PlayerControlComponent))]
    public static partial class PlayerControlComponentSystem
    {
        [EntitySystem]
        private static void Awake(this PlayerControlComponent self)
        {
            self.CinemachineComponent = self.GetParent<Unit>().GetComponent<CinemachineComponent>();
            
            self.PlayerControl = new PlayerControl();
            self.PlayerControl.Player.Enable();

            self.PlayerControl.Player.Look.performed += self.OnLookChange;
        }
        
        [EntitySystem]
        private static void Update(this PlayerControlComponent self)
        {
            if (self.PlayerControl.Player.Move.IsPressed())
            {
                if (TimeInfo.Instance.FrameTime - self.PressTime > 100)
                {
                    self.PressTime = TimeInfo.Instance.FrameTime;
                    Vector2 v = self.PlayerControl.Player.Move.ReadValue<Vector2>();
                    self.Move(v);    
                }
            }
        }

        private static void OnLookChange(this PlayerControlComponent self, InputAction.CallbackContext context)
        {
            if (!Mouse.current.rightButton.isPressed)
            {
                return;
            }
            
            Vector2 v = context.ReadValue<Vector2>();
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            cinemachineComponent.RotationFollow(v);
        }
        
        private static void Move(this PlayerControlComponent self, Vector2 v)
        {
            if (v.magnitude < 0.001f)
            {
                return;
            }
            
            Unit unit = self.GetParent<Unit>();
            v = v.normalized;
            
            CinemachineComponent cinemachineComponent = self.CinemachineComponent;
            Vector3 eulerAngles = cinemachineComponent.Follow.rotation.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            
            Vector3 rotV = Quaternion.Euler(eulerAngles) * new float3(v.x, 0, v.y);
            
            C2M_PathfindingResult c2MPathfindingResult = C2M_PathfindingResult.Create();
            c2MPathfindingResult.Position = unit.Position + new float3(rotV);
            
            self.Root().GetComponent<ClientSenderComponent>().Send(c2MPathfindingResult);
        }
    }
}