#nullable enable
using SocialEvolution.Domain;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 地图层读取市民位置的合同。TryGet 返回 true 表示这个市民有姿态。
    /// world 是 Unity 世界坐标（XY，z = 0）。inBuilding 为 true 时隐藏精灵，为 false 时画在 world。
    /// PoseSource 为空时地图层继续用自己的占位散布。
    /// Gap 上若 <c>CitizenMapVisualController.cs</c> 已经声明了同名接口，不要再复制这一份。
    /// </summary>
    public interface ICitizenMapPoseSource
    {
        bool TryGet(CitizenId id, out Vector3 world, out bool inBuilding);
    }
}
