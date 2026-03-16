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

            UnitViewInterpolationComponent interpolationComponent = unit.GetComponent<UnitViewInterpolationComponent>();
            if (interpolationComponent != null)
            {
                // 远程单位：始终更新预测方向和速度（必须在 PredictionEnabled 判断之前，
                // 否则一旦 PredictionEnabled=true 后就再也走不到方向更新逻辑，导致停止时永远滑行）
                if (!unit.IsMyUnit())
                {
                    // 用 AuthoritativePosition 计算增量，避免 PredictedDelta/VisualCorrection 的干扰
                    Vector3 delta = (Vector3)unit.Position - interpolationComponent.AuthoritativePosition;
                    float speed = unit.NumericComponent?.GetAsFloat(NumericType.Speed) ?? 0f;
                    bool isMoving = delta.sqrMagnitude > 0.0001f && speed > 0.01f;

                    interpolationComponent.PredictionEnabled = true;
                    interpolationComponent.PredictedDirection = isMoving ? delta.normalized : Vector3.zero;
                    interpolationComponent.PredictedSpeed = isMoving ? speed : 0f;

                    interpolationComponent.ApplyAuthoritativePosition(unit.Position);
                    await ETTask.CompletedTask;
                    return;
                }

                // 本机单位的预测路径
                if (interpolationComponent.PredictionEnabled)
                {
                    if (interpolationComponent.SkipNextChangePositionSync)
                    {
                        interpolationComponent.SkipNextChangePositionSync = false;
                        await ETTask.CompletedTask;
                        return;
                    }

                    interpolationComponent.ApplyAuthoritativePosition(unit.Position);
                    await ETTask.CompletedTask;
                    return;
                }

                interpolationComponent.SetTargetPosition(unit.Position);
                await ETTask.CompletedTask;
                return;
            }

            gameObjectComponent.Transform.position = unit.Position;
            await ETTask.CompletedTask;
        }
    }
}
