using MongoDB.Bson.Serialization.Attributes;

namespace ET.Client
{
    public class BTCreateFakeBullet: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Caster;

        [BTInput(typeof(Unit))]
        public string Target;
        
        public BindPoint CasterBindPoint;
        
        public BindPoint TargetBindPoint;

        public int Speed;
        

#if UNITY
        [BsonIgnore]
        public UnityEngine.GameObject Effect;
#endif

        public int Duration = 50000;
    }
}