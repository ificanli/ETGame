using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Common)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class ActionBarComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "Main";
        public const string ResName = "ActionBar";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot12;
        public ET.Client.ActionBarSlotComponent UISlot12 => u_UISlot12;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot11;
        public ET.Client.ActionBarSlotComponent UISlot11 => u_UISlot11;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot10;
        public ET.Client.ActionBarSlotComponent UISlot10 => u_UISlot10;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot9;
        public ET.Client.ActionBarSlotComponent UISlot9 => u_UISlot9;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot8;
        public ET.Client.ActionBarSlotComponent UISlot8 => u_UISlot8;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot7;
        public ET.Client.ActionBarSlotComponent UISlot7 => u_UISlot7;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot6;
        public ET.Client.ActionBarSlotComponent UISlot6 => u_UISlot6;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot5;
        public ET.Client.ActionBarSlotComponent UISlot5 => u_UISlot5;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot4;
        public ET.Client.ActionBarSlotComponent UISlot4 => u_UISlot4;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot3;
        public ET.Client.ActionBarSlotComponent UISlot3 => u_UISlot3;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot2;
        public ET.Client.ActionBarSlotComponent UISlot2 => u_UISlot2;
        public EntityRef<ET.Client.ActionBarSlotComponent> u_UISlot1;
        public ET.Client.ActionBarSlotComponent UISlot1 => u_UISlot1;

    }
}