using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [EnableMethod]
    [ChildOf]
    public class Scene: Entity, IScene
    {
        [BsonIgnore]
        public Fiber Fiber { get; set; }
        
        public string Name { get; set; }
        
        public int SceneType
        {
            get;
            set;
        }

        public Scene()
        {
        }

        public Scene(Fiber fiber, long id, int sceneType, string name)
        {
            this.Id = id;
            this.Name = name;
            this.InstanceId = fiber.NewInstanceId();
            this.SceneType = sceneType;
            this.IsNew = true;
            this.Fiber = fiber;
            this.IScene = this;
            this.IsRegister = true;
            Log.Info($"scene create: {SceneTypeSingleton.Instance.GetSceneName(this.SceneType)} {this.SceneType} {this.Id} {this.InstanceId}");
        }

        public override void Dispose()
        {
            base.Dispose();
            
            Log.Info($"scene dispose: {SceneTypeSingleton.Instance.GetSceneName(this.SceneType)} {this.SceneType} {this.Id} {this.InstanceId}");
        }
        
        protected override string ViewName
        {
            get
            {
                return $"{this.GetType().Name} ({this.SceneType})";
            }
        }
    }
}