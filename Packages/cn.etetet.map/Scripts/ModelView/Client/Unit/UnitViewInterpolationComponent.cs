using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class UnitViewInterpolationComponent : Entity, IAwake, IUpdate, IDestroy
    {
        // --- 保留字段 ---
        public Vector3 TargetPosition;
        public bool Initialized;
        public bool PredictionEnabled;
        public Vector3 AuthoritativePosition;
        public long LastAuthoritativeSyncTime;

        // --- 新增字段 ---
        public Vector3 LocalPredictedPosition;
        public Vector3 LocalMoveDirection;
        public float LocalMoveSpeed;
        public long LastDiagnosticLogTime;
    }
}
