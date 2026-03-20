using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 小地图底图采集组件，负责把当前地图渲染到小地图 RenderTexture。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class MinimapCaptureComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public GameObject CameraObject;
        public Camera CaptureCamera;
        public RenderTexture TargetTexture;
        public bool MissingTargetLogged;
    }
}
