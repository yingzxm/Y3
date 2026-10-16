#nullable enable
using SocialEvolution.Stats;
using UnityEngine;

namespace SocialEvolution.Bootstrap
{
    /// <summary>
    /// 场景里可以没有 <see cref="StatsDebugPanel"/>。
    /// AfterSceneLoad 把自己挂到 <see cref="SimulationDriver"/> 上，Start 时发布 PoseSource。
    /// 这时 CitizenMapVisualController 若已由它自己的 AfterSceneLoad 生成，就能被赋上姿态。
    /// 不改那个控制器的生成和 ApplyPose。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CitizenMapPoseHook : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SimulationDriver? driver = UnityEngine.Object.FindAnyObjectByType<SimulationDriver>();
            if (driver == null || driver.GetComponent<CitizenMapPoseHook>() != null)
            {
                return;
            }

            driver.gameObject.AddComponent<CitizenMapPoseHook>();
        }

        private void Start()
        {
            SimulationDriver? driver = GetComponent<SimulationDriver>();
            if (driver == null || driver.Session == null)
            {
                return;
            }

            DemoReadout readout = driver.Session.EnsureReadout();
            CitizenMapPoseAutoAttach.Apply(driver.Session, readout.Agents);
        }
    }
}
