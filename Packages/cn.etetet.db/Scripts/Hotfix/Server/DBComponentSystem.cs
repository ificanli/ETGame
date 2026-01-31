using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ET.Server
{
	[EntitySystemOf(typeof(DBComponent))]
    public static partial class DBComponentSystem
    {
	    [EntitySystem]
	    private static void Awake(this DBComponent self, string dbConnection, string dbName)
		{
			self.mongoClient = new MongoClient(dbConnection);
			self.database = self.mongoClient.GetDatabase(dbName);
			self.GetCollectionNames();
		}

	    private static IMongoCollection<T> GetCollection<T>(this DBComponent self, string collection = null)
	    {
		    string name = collection ?? typeof (T).Name;
		    if (!self.CollectionDic.Contains(name))
		    {
			    throw new Exception($"数据库不存在表:{name}, 请在DBVersion中追加表");
		    }
		    return self.database.GetCollection<T>(collection ?? typeof (T).Name);
	    }

	    private static IMongoCollection<Entity> GetCollection(this DBComponent self, string name)
	    {
		    if (!self.CollectionDic.Contains(name))
		    {
			    throw new Exception($"数据库不存在表:{name}, 请在DBVersion中追加表");
		    }
		    return self.database.GetCollection<Entity>(name);
	    }
	    
	    public static void GetCollectionNames(this DBComponent self)
	    {
		    var list = self.database.ListCollectionNames().ToList();
		    foreach (string collection in list)
		    {
			    self.CollectionDic.Add(collection);
		    }
	    }
        
	    public static async ETTask CreateCollection<T>(this DBComponent self)
	    {
		    string name = typeof(T).Name;
		    if (self.CollectionDic.Contains(name))
		    {
			    Log.Error($"重复创建表:{name}");
			    return;
		    }

		    EntityRef<DBComponent> selfRef = self;
		    // Log.Error($"CreateDB <> {name}"); // 追踪 重复创建表
		    await self.database.CreateCollectionAsync(name);
		    self = selfRef;
		    self.CollectionDic.Add(name);
	    }
        
	    public static async ETTask CreateCollection(this DBComponent self, string name)
	    {
		    if (self.CollectionDic.Contains(name))
		    {
			    Log.Error($"重复创建表:{name}");
			    return;
		    }
		    // Log.Error($"CreateDB () {name}"); // 追踪 重复创建表
		    
		    EntityRef<DBComponent> selfRef = self;
		    await self.database.CreateCollectionAsync(name);
		    self = selfRef;
		    self.CollectionDic.Add(name);
	    }
	    
	    #region Count

	    public static async ETTask<long> Count<T>(this DBComponent self, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    return await self.GetCollection<T>(collection).CountDocumentsAsync(d => true);
		    }
	    }

	    public static async ETTask<long> Count<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    { 
			    return await self.GetCollection<T>(collection).CountDocumentsAsync(filter);
		    }
	    }

	    #endregion
	    
	    #region Exist

	    public static async ETTask<bool> Exist<T>(this DBComponent self, string collection = null) where T : Entity
	    {
		    long count = await self.Count<T>(collection);

		    return count > 0;
	    }

	    public static async ETTask<bool> Exist<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null) where T : Entity
	    {
		    long count = await self.Count(filter, collection);

		    return count > 0;
	    }
        
	    // //是否存在表
	    public static async ETTask<bool> CollectionExistsAsync(this DBComponent self, string collectionName)
	    {
		    var filter = new BsonDocument("name", collectionName);
		    //filter by collection name
		    var collections = await self.database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
		    //check for existence
		    return await collections.AnyAsync();
	    }

	    #endregion
	    
	    #region Query

	    public static async ETTask<T> Query<T>(this DBComponent self, long id, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, id % DBComponent.TaskCount))
		    {
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(d => d.Id == id);
			    
			    return await cursor.FirstOrDefaultAsync();
		    }
	    }
	    
	    public static async ETTask<List<T>> Query<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null)
			    where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filter);

			    return await cursor.ToListAsync();
		    }
	    }

	    public static async ETTask<List<T>> Query<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter, string collection = null)
			    where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filter);

			    return await cursor.ToListAsync();
		    }
	    }
	    
	    public static async ETTask Query(this DBComponent self, long id, List<string> collectionNames, List<Entity> result)
	    {
		    if (collectionNames == null || collectionNames.Count == 0)
		    {
			    return;
		    }

		    EntityRef<DBComponent> selfRef = self;
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, id % DBComponent.TaskCount))
		    {
			    foreach (string collectionName in collectionNames)
			    {
				    self = selfRef;
				    IAsyncCursor<Entity> cursor = await self.GetCollection(collectionName).FindAsync(d => d.Id == id);

				    Entity e = await cursor.FirstOrDefaultAsync();

				    if (e == null)
				    {
					    continue;
				    }

				    result.Add(e);
			    }
		    }
	    }

	    public static async ETTask<List<T>> QueryJson<T>(this DBComponent self, string json, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filterDefinition);
			    return await cursor.ToListAsync();
		    }
	    }

	    public static async ETTask<List<T>> QueryJson<T>(this DBComponent self, long taskId, string json, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    FilterDefinition<T> filterDefinition = new JsonFilterDefinition<T>(json);
			    IAsyncCursor<T> cursor = await self.GetCollection<T>(collection).FindAsync(filterDefinition);
			    return await cursor.ToListAsync();
		    }
	    }

	    #endregion

	    #region Insert

	    public static async ETTask InsertBatch<T>(this DBComponent self, IEnumerable<T> list, string collection = null) where T: Entity
	    {
		    if (collection == null)
		    {
			    collection = typeof (T).Name;
		    }
		    
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    await self.GetCollection(collection).InsertManyAsync(list);
		    }
	    }

	    #endregion

	    #region Save

	    public static async ETTask Save<T>(this DBComponent self, T entity, string collection = null) where T : Entity
	    {
		    if (entity == null)
		    {
			    Log.Error($"save entity is null: {typeof (T).Name}");

			    return;
		    }
		    
		    if (collection == null)
		    {
			    collection = entity.GetType().Name;
		    }

		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, entity.Id % DBComponent.TaskCount))
		    {
			    await self.GetCollection(collection).ReplaceOneAsync(d => d.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = true });
		    }
	    }

	    public static async ETTask Save<T>(this DBComponent self, long taskId, T entity, string collection = null) where T : Entity
	    {
		    if (entity == null)
		    {
			    Log.Error($"save entity is null: {typeof (T).Name}");

			    return;
		    }

		    if (collection == null)
		    {
			    collection = entity.GetType().Name;
		    }

		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    await self.GetCollection(collection).ReplaceOneAsync(d => d.Id == entity.Id, entity, new ReplaceOptions { IsUpsert = true });
		    }
	    }

	    public static async ETTask Save(this DBComponent self, long id, List<Entity> entities)
	    {
		    if (entities == null)
		    {
			    Log.Error($"save entity is null");
			    return;
		    }

		    EntityRef<DBComponent> selfRef = self;
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, id % DBComponent.TaskCount))
		    {
			    foreach (Entity entity in entities)
			    {
				    if (entity == null)
				    {
					    continue;
				    }

				    self = selfRef;
				    long entityId = entity.Id;
				    await self.GetCollection(entity.GetType().Name).ReplaceOneAsync(d => d.Id == entityId, entity, new ReplaceOptions { IsUpsert = true });
			    }
		    }
	    }

	    public static async ETTask SaveNotWait<T>(this DBComponent self, T entity, long taskId = 0, string collection = null) where T : Entity
	    {
		    if (taskId == 0)
		    {
			    await self.Save(entity, collection);

			    return;
		    }

		    await self.Save(taskId, entity, collection);
	    }

	    #endregion

	    #region Remove
	    
	    public static async ETTask<long> Remove<T>(this DBComponent self, long id, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, id % DBComponent.TaskCount))
		    {
			    DeleteResult result = await self.GetCollection<T>(collection).DeleteOneAsync(d => d.Id == id);

			    return result.DeletedCount;
		    }
	    }

	    public static async ETTask<long> Remove<T>(this DBComponent self, long taskId, long id, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    DeleteResult result = await self.GetCollection<T>(collection).DeleteOneAsync(d => d.Id == id);

			    return result.DeletedCount;
		    }
	    }

	    public static async ETTask<long> Remove<T>(this DBComponent self, Expression<Func<T, bool>> filter, string collection = null) where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, RandomGenerator.RandInt64() % DBComponent.TaskCount))
		    {
			    DeleteResult result = await self.GetCollection<T>(collection).DeleteManyAsync(filter);

			    return result.DeletedCount;
		    }
	    }

	    public static async ETTask<long> Remove<T>(this DBComponent self, long taskId, Expression<Func<T, bool>> filter, string collection = null)
			    where T : Entity
	    {
		    using (await self.Root().CoroutineLockComponent.Wait(CoroutineLockType.MongoDB, taskId % DBComponent.TaskCount))
		    {
			    DeleteResult result = await self.GetCollection<T>(collection).DeleteManyAsync(filter);

			    return result.DeletedCount;
		    }
	    }

	    #endregion

	    #region Index

	    public static async ETTask CreateIndex<T>(this DBComponent self, params IndexKeysDefinition<T>[] keys) where T : Entity
	    {
		    if (keys == null)
		    {
			    return;
		    }

		    var indexModels = new List<CreateIndexModel<T>>();

		    foreach (var indexKeysDefinition in keys)
		    {
			    indexModels.Add(new CreateIndexModel<T>(indexKeysDefinition));
		    }

		    await self.GetCollection<T>().Indexes.CreateManyAsync(indexModels);
	    }

	    public static async ETTask CreateIndex<T>(this DBComponent self, string collection, params IndexKeysDefinition<T>[] keys) where T : Entity
	    {
		    if (keys == null)
		    {
			    return;
		    }

		    var indexModels = new List<CreateIndexModel<T>>();

		    foreach (var indexKeysDefinition in keys)
		    {
			    indexModels.Add(new CreateIndexModel<T>(indexKeysDefinition));
		    }

		    await self.GetCollection<T>(collection).Indexes.CreateManyAsync(indexModels);
	    }

	    public static async ETTask CreateIndex<T>(this DBComponent self, CreateIndexOptions indexOptions, params IndexKeysDefinition<T>[] keys) where T : Entity
	    {
		    if (keys == null)
		    {
			    return;
		    }

		    var indexModels = new List<CreateIndexModel<T>>();

		    foreach (var indexKeysDefinition in keys)
		    {
			    indexModels.Add(new CreateIndexModel<T>(indexKeysDefinition, indexOptions));
		    }

		    await self.GetCollection<T>().Indexes.CreateManyAsync(indexModels);
	    }

	    #endregion
    }
}