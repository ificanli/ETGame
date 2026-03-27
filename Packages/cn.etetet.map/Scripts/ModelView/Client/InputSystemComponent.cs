using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    public struct PendingMoveInputSample
    {
        public uint Sequence;
        public Vector2 WorldDirection;
        public float Speed;
        public long ClientTime;
    }

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
        public Vector2 KeyboardMoveInput;
        public Vector2 JoystickMoveInput;
        public Vector2 LastSentMoveInput;
        public long LastMoveSendTime;
        public uint MoveInputSequence;
        public uint LastAcknowledgedMoveInputSequence;
        public long LastInputTraceLogTime;
        public bool PendingStopActive;
        public uint PendingStopSequence;
        public Vector3 PendingStopAnchorPosition;
        public Vector3 PendingStopInitialAnchorDelta;
        public List<PendingMoveInputSample> PendingMoveInputs = new();
    }
}
