#nullable enable
using System.Collections.Generic;
using SocialEvolution.Domain;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 贫富指标：市民财富的基尼系数。
    /// 较贫的人在前，序号 i 从 1 计。
    /// G = (2 × Σ i·xᵢ) / (n × Σ xᵢ) − (n + 1) / n。
    /// 有人财富为负时，先平移到最小值为 0。总和为 0 或没有市民时为 0。结果夹到 [0, 1]。
    /// </summary>
    public static class RichPoor
    {
        public static double Compute(IReadOnlyList<Citizen> citizens)
        {
            if (citizens == null || citizens.Count == 0)
            {
                return 0d;
            }

            int n = citizens.Count;
            var wealth = new double[n];
            double min = citizens[0].Wealth;
            for (int i = 0; i < n; i++)
            {
                wealth[i] = citizens[i].Wealth;
                if (wealth[i] < min)
                {
                    min = wealth[i];
                }
            }

            if (min < 0d)
            {
                for (int i = 0; i < n; i++)
                {
                    wealth[i] -= min;
                }
            }

            System.Array.Sort(wealth);
            double sum = 0d;
            for (int i = 0; i < n; i++)
            {
                sum += wealth[i];
            }

            if (sum == 0d)
            {
                return 0d;
            }

            double weighted = 0d;
            for (int i = 0; i < n; i++)
            {
                weighted += (i + 1d) * wealth[i];
            }

            double gini = ((2d * weighted) / (n * sum)) - ((n + 1d) / n);
            if (gini < 0d)
            {
                return 0d;
            }

            if (gini > 1d)
            {
                return 1d;
            }

            return gini;
        }
    }
}
