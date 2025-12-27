using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 记录行为树执行路径事件处理器
    /// </summary>
    [Event(SceneType.All)]
    public class BTRunTreeEvent_RecordRunPath: AEvent<Scene, BTRunTreeEvent>
    {
        protected override async ETTask Run(Scene scene, BTRunTreeEvent args)
        {
            // 获取所有编辑器实例
            var editors = BehaviorTreeEditor.GetAllInstances();
            if (editors == null || editors.Count == 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            List<int> path = args.Env.RunPath;
            long treeId = args.Root.TreeId;
            long entityId = args.Env.EntityId;
            // 直接获取env字典中的StringBuilder引用，不复制
            System.Text.StringBuilder snapshot = args.Env.GetSnapshot();

            // 记录路径到所有编辑器实例的调试记录器
            foreach (BehaviorTreeEditor editor in editors)
            {
                if (editor != null && editor.PathRecorder != null)
                {
                    editor.PathRecorder.RecordPath(entityId, treeId, path, snapshot);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}