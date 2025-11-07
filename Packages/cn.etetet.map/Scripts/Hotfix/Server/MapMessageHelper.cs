using System.Collections.Generic;
using System.IO;

namespace ET.Server
{
    public static partial class MapMessageHelper
    {
        public static void NoticeUnitAdd(Unit unit, Unit sendUnit)
        {
            M2C_CreateUnits createUnits = M2C_CreateUnits.Create();
            createUnits.Units.Add(UnitHelper.CreateUnitInfo(sendUnit));
            MapMessageHelper.SendToClient(unit, createUnits);
        }
        
        public static void NoticeUnitRemove(Unit unit, Unit sendUnit)
        {
            M2C_RemoveUnits removeUnits = M2C_RemoveUnits.Create();
            removeUnits.Units.Add(sendUnit.Id);
            MapMessageHelper.SendToClient(unit, removeUnits);
        }
        
        private static void Broadcast(Unit unit, IMessage message, bool withSelf = true)
        {
            (message as MessageObject).IsFromPool = false;
            Dictionary<long, EntityRef<AOIEntity>> dict = unit.GetBeSeePlayers();
            MessageSender messageSender = unit.Root().GetComponent<MessageSender>();
            
            foreach (AOIEntity u in dict.Values)
            {
                if (!withSelf && u.Id == unit.Id)
                {
                    continue;
                }
                messageSender.Send(u.Unit.GetComponent<UnitGateInfoComponent>().ActorId, message);
            }
        }
        
        private static void SendToClient(Unit unit, IMessage message)
        {
            if (!unit.UnitType.IsSame(UnitType.Player))
            {
                return;
            }
            MessageSender messageSender = unit.Root().GetComponent<MessageSender>();
            messageSender.Send(unit.GetComponent<UnitGateInfoComponent>().ActorId, message);
        }
        
        public static void NoticeClient(Unit unit, IMessage message, NoticeType noticeType)
        {
            switch (noticeType)
            {
                case NoticeType.Broadcast:
                    Broadcast(unit, message);
                    break;
                case NoticeType.Self:
                    SendToClient(unit, message);
                    break;
                case NoticeType.NoNotice:
                    break;
                case NoticeType.BroadcastWithoutSelf:
                    Broadcast(unit, message, false);
                    break;
            }
        }
    }
}