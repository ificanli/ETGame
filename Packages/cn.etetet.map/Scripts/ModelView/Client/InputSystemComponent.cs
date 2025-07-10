using UnityEngine;

namespace ET.Client
{
    public struct OnSpellTrigger
    {
        public EntityRef<Unit> Unit;
        public int SpellConfigId;
    }
    
    [ComponentOf(typeof(Unit))]
    public partial class InputSystemComponent: Entity, IAwake, IUpdate, IDestroy
    {
        public InputSystem InputSystem;
        public long PressTime;

        public EntityRef<CinemachineComponent> CinemachineComponent;

        public bool IsJumping;
    }
}