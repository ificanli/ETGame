using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTCreateEffect: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public BindPoint BindPoint;

        public OdinUnityObject Effect;

        public int Duration = 5000;
    }
}