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
            self.PlayerControl = new PlayerControl();
            self.PlayerControl.Normal.Enable();
        }
        
        [EntitySystem]
        private static void Update(this PlayerControlComponent self)
        {
            if (!self.PlayerControl.Normal.Move.IsPressed())
            {
                return;
            }
            
            if (TimeInfo.Instance.FrameTime - self.PressTime > 100)
            {
                self.PressTime = TimeInfo.Instance.FrameTime;
                Vector2 v = self.PlayerControl.Normal.Move.ReadValue<Vector2>();
                self.Move(v);    
            }
        }
        
        private static void Move(this PlayerControlComponent self, Vector2 v)
        {
            if (v.magnitude < 0.001f)
            {
                return;
            }
            
            Unit unit = self.GetParent<Unit>();
            v = v.normalized;

            float3 rotV = math.rotate(unit.Rotation, new float3(v.x, 0, v.y));
            
            C2M_PathfindingResult c2MPathfindingResult = C2M_PathfindingResult.Create();
            c2MPathfindingResult.Position = unit.Position + rotV;
            self.Root().GetComponent<ClientSenderComponent>().Send(c2MPathfindingResult);
        }
    }
}