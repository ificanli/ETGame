using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class TurnComponent: Entity, IAwake
    {
        public long StartTime;
        
        public int TurnTime;

        public bool IsTurnHorizontal;

        public quaternion From;

        public quaternion To;

        public long timer;
    }
}