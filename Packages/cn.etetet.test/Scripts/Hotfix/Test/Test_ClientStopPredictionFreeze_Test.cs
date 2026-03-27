using System;
using System.IO;
using System.Reflection;
using ET.Client;
using Unity.Mathematics;

namespace ET.Test
{
    public class Test_ClientStopPredictionFreeze_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_ClientStopPredictionFreeze_Test));
            Fiber testFiber = scope.TestFiber;

            Fiber robot = await TestHelper.CreateRobot(testFiber, nameof(Test_ClientStopPredictionFreeze_Test));
            Scene clientRoot = robot.Root;
            if (clientRoot == null || clientRoot.IsDisposed)
            {
                Log.Console("client root is null");
                return 1;
            }

            Scene currentScene = clientRoot.CurrentScene();
            if (currentScene == null || currentScene.IsDisposed)
            {
                Log.Console("current scene is null");
                return 2;
            }

            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (unit == null || unit.IsDisposed)
            {
                Log.Console("my unit is null");
                return 3;
            }

            Type interpolationType = GetRequiredType("ET.Client.UnitViewInterpolationComponent");
            Type interpolationSystemType = GetRequiredType("ET.Client.UnitViewInterpolationComponentSystem");
            Entity interpolation = unit.GetComponent(interpolationType) ?? unit.AddComponent(interpolationType);
            if (interpolation == null || interpolation.IsDisposed)
            {
                Log.Console("interpolation component is null");
                return 4;
            }

            SetField(interpolation, "PredictionEnabled", true);
            SetField(interpolation, "AuthoritativePosition", CreateVector3(interpolationType, 1f, 0.2f, 2f));
            SetField(interpolation, "VisualCorrection", CreateVector3(interpolationType, 0.2f, 0f, -0.1f));
            SetField(interpolation, "PredictedDelta", CreateVector3(interpolationType, 0.5f, 0f, 0.3f));
            SetField(interpolation, "PredictedDirection", CreateVector3(interpolationType, 0f, 0f, 1f));
            SetField(interpolation, "PredictedSpeed", 3f);
            SetField(interpolation, "PositionPredictionDirection", CreateVector3(interpolationType, 0f, 0f, 1f));
            SetField(interpolation, "PositionPredictionSpeed", 3f);
            SetField(interpolation, "PositionPredictionBlocked", true);
            SetField(interpolation, "HoldLocalPredictionOnStationarySync", true);
            SetField(interpolation, "WallBlockedCount", 2);

            float3 currentViewPosition = new float3(1.35f, 0.2f, 2.18f);
            InvokeStatic(interpolationSystemType, "FreezePredictionOnLocalStop", interpolation, currentViewPosition);

            float predictedSpeed = GetField<float>(interpolation, "PredictedSpeed");
            float constrainedSpeed = GetField<float>(interpolation, "PositionPredictionSpeed");
            if (predictedSpeed > 0.01f || constrainedSpeed > 0.01f)
            {
                Log.Console($"local stop did not clear speeds: predicted={predictedSpeed}, constrained={constrainedSpeed}");
                return 5;
            }

            bool blocked = GetField<bool>(interpolation, "PositionPredictionBlocked");
            bool hold = GetField<bool>(interpolation, "HoldLocalPredictionOnStationarySync");
            int wallBlockedCount = GetField<int>(interpolation, "WallBlockedCount");
            if (blocked || hold || wallBlockedCount != 0)
            {
                Log.Console($"local stop did not clear state: blocked={blocked}, hold={hold}, wallBlockedCount={wallBlockedCount}");
                return 6;
            }

            float3 targetPosition = ReadVector3(interpolation, "TargetPosition");
            if (math.lengthsq(targetPosition - currentViewPosition) > 0.000001f)
            {
                Log.Console($"local stop target mismatch: expected={currentViewPosition}, actual={targetPosition}");
                return 7;
            }

            SetField(interpolation, "AuthoritativePosition", CreateVector3(interpolationType, 2f, 0.2f, 4f));
            SetField(interpolation, "VisualCorrection", CreateVector3(interpolationType, 0.3f, 0f, 0.1f));
            SetField(interpolation, "PredictedDelta", CreateVector3(interpolationType, 0.4f, 0f, 0.2f));
            SetField(interpolation, "PredictedDirection", CreateVector3(interpolationType, 1f, 0f, 0f));
            SetField(interpolation, "PredictedSpeed", 3f);
            SetField(interpolation, "PositionPredictionDirection", CreateVector3(interpolationType, 1f, 0f, 0f));
            SetField(interpolation, "PositionPredictionSpeed", 3f);
            SetField(interpolation, "PositionPredictionBlocked", true);
            SetField(interpolation, "HoldLocalPredictionOnStationarySync", true);
            SetField(interpolation, "WallBlockedCount", 3);
            SetField(interpolation, "TargetPosition", CreateVector3(interpolationType, 2.7f, 0.2f, 4.3f));

            InvokeStatic(interpolationSystemType, "ClearLocalMotionOnAuthoritativeStop", interpolation);
            InvokeStatic(interpolationSystemType, "ResolveLocalPositionPrediction", interpolation, unit, new float3(2.1f, 0.2f, 4.05f));
            InvokeStatic(interpolationSystemType, "ApplyAuthoritativePosition", interpolation, new float3(2.1f, 0.2f, 4.05f));

            predictedSpeed = GetField<float>(interpolation, "PredictedSpeed");
            constrainedSpeed = GetField<float>(interpolation, "PositionPredictionSpeed");
            if (predictedSpeed > 0.01f || constrainedSpeed > 0.01f)
            {
                Log.Console($"authoritative stop did not clear speeds: predicted={predictedSpeed}, constrained={constrainedSpeed}");
                return 8;
            }

            float3 predictedDelta = ReadVector3(interpolation, "PredictedDelta");
            float3 visualCorrection = ReadVector3(interpolation, "VisualCorrection");
            if (math.lengthsq(predictedDelta) > 0.000001f || math.lengthsq(visualCorrection) > 0.000001f)
            {
                Log.Console($"authoritative stop kept residual correction: predictedDelta={predictedDelta}, visualCorrection={visualCorrection}");
                return 9;
            }

            float3 authoritativeStopPosition = new float3(2.1f, 0.2f, 4.05f);
            targetPosition = ReadVector3(interpolation, "TargetPosition");
            if (math.lengthsq(targetPosition - authoritativeStopPosition) > 0.000001f)
            {
                Log.Console($"authoritative stop target mismatch: expected={authoritativeStopPosition}, actual={targetPosition}");
                return 10;
            }

            Log.Console("client stop prediction freeze passed");
            return ErrorCode.ERR_Success;
        }

        private static Type GetRequiredType(string fullTypeName)
        {
            Type type = Type.GetType(fullTypeName);
            if (type != null)
            {
                return type;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            string[] candidateAssemblies =
            {
                "ET.ModelView",
                "ET.HotfixView",
                "ET.Model",
                "ET.Hotfix",
            };
            foreach (string assemblyName in candidateAssemblies)
            {
                try
                {
                    Assembly assembly = Assembly.Load(assemblyName);
                    type = assembly?.GetType(fullTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                }
            }

            string[] candidatePaths =
            {
                Path.Combine(Environment.CurrentDirectory, "Temp", "Bin", "Debug", "ET.ModelView", "ET.ModelView.dll"),
                Path.Combine(Environment.CurrentDirectory, "Temp", "Bin", "Debug", "ET.HotfixView", "ET.HotfixView.dll"),
            };
            foreach (string assemblyPath in candidatePaths)
            {
                try
                {
                    if (!File.Exists(assemblyPath))
                    {
                        continue;
                    }

                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    type = assembly?.GetType(fullTypeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                }
            }

            throw new Exception($"type not found: {fullTypeName}");
        }

        private static object CreateVector3(Type interpolationType, float x, float y, float z)
        {
            FieldInfo fieldInfo = interpolationType.GetField("TargetPosition");
            Type vectorType = fieldInfo?.FieldType;
            if (vectorType == null)
            {
                throw new Exception("TargetPosition field not found");
            }

            return Activator.CreateInstance(vectorType, x, y, z);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName);
            if (fieldInfo == null)
            {
                throw new Exception($"field not found: {fieldName}");
            }

            fieldInfo.SetValue(instance, value);
        }

        private static T GetField<T>(object instance, string fieldName)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName);
            if (fieldInfo == null)
            {
                throw new Exception($"field not found: {fieldName}");
            }

            return (T)fieldInfo.GetValue(instance);
        }

        private static float3 ReadVector3(object instance, string fieldName)
        {
            FieldInfo fieldInfo = instance.GetType().GetField(fieldName);
            if (fieldInfo == null)
            {
                throw new Exception($"field not found: {fieldName}");
            }

            object value = fieldInfo.GetValue(instance);
            if (value == null)
            {
                return float3.zero;
            }

            Type vectorType = value.GetType();
            float x = Convert.ToSingle(vectorType.GetField("x")?.GetValue(value) ?? 0f);
            float y = Convert.ToSingle(vectorType.GetField("y")?.GetValue(value) ?? 0f);
            float z = Convert.ToSingle(vectorType.GetField("z")?.GetValue(value) ?? 0f);
            return new float3(x, y, z);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (methodInfo.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters.Length != args.Length)
                {
                    continue;
                }

                object[] invokeArgs = new object[args.Length];
                for (int i = 0; i < args.Length; ++i)
                {
                    invokeArgs[i] = ConvertArgument(args[i], parameters[i].ParameterType);
                }

                return methodInfo.Invoke(null, invokeArgs);
            }

            throw new Exception($"method not found: {type.FullName}.{methodName}/{args.Length}");
        }

        private static object ConvertArgument(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            if (targetType.FullName == "Unity.Mathematics.float3")
            {
                float3 source = (float3)value;
                return Activator.CreateInstance(targetType, source.x, source.y, source.z);
            }

            return value;
        }
    }
}
