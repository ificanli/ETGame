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

                // 本机单位：客户端权威模式下，收到服务器纠正时重置预测
                if (interpolation.PredictionEnabled)
                {
                    // 纠正检测：如果新位置与预测位置差距大，说明是服务器纠正
                    Vector3 diff = (Vector3)unit.Position - interpolation.LocalPredictedPosition;
                    if (diff.sqrMagnitude > 0.01f)
                    {
                        // 服务器纠正：重置预测位置
                        interpolation.ResetPrediction(unit.Position);
                    }
                    else
                    {
                        interpolation.ApplyAuthoritativePosition(unit.Position);
                    }
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
