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
            HashSet<long> sentViewerIds = new HashSet<long>();
            
            foreach (AOIEntity u in dict.Values)
            {
                if (!TryGetViewer(unit, u?.Unit, withSelf, out Unit viewer))
                {
                    continue;
                }

                if (!sentViewerIds.Add(viewer.Id))
                {
                    continue;
                }

                UnitGateInfoComponent gateInfo = viewer.GetComponent<UnitGateInfoComponent>();
                if (gateInfo == null)
                {
                    continue;
                }

                messageSender.Send(gateInfo.ActorId, message);
            }

            ExtraUnitVisibilityComponent extraVisibility = unit.Scene()?.GetComponent<ExtraUnitVisibilityComponent>();
            HashSet<long> extraViewerIds = extraVisibility?.GetExtraViewerIds(unit.Id);
            if (extraViewerIds == null || extraViewerIds.Count == 0)
            {
                return;
            }

            UnitComponent unitComponent = unit.Scene()?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            foreach (long viewerId in extraViewerIds)
            {
                Unit viewer = unitComponent.Get(viewerId);
                if (!TryGetViewer(unit, viewer, withSelf, out viewer))
                {
                    continue;
                }

                if (!sentViewerIds.Add(viewer.Id))
                {
                    continue;
                }

                UnitGateInfoComponent gateInfo = viewer.GetComponent<UnitGateInfoComponent>();
                if (gateInfo == null)
                {
                    continue;
                }

                messageSender.Send(gateInfo.ActorId, message);
            }
        }
        
        private static void SendToClient(Unit unit, IMessage message)
        {
            if (!unit.UnitType.IsSame(UnitType.Player))
            {
                return;
            }

            UnitGateInfoComponent gateInfo = unit.GetComponent<UnitGateInfoComponent>();
            if (gateInfo == null)
            {
                return;
            }

            MessageSender messageSender = unit.Root().GetComponent<MessageSender>();
            messageSender.Send(gateInfo.ActorId, message);
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

        private static bool TryGetViewer(Unit sourceUnit, Unit viewer, bool withSelf, out Unit result)
        {
            result = null;

            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
            {
                return false;
            }

            if (!withSelf && viewer.Id == sourceUnit.Id)
            {
                return false;
            }

            ExtraUnitVisibilityComponent extraVisibility = sourceUnit.Scene()?.GetComponent<ExtraUnitVisibilityComponent>();
            if (extraVisibility != null && extraVisibility.IsTargetConcealed(viewer.Id, sourceUnit.Id))
            {
                return false;
            }

            result = viewer;
            return true;
        }
    }
}
