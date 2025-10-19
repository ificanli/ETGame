using System;
using MongoDB.Driver;

namespace ET.Server
{
    [FriendOf(typeof(DBManagerComponent))]
    public static partial class DBManagerComponentSystem
    {
        public static async ETTask DropDB(this DBManagerComponent self, int zone)
        {
            StartZoneConfig startZoneConfig = StartZoneConfigCategory.Instance.Get(zone);
            if (startZoneConfig.DBName == "")
            {
                return;
            }
            MongoClient mongoClient = new(startZoneConfig.DBConnection);
            await mongoClient.DropDatabaseAsync(startZoneConfig.DBName);
            await ETTask.CompletedTask;
        }
        
        public static DBComponent GetZoneDB(this DBManagerComponent self, int zone)
        {
            DBComponent dbComponent = self.GetChild<DBComponent>(zone);
            if (dbComponent != null)
            {
                return dbComponent;
            }

            StartZoneConfig startZoneConfig = StartZoneConfigCategory.Instance.Get(zone);
            if (startZoneConfig.DBConnection == "")
            {
                throw new Exception($"zone: {zone} not found mongo connect string");
            }

            dbComponent = self.AddChildWithId<DBComponent, string, string>(zone, startZoneConfig.DBConnection, startZoneConfig.DBName);
            return dbComponent;
        }
    }
}