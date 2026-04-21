using Unity.Mathematics;

namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_MoveStateHandler : MessageLocationHandler<Unit, C2M_MoveState>
    {
        protected override async ETTask Run(Unit unit, C2M_MoveState message)
        {
            MoveValidationComponent validation = unit.GetComponent<MoveValidationComponent>();
            if (validation == null)
            {
                validation = unit.AddComponent<MoveValidationComponent>();
            }

            float3 clientPos = new float3(message.PosX, message.PosY, message.PosZ);
            bool isValid = validation.Validate(clientPos, message.Speed, message.Sequence, message.ClientTimeMs, out float3 correctedPos);

            if (isValid)
            {
                unit.Position = clientPos;
                if (message.Speed > 0.01f && (message.DirX != 0f || message.DirZ != 0f))
                {
                    float3 dir = new float3(message.DirX, 0f, message.DirZ);
                    unit.Rotation = quaternion.LookRotationSafe(dir, math.up());
                }

                M2C_MoveSync syncMsg = M2C_MoveSync.Create(true);
                syncMsg.UnitId = unit.Id;
                syncMsg.PosX = clientPos.x;
                syncMsg.PosY = clientPos.y;
                syncMsg.PosZ = clientPos.z;
                syncMsg.DirX = message.DirX;
                syncMsg.DirZ = message.DirZ;
                syncMsg.Speed = message.Speed;
                syncMsg.Sequence = message.Sequence;
                MapMessageHelper.NoticeClient(unit, syncMsg, NoticeType.BroadcastWithoutSelf);
            }
            else
            {
                M2C_MoveCorrection correction = M2C_MoveCorrection.Create(true);
                correction.PosX = correctedPos.x;
                correction.PosY = correctedPos.y;
                correction.PosZ = correctedPos.z;
                correction.AckSequence = message.Sequence;
                MapMessageHelper.NoticeClient(unit, correction, NoticeType.Self);

                M2C_MoveSync syncMsg = M2C_MoveSync.Create(true);
                syncMsg.UnitId = unit.Id;
                syncMsg.PosX = correctedPos.x;
                syncMsg.PosY = correctedPos.y;
                syncMsg.PosZ = correctedPos.z;
                syncMsg.DirX = 0f;
                syncMsg.DirZ = 0f;
                syncMsg.Speed = 0f;
                syncMsg.Sequence = message.Sequence;
                MapMessageHelper.NoticeClient(unit, syncMsg, NoticeType.BroadcastWithoutSelf);
            }

            await ETTask.CompletedTask;
        }
    }
}
