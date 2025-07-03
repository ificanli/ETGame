namespace ET
{    
    public abstract class ASingleton: DisposeObject
    {
        internal abstract void Register(int id);

        public int Id { get; protected set; }
    }
    
    public abstract class Singleton<T>: ASingleton where T: Singleton<T>
    {
        [StaticField]
        private static T instance;
        
        [StaticField]
        public static T Instance
        {
            get
            {
                return instance;
            }
            private set
            {
                instance = value;
            }
        }

        internal override void Register(int id)
        {
            Instance = (T)this;
            Id = id;
        }

        protected virtual void Destroy()
        {
            
        }

        public override void Dispose()
        {
            if (Instance == null)
            {
                return;
            }
            Instance = null;

            this.Destroy();
        }
    }
}