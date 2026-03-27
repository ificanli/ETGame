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
                    interpolationComponent.PositionPredictionDirection = interpolationComponent.PredictedDirection;
                    interpolationComponent.PositionPredictionSpeed = interpolationComponent.PredictedSpeed;
                    interpolationComponent.PositionPredictionBlocked = false;
                    interpolationComponent.LastAuthoritativeSyncTime = TimeInfo.Instance.ClientNow();

                    interpolationComponent.ApplyAuthoritativePosition(unit.Position);
                    await ETTask.CompletedTask;
                    return;
                }

                if (interpolationComponent.PredictionEnabled)
                {
                    if (interpolationComponent.SkipNextChangePositionSync)
                    {
                        interpolationComponent.SkipNextChangePositionSync = false;
                        await ETTask.CompletedTask;
                        return;
                    }

                    JoystickMoveAuthorityStateComponent authorityState = unit.GetComponent<JoystickMoveAuthorityStateComponent>();
                    if (unit.IsMyUnit())
                    {
                        GameObjectComponent currentGameObjectComponent = unit.GetComponent<GameObjectComponent>();
                        Vector3 viewPosition = currentGameObjectComponent?.Transform != null
                            ? currentGameObjectComponent.Transform.position
                            : unit.Position;
                        Log.Info(
                            $"[NavMove][ChangePosBefore] unitId={unit.Id}, unitPos={unit.Position}, viewPos={viewPosition}, authPos={interpolationComponent.AuthoritativePosition}, targetPos={interpolationComponent.TargetPosition}, predictedDelta={interpolationComponent.PredictedDelta}, visualCorrection={interpolationComponent.VisualCorrection}, predictedSpeed={interpolationComponent.PredictedSpeed:F3}, constrainedSpeed={interpolationComponent.PositionPredictionSpeed:F3}, blocked={interpolationComponent.PositionPredictionBlocked}, hold={interpolationComponent.HoldLocalPredictionOnStationarySync}, authoritySpeed={authorityState?.LastSpeed ?? -1f:F3}, authorityDir={authorityState?.LastDirection.ToString() ?? "null"}, ackInputSeq={authorityState?.LastProcessedInputSequence ?? 0}");
                    }

                    InputSystemComponent inputSystemComponent = unit.GetComponent<InputSystemComponent>();
                    if (unit.IsMyUnit() &&
                        inputSystemComponent != null &&
                        inputSystemComponent.TryReconcileAuthoritativeMove(unit, interpolationComponent, unit.Position,
                            authorityState?.LastProcessedInputSequence ?? 0))
                    {
                        Log.Info(
                            $"[NavMove][ChangePosAfter] unitId={unit.Id}, unitPos={unit.Position}, authPos={interpolationComponent.AuthoritativePosition}, targetPos={interpolationComponent.TargetPosition}, predictedDelta={interpolationComponent.PredictedDelta}, visualCorrection={interpolationComponent.VisualCorrection}, predictedSpeed={interpolationComponent.PredictedSpeed:F3}, constrainedSpeed={interpolationComponent.PositionPredictionSpeed:F3}, blocked={interpolationComponent.PositionPredictionBlocked}, hold={interpolationComponent.HoldLocalPredictionOnStationarySync}, ackInputSeq={authorityState?.LastProcessedInputSequence ?? 0}");
                        await ETTask.CompletedTask;
                        return;
                    }

                    if (unit.IsMyUnit() &&
                        authorityState != null &&
                        authorityState.LastSpeed <= 0.01f &&
                        math.lengthsq(authorityState.LastDirection) <= 0.0001f)
                    {
                        interpolationComponent.ClearLocalMotionOnAuthoritativeStop();
                    }

                    interpolationComponent.ResolveLocalPositionPrediction(unit, unit.Position);
                    interpolationComponent.ApplyAuthoritativePosition(unit.Position);
                    if (unit.IsMyUnit())
                    {
                        Log.Info(
                            $"[NavMove][ChangePosAfter] unitId={unit.Id}, unitPos={unit.Position}, authPos={interpolationComponent.AuthoritativePosition}, targetPos={interpolationComponent.TargetPosition}, predictedDelta={interpolationComponent.PredictedDelta}, visualCorrection={interpolationComponent.VisualCorrection}, predictedSpeed={interpolationComponent.PredictedSpeed:F3}, constrainedSpeed={interpolationComponent.PositionPredictionSpeed:F3}, blocked={interpolationComponent.PositionPredictionBlocked}, hold={interpolationComponent.HoldLocalPredictionOnStationarySync}, ackInputSeq={authorityState?.LastProcessedInputSequence ?? 0}");
                    }
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
