using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public class BTCreateEffect: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public BindPoint BindPoint;

        public OdinUnityObject Effect;

        public int Duration = 5000;
    }
}