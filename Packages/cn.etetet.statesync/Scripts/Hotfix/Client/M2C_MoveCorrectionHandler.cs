using Unity.Mathematics;

namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_MoveCorrectionHandler : MessageHandler<Scene, M2C_MoveCorrection>
    {
        protected override async ETTask Run(Scene root, M2C_MoveCorrection message)
        {
            Scene currentScene = root.CurrentScene();
            if (currentScene == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            Unit myUnit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (myUnit == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            float3 correctedPos = new float3(message.PosX, message.PosY, message.PosZ);
            Log.Warning($"[MoveCorrection] Server corrected position to ({correctedPos.x:F2},{correctedPos.y:F2},{correctedPos.z:F2}), ackSeq={message.AckSequence}");
            myUnit.Position = correctedPos;

            await ETTask.CompletedTask;
        }
    }
}
