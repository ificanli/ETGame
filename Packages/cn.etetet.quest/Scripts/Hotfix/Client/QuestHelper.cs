namespace ET.Client
{
    /// <summary>
    /// 客户端任务帮助类
    /// </summary>
    public static class QuestHelper
    {
        /// <summary>
        /// 接取任务
        /// </summary>
        public static async ETTask<bool> AcceptQuest(Scene scene, long questId)
        {
            try
            {
                // 在await前创建EntityRef
                EntityRef<Scene> sceneRef = scene;
                
                C2M_AcceptQuest request = C2M_AcceptQuest.Create();
                request.QuestId = questId;

                M2C_AcceptQuest response = (M2C_AcceptQuest)await scene.GetComponent<ClientSenderComponent>().Call(request);
                
                if (response.Error != 0)
                {
                    Log.Error($"接取任务失败: {response.Message}");
                    return false;
                }

                // await后重新获取Scene
                scene = sceneRef;

                // 更新客户端任务数据 - 任务接取成功，状态为进行中
                QuestComponent questComponent = scene.GetComponent<QuestComponent>();
                if (questComponent != null)
                {
                    questComponent.UpdateQuestData(questId, QuestStatus.InProgress);
                }

                Log.Debug($"成功接取任务: {questId}");
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"接取任务请求失败: {e}");
                return false;
            }
        }

        /// <summary>
        /// 提交任务
        /// </summary>
        public static async ETTask<bool> SubmitQuest(Scene scene, long questId)
        {
            try
            {
                // 在await前创建EntityRef
                EntityRef<Scene> sceneRef = scene;
                
                C2M_SubmitQuest request = C2M_SubmitQuest.Create();
                request.QuestId = questId;

                M2C_SubmitQuest response = (M2C_SubmitQuest)await scene.GetComponent<ClientSenderComponent>().Call(request);
                
                if (response.Error != 0)
                {
                    Log.Error($"提交任务失败: {response.Message}");
                    return false;
                }

                // await后重新获取Scene
                scene = sceneRef;

                // 更新客户端任务数据 - 移除已完成的任务
                QuestComponent questComponent = scene.GetComponent<QuestComponent>();
                if (questComponent != null)
                {
                    questComponent.RemoveQuest(questId);
                }

                Log.Debug($"成功提交任务: {questId}");
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"提交任务请求失败: {e}");
                return false;
            }
        }

        /// <summary>
        /// 同步任务数据
        /// </summary>
        public static async ETTask<bool> SyncQuestData(Scene scene)
        {
            try
            {
                C2M_SyncQuestData request = C2M_SyncQuestData.Create();

                M2C_SyncQuestData response = (M2C_SyncQuestData)await scene.GetComponent<ClientSenderComponent>().Call(request);
                
                if (response.Error != 0)
                {
                    Log.Error($"同步任务数据失败: {response.Message}");
                    return false;
                }

                Log.Debug("成功同步任务数据");
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"同步任务数据请求失败: {e}");
                return false;
            }
        }

        /// <summary>
        /// 放弃任务
        /// </summary>
        public static async ETTask<bool> AbandonQuest(Scene scene, long questId)
        {
            try
            {
                // 在await前创建EntityRef
                EntityRef<Scene> sceneRef = scene;
                
                C2M_AbandonQuest request = C2M_AbandonQuest.Create();
                request.QuestId = questId;

                M2C_AbandonQuest response = (M2C_AbandonQuest)await scene.GetComponent<ClientSenderComponent>().Call(request);
                
                if (response.Error != 0)
                {
                    Log.Error($"放弃任务失败: {response.Message}");
                    return false;
                }

                // await后重新获取Scene
                scene = sceneRef;

                // 更新客户端任务数据 - 移除放弃的任务
                QuestComponent questComponent = scene.GetComponent<QuestComponent>();
                if (questComponent != null)
                {
                    questComponent.RemoveQuest(questId);
                }

                Log.Debug($"成功放弃任务: {questId}");
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"放弃任务请求失败: {e}");
                return false;
            }
        }

        /// <summary>
        /// 查询可接取任务
        /// </summary>
        public static async ETTask<AvailableQuestInfo[]> QueryAvailableQuests(Scene scene, int npcId = 0)
        {
            try
            {
                C2M_QueryAvailableQuests request = C2M_QueryAvailableQuests.Create();
                request.NPCId = npcId;

                M2C_QueryAvailableQuests response = (M2C_QueryAvailableQuests)await scene.GetComponent<ClientSenderComponent>().Call(request);
                
                if (response.Error != 0)
                {
                    Log.Error($"查询可接取任务失败: {response.Message}");
                    return new AvailableQuestInfo[0];
                }

                Log.Debug($"查询到 {response.AvailableQuests.Count} 个可接取任务");
                return response.AvailableQuests.ToArray();
            }
            catch (System.Exception e)
            {
                Log.Error($"查询可接取任务请求失败: {e}");
                return new AvailableQuestInfo[0];
            }
        }

        /// <summary>
        /// 获取任务详情
        /// </summary>
        public static async ETTask<QuestDetailInfo> GetQuestDetail(Scene scene, int questId)
        {
            try
            {
                C2M_GetQuestDetail request = C2M_GetQuestDetail.Create();
                request.QuestId = questId;

                M2C_GetQuestDetail response = (M2C_GetQuestDetail)await scene.GetComponent<ClientSenderComponent>().Call(request);
                
                if (response.Error != 0)
                {
                    Log.Error($"获取任务详情失败: {response.Message}");
                    return null;
                }

                Log.Debug($"成功获取任务详情: {response.QuestDetail.QuestName}");
                return response.QuestDetail;
            }
            catch (System.Exception e)
            {
                Log.Error($"获取任务详情请求失败: {e}");
                return null;
            }
        }
    }
}