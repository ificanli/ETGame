using MongoDB.Bson.Serialization.Attributes;
using Unity.Mathematics;

namespace ET
{
    public class BTCreateEffectOnPos: BTNode
    {
        [BTInput(typeof(float3))]
        public string Pos;
        
#if UNITY
        [BsonIgnore]
        public UnityEngine.GameObject Effect;
#endif

        public int Duration = 5000;
    }
}