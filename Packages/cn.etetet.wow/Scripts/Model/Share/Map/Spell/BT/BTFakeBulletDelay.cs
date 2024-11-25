namespace ET
{
    public class BTFakeBulletDelay: BTNode
    {
        [BTInput(typeof(Spell))]
        public string Spell;
        
        [BTInput(typeof(Unit))]
        public string Target;

        public int Speed;
    }
}