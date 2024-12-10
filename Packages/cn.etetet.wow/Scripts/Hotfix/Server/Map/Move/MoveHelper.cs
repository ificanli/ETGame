using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public static partial class MoveHelper
    {
        // 可以多次调用，多次调用的话会取消上一次的协程
        public static async ETTask FindPathMoveToAsync(this Unit unit, float3 target, int turnTime = 100)
        {
            float speed = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Speed);
            if (speed < 0.01)
            {
                unit.SendStop(2);
                return;
            }

            M2C_PathfindingResult m2CPathfindingResult = M2C_PathfindingResult.Create();
            unit.GetComponent<PathfindingComponent>().Find(unit.Position, target, m2CPathfindingResult.Points);

            if (m2CPathfindingResult.Points.Count < 2)
            {
                unit.SendStop(3);
                return;
            }
                
            // 广播寻路路径
            m2CPathfindingResult.Id = unit.Id;
            MapMessageHelper.NoticeClient(unit, m2CPathfindingResult, NoticeType.Broadcast);

            MoveComponent moveComponent = unit.GetComponent<MoveComponent>();
            
            bool ret = await moveComponent.MoveToAsync(m2CPathfindingResult.Points, speed, turnTime);
            if (ret) // 如果返回false，说明被其它移动取消了，这时候不需要通知客户端stop
            {
                unit.SendStop(0);
            }
        }

        public static void Stop(this Unit unit, int error)
        {
            unit.GetComponent<MoveComponent>().Stop(error == 0);
            unit.SendStop(error);
        }

        // error: 0表示协程走完正常停止
        public static void SendStop(this Unit unit, int error)
        {
            M2C_Stop m2CStop = M2C_Stop.Create();
            m2CStop.Error = error;
            m2CStop.Id = unit.Id;
            m2CStop.Position = unit.Position;
            m2CStop.Rotation = unit.Rotation;
            
            MapMessageHelper.NoticeClient(unit, m2CStop, NoticeType.Broadcast);
        }

        public static void Turn(this Unit unit, quaternion to, int turnTime)
        {
            TurnComponent turnComponent = unit.GetComponent<TurnComponent>();
            turnComponent.Turn(to, turnTime);
            M2C_Turn m2CTurn = M2C_Turn.Create();
            m2CTurn.UnitId = unit.Id;
            m2CTurn.Rotation = to;
            m2CTurn.TurnTime = turnTime;
            MapMessageHelper.NoticeClient(unit, m2CTurn, NoticeType.Broadcast);
        }
    }
}