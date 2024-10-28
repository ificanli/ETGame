namespace ET
{
    public struct ChangeSceneProgress
    {
        public float Progress;
    }
    
    // 可以用来管理多个客户端场景，比如大世界会加载多块场景
    [ComponentOf(typeof(Scene))]
    public class CurrentScenesComponent: Entity, IAwake
    {
        private float progress;
        
        public float Progress
        {
            get
            {
                return this.progress;
            }
            set
            {
                EventSystem.Instance.Publish(this.Root(), new ChangeSceneProgress() {Progress = value});
                this.progress = value;
            }
        }
        
        private EntityRef<Scene> scene;

        public Scene Scene
        {
            get
            {
                return this.scene;
            }
            set
            {
                this.scene = value;
            }
        }
    }
}