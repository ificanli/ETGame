using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public class BTCreateEffect: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public BindPoint BindPoint;

#if UNITY
        [BsonIgnore]
        public UnityEngine.GameObject Effect;
#endif

        public int Duration = 5000;
    }
}