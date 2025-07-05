using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTCreateBuffEffect: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        [BTInput(typeof(Unit))]
        public string Buff;
        
        public BindPoint BindPoint;

        public OdinUnityObject Effect;

        public int Duration = 5000;
    }
}