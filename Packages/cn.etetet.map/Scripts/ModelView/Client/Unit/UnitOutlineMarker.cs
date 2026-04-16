using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 标记运行时生成的单位描边渲染器，避免再次被当作原始渲染器缓存。
    /// </summary>
    [EnableClass]
    public class UnitOutlineMarker : MonoBehaviour
    {
    }
}
