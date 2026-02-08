using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class CombatTextComponent : Entity, IAwake, IDestroy, ILateUpdate
    {
        public GameObject RootGameObject;
        public Canvas Canvas;
        public RectTransform CanvasRect;

        public readonly List<CombatTextNodeData> ActiveNodes = new();
        public readonly Queue<CombatTextNodeData> CachedNodes = new();
    }

    public struct CombatTextNodeData
    {
        public GameObject GameObject;
        public RectTransform RectTransform;
        public TextMeshProUGUI Text;
        public Vector2 Velocity;
        public float Lifetime;
        public float Elapsed;
        public float StartScale;
        public float EndScale;
    }
}
