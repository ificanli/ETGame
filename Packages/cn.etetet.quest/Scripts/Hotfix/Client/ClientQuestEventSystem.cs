using System.Collections.Generic;
using System.Linq;

namespace ET.Client
{
    /// <summary>
    /// 客户端任务事件系统 - 处理场景初始化时添加任务组件等
    /// </summary>
    
    /// <summary>
    /// 场景初始化后添加任务组件
    /// </summary>
    [Event(SceneType.Client)]
    public class AfterCreateCurrentScene_AddQuestComponent : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            // 为当前场景添加任务组件
            scene.AddComponent<QuestComponent>();

            Log.Info("QuestComponent added to scene");
            
            await ETTask.CompletedTask;
        }
    }


    /// <summary>
    /// 登录完成后初始化任务数据
    /// </summary>
    [Event(SceneType.Client)]
    public class LoginFinish_InitQuestData : AEvent<Scene, LoginFinish>
    {
        protected override async ETTask Run(Scene scene, LoginFinish args)
        {
            QuestComponent questComponent = scene.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                Log.Error("QuestComponent not found in scene");
                await ETTask.CompletedTask;
                return;
            }

            // 请求服务器同步任务数据
            await RequestSyncQuestData(scene);
            
            Log.Info("Quest data sync requested after login");
            
            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 请求同步任务数据
        /// </summary>
        private async ETTask RequestSyncQuestData(Scene scene)
        {
            try
            {
                // TODO: 发送同步任务数据的请求
                // 这里需要定义对应的协议消息
                /*
                C2M_SyncQuestData request = C2M_SyncQuestData.Create();
                M2C_SyncQuestData response = await scene.GetComponent<SessionComponent>().Session.Call(request) as M2C_SyncQuestData;
                
                if (response.Error == ErrorCode.ERR_Success)
                {
                    ClientQuestComponent questComponent = scene.GetComponent<QuestComponent>();
                    // 处理同步回来的任务数据
                    foreach (var questInfo in response.QuestList)
                    {
                        questComponent.UpdateQuestData(questInfo.QuestId, (QuestStatus)questInfo.Status);
                        questComponent.UpdateQuestObjective(questInfo.QuestId, questInfo.Objectives);
                    }
                }
                */
                
                Log.Info("Quest data synced successfully");
            }
            catch (System.Exception e)
            {
                Log.Error($"Sync quest data failed: {e}");
            }
            
            await ETTask.CompletedTask;
        }
    }

    /// <summary>
    /// 任务NPC交互事件
    /// </summary>
    public static class QuestNPCInteractionEvent
    {
        /// <summary>
        /// 与NPC对话时检查任务
        /// </summary>
        public static void OnTalkToNPC(Scene scene, int npcId)
        {
            // 检查该NPC是否有可接取的任务
            CheckAvailableQuests(scene, npcId);
            
            // 检查该NPC是否有可提交的任务
            CheckSubmittableQuests(scene, npcId);
        }

        /// <summary>
        /// 检查可接取的任务
        /// </summary>
        private static void CheckAvailableQuests(Scene scene, int npcId)
        {
            // TODO: 根据NPC ID查询可接取的任务
            // 这里需要遍历任务配置表，找到AcceptNPC为该npcId的任务
            /*
            var availableQuests = QuestConfigCategory.Instance.GetAll()
                .Where(config => config.Value.AcceptNPC == npcId)
                .Where(config => ClientQuestHelper.CanAcceptQuest(scene, config.Key))
                .ToList();

            if (availableQuests.Count > 0)
            {
                // 显示可接取任务列表UI
                ShowAvailableQuestsUI(scene, availableQuests);
            }
            */
        }

        /// <summary>
        /// 检查可提交的任务
        /// </summary>
        private static void CheckSubmittableQuests(Scene scene, int npcId)
        {
            QuestComponent questComponent = scene.GetComponent<QuestComponent>();
            if (questComponent == null)
            {
                return;
            }

            List<Quest> submittableQuests = questComponent.GetSubmittableQuests();
            // TODO: 需要根据任务配置检查NPC ID
            /*
            submittableQuests = submittableQuests
                .Where(quest => 
                {
                    QuestConfig config = GetQuestConfig(quest.QuestId);
                    return config != null && config.SubmitNPC == npcId;
                })
                .ToList();
            */

            if (submittableQuests.Count > 0)
            {
                // 显示可提交任务列表UI
                ShowSubmittableQuestsUI(scene, submittableQuests);
            }
        }

        /// <summary>
        /// 显示可接取任务UI
        /// </summary>
        private static void ShowAvailableQuestsUI(Scene scene, List<QuestConfig> availableQuests)
        {
            // TODO: 显示可接取任务的UI界面
            Log.Info($"Found {availableQuests.Count} available quests");
        }

        /// <summary>
        /// 显示可提交任务UI
        /// </summary>
        private static void ShowSubmittableQuestsUI(Scene scene, List<Quest> submittableQuests)
        {
            // TODO: 显示可提交任务的UI界面
            Log.Info($"Found {submittableQuests.Count} submittable quests");
        }
    }

    /// <summary>
    /// 任务进度提示系统
    /// </summary>
    public static class QuestProgressNotification
    {
        /// <summary>
        /// 显示任务进度更新提示
        /// </summary>
        public static void ShowProgressUpdate(long questId, string objectiveText, int currentCount, int requiredCount)
        {
            string message = $"{objectiveText} ({currentCount}/{requiredCount})";
            
            // TODO: 显示进度更新UI提示
            // NotificationUI.Show(message, NotificationType.QuestProgress);
            
            Log.Info($"Quest progress: {message}");
        }

        /// <summary>
        /// 显示任务完成提示
        /// </summary>
        public static void ShowQuestComplete(long questId)
        {
            // TODO: 获取任务配置
            /*
            QuestConfig config = GetQuestConfig(questId);
            if (config == null)
            {
                return;
            }

            string message = $"任务完成: {config.Name}";
            */
            
            string message = $"任务完成: {questId}";
            
            // TODO: 显示任务完成UI提示
            // NotificationUI.Show(message, NotificationType.QuestComplete);
            
            Log.Info($"Quest completed: {questId}");
        }

        /// <summary>
        /// 显示新任务可接取提示
        /// </summary>
        public static void ShowNewQuestAvailable(int questId)
        {
            // TODO: 获取任务配置
            /*
            QuestConfig config = GetQuestConfig(questId);
            if (config == null)
            {
                return;
            }

            string message = $"新任务可接取: {config.Name}";
            */
            
            string message = $"新任务可接取: {questId}";
            
            // TODO: 显示新任务可接取UI提示
            // NotificationUI.Show(message, NotificationType.NewQuest);
            
            Log.Info($"New quest available: {questId}");
        }
    }
}