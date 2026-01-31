using System;
using System.Collections.Generic;

namespace ET
{
    public partial class Scene
    {
        private EntityRef<CoroutineLockComponent> coroutineLockComponent;

        public CoroutineLockComponent CoroutineLockComponent
        {
            get
            {
                return this.coroutineLockComponent;
            }
            set
            {
                this.coroutineLockComponent = value;
            }
        }
    }

    [DisableGetComponent]
    [ComponentOf(typeof(Scene))]
    public class CoroutineLockComponent: Entity, IAwake, IScene, IUpdate
    {
        public Fiber Fiber { get; set; }
        public int SceneType { get; set; }
        
        public readonly Queue<(long, long, int)> nextFrameRun = new();
    }
}