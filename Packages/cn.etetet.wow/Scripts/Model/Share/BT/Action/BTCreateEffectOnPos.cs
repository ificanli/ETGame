using MongoDB.Bson.Serialization.Attributes;
using Unity.Mathematics;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTCreateEffectOnPos: BTNode
    {
        [BTInput(typeof(float3))]
        public string Pos;
        
        public OdinUnityObject Effect;

        public int Duration = 5000;
    }
}