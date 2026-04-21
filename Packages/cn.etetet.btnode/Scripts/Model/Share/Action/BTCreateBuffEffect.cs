using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    public class BTCreateBuffEffect: BTNode
    {
        [BTInput(typeof(Unit))]
        public string Unit;
        
        [BTInput(typeof(Buff))]
        public string Buff;
        
        public BindPoint BindPoint;

        public OdinUnityObject Effect;

        public int Duration = 5000;

        public bool FollowUnit;

        public float LocalPositionOffsetX;

        public float LocalPositionOffsetY;

        public float LocalPositionOffsetZ;

        public float LocalEulerAnglesOffsetX;

        public float LocalEulerAnglesOffsetY;

        public float LocalEulerAnglesOffsetZ;

        public bool SyncLineRegionLengthFromSpellTarget;

        public float LineRegionMaxLength;

        public float LineRegionLengthOffset;

        public bool AnimateLineRegionFillByDuration;
    }
}
