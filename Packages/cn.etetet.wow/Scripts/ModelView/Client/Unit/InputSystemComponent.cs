using UnityEngine;

namespace ET.Client
{
    public struct OnSpellTrigger
    {
        public Unit Unit;
        public int SpellConfigId;
    }
    
    [ComponentOf(typeof(Unit))]
    public partial class InputSystemComponent: Entity, IAwake, IUpdate
    {
        public InputSystem InputSystem;
        public long PressTime;

        public EntityRef<CinemachineComponent> CinemachineComponent;

        public bool IsJumping;
    }
}