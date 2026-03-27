using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 场景迷雾覆盖组件，负责把小地图迷雾运行时同步到主相机覆盖层。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class SceneFogComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public string RuntimeMapName;
        public GameObject OverlayObject;
        public SceneFogProjectorView ProjectorView;
        public Material OverlayMaterial;
        public Texture2D FogTexture;
        public Mesh OverlayMesh;
        public Color32[] FogPixels;
        public float RefreshInterval;
        public float EdgeSoftness;
        public float ExploredAlphaScale;
        public float LastRefreshTime;
        public bool ShaderMissingLogged;
        public long LastDiagnosticLogTime;
        public string LastDiagnosticSignature;
        public HashSet<string> LoggedDebugKeys;
    }
}
