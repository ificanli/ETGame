using Sirenix.Serialization;

namespace ET
{
    /// <summary>
    /// 行为树全局剪贴板单例，支持跨编辑器实例的复制粘贴
    /// </summary>
    public class BTClipboard
    {
        private static BTClipboard instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static BTClipboard Instance
        {
            get
            {
                instance ??= new BTClipboard();
                return instance;
            }
        }

        /// <summary>
        /// 被复制/剪切的节点数据（序列化后的字节数组）
        /// </summary>
        private byte[] copiedNodeData;

        /// <summary>
        /// 是否是剪切操作（true为剪切，false为复制）
        /// </summary>
        private bool isCut;

        /// <summary>
        /// 剪切操作的源TreeView（用于剪切后删除节点）
        /// </summary>
        private TreeView cutSourceTreeView;

        /// <summary>
        /// 剪切操作的源NodeView（用于剪切后删除节点）
        /// </summary>
        private NodeView cutSourceNodeView;

        private BTClipboard()
        {
        }

        /// <summary>
        /// 复制节点到剪贴板
        /// </summary>
        public void Copy(BTNode node)
        {
            if (node == null)
            {
                return;
            }

            try
            {
                this.copiedNodeData = SerializationUtility.SerializeValue(node, DataFormat.Binary);
                this.isCut = false;
                this.cutSourceTreeView = null;
                this.cutSourceNodeView = null;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Copy node failed: {e.Message}");
            }
        }

        /// <summary>
        /// 剪切节点到剪贴板
        /// </summary>
        public void Cut(TreeView treeView, NodeView nodeView, BTNode node)
        {
            if (node == null || treeView == null || nodeView == null)
            {
                return;
            }

            try
            {
                this.copiedNodeData = SerializationUtility.SerializeValue(node, DataFormat.Binary);
                this.isCut = true;
                this.cutSourceTreeView = treeView;
                this.cutSourceNodeView = nodeView;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Cut node failed: {e.Message}");
            }
        }

        /// <summary>
        /// 从剪贴板粘贴节点
        /// </summary>
        /// <returns>反序列化的节点，如果剪贴板为空则返回null</returns>
        public BTNode Paste()
        {
            if (this.copiedNodeData == null)
            {
                return null;
            }

            try
            {
                BTNode clone = SerializationUtility.DeserializeValue<BTNode>(this.copiedNodeData, DataFormat.Binary);
                return clone;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Paste node failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 完成粘贴操作后，如果是剪切操作，需要从源位置删除节点
        /// </summary>
        public void FinishPaste()
        {
            if (this.isCut && this.cutSourceTreeView != null && this.cutSourceNodeView is { Parent: not null })
                    // 检查源TreeView和NodeView是否仍然有效
            {
                this.cutSourceNodeView.Parent.RemoveChild(this.cutSourceNodeView);
                this.cutSourceTreeView.Layout();
            }

            // 清空剪贴板
            this.Clear();
        }

        /// <summary>
        /// 清空剪贴板
        /// </summary>
        public void Clear()
        {
            this.copiedNodeData = null;
            this.isCut = false;
            this.cutSourceTreeView = null;
            this.cutSourceNodeView = null;
        }

        /// <summary>
        /// 检查剪贴板是否有数据
        /// </summary>
        public bool HasData()
        {
            return this.copiedNodeData != null;
        }

        /// <summary>
        /// 检查是否是剪切操作
        /// </summary>
        public bool IsCut()
        {
            return this.isCut;
        }

        /// <summary>
        /// 销毁单例（所有编辑器实例关闭时调用）
        /// </summary>
        public static void Destroy()
        {
            if (instance != null)
            {
                instance.Clear();
                instance = null;
            }
        }
    }
}
