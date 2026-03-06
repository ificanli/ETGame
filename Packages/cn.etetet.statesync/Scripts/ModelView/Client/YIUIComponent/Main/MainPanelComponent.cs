using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public partial class MainPanelComponent : Entity, ILateUpdate
    {
        public const string OnEventClickSearchingButtonInvoke = "MainPanelComponent.OnEventClickSearchingButtonInvoke";
        public const string OnEventClickBagButtonInvoke = "MainPanelComponent.OnEventClickBagButtonInvoke";
        public UIDataValueBool u_DataSearchingButton;
        public UITaskEventP0 u_EventClickSearchingButton;
        public UITaskEventHandleP0 u_EventClickSearchingButtonHandle;
        public UITaskEventP0 u_EventClickBagButton;
        public UITaskEventHandleP0 u_EventClickBagButtonHandle;
        public bool LastSearchButtonShow;
        public string LastFocusPointId;
        public string LastOpenContainerPointId;
        public int DebugProbeFrame;
    }
}
