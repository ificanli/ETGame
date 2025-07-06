using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTCreateFakeBullet: BTAction
    {
        [BTInput(typeof(Unit))]
        public string Caster;

        [BTInput(typeof(Unit))]
        public string Target;
        
        public BindPoint CasterBindPoint;
        
        public BindPoint TargetBindPoint;

        public int Speed;

        public OdinUnityObject Effect;

        public int Duration = 50000;
    }
}