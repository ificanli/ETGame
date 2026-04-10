using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class ChangePosition_SyncGameObjectPos: AEvent<Scene, ChangePosition>
    {
        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                return;
            }

            UnitViewInterpolationComponent interpolation = unit.GetComponent<UnitViewInterpolationComponent>();
            if (interpolation != null)
            {
                if (!unit.IsMyUnit())
                {
                    // 远端单位：更新权威位置 + 外推方向/速度
                    Vector3 delta = (Vector3)unit.Position - interpolation.AuthoritativePosition;
                    float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
                    bool isMoving = delta.sqrMagnitude > 0.0001f && speed > 0.01f;

                    interpolation.PredictionEnabled = true;
                    interpolation.LocalMoveDirection = isMoving ? delta.normalized : Vector3.zero;
                    interpolation.LocalMoveSpeed = isMoving ? speed : 0f;
                    interpolation.LocalPredictedPosition = unit.Position;
                    interpolation.ApplyAuthoritativePosition(unit.Position);
                    await ETTask.CompletedTask;
                    return;
                }

                // 本机单位：只更新权威位置，不做 reconcile
                if (interpolation.PredictionEnabled)
                {
                    interpolation.ApplyAuthoritativePosition(unit.Position);
                    await ETTask.CompletedTask;
                    return;
                }

                interpolation.SetTargetPosition(unit.Position);
                await ETTask.CompletedTask;
                return;
            }

            gameObjectComponent.Transform.position = unit.Position;
            await ETTask.CompletedTask;
        }
    }
}
