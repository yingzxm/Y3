#nullable enable

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 工作疲劳。Fatigue_new = Fatigue + WorkHours × WorkIntensity × HealthFactor − Recovery。
    /// </summary>
    public static class FatigueFormula
    {
        public static float Compute(
            float currentFatigue,
            float workHours,
            float workIntensity,
            float healthFactor,
            float recovery)
        {
            return currentFatigue + (workHours * workIntensity * healthFactor) - recovery;
        }
    }
}
