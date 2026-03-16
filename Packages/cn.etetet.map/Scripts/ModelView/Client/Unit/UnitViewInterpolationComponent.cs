using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class UnitViewInterpolationComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public Vector3 TargetPosition;
        public bool Initialized;
        public bool PredictionEnabled;
        public bool SkipNextChangePositionSync;
        public Vector3 AuthoritativePosition;
        public Vector3 PredictedDelta;
        public Vector3 VisualCorrection;
        public Vector3 PredictedDirection;
        public float PredictedSpeed;
    }
}
