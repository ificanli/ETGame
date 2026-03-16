using System;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 武器槽位组件（自定义部分）
    /// </summary>
    public partial class WeaponItemComponent : Entity
    {
        // 槽位索引（1或2），在初始化时设置
        public int SlotIndex;
        public Image IconImage;
        public Image SelectImage;
        public string LoadedIconName = string.Empty;
        public Sprite LoadedSprite;
    }
}
