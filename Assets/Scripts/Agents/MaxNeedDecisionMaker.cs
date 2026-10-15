#nullable enable

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 取 NeedScore 最大的一项，映射成一个做得到的行动。
    /// 繁荣指数只软乘基础分：基本需求（饥饿、疲劳）不低于
    /// <see cref="ProsperityBasicNeedFloor"/>，也不会被乘成 0。
    /// </summary>
    public sealed class MaxNeedDecisionMaker : IDecisionMaker
    {
        public const float MinimumProsperityFloor = 0.01f;

        public float BaseHunger { get; set; } = 1f;

        public float BaseFatigue { get; set; } = 1f;

        public float BaseWealth { get; set; } = 0.8f;

        public float BaseSafety { get; set; } = 0.9f;

        /// <summary>
        /// 繁荣指数为 1 时，饥饿和疲劳的基础分保留这个比例。默认 0.7。
        /// 小于 <see cref="MinimumProsperityFloor"/> 时按该下限算，避免把饥饿乘没。
        /// </summary>
        public float ProsperityBasicNeedFloor { get; set; } = 0.7f;

        public float QuitSafetyUrgencyThreshold { get; set; } = 0.7f;

        public float QuitRiskPreferenceThreshold { get; set; } = 0.65f;

        public AgentAction Decide(in AgentDecisionContext context)
        {
            NeedScores scores = Evaluate(in context);
            if (scores.Best <= 0f)
            {
                return AgentAction.Idle;
            }

            return MapNeedToAction(scores.Winner, in context);
        }

        public NeedScores Evaluate(in AgentDecisionContext context)
        {
            AgentNeeds needs = context.Needs ?? new AgentNeeds();
            PersonalityScalars personality = context.Personality ?? new PersonalityScalars();
            float basicSoft = SoftBasicNeedMultiplier(context.ProsperityIndex);
            float otherSoft = SoftOtherNeedMultiplier(context.ProsperityIndex);
            float work = Unit(personality.WorkDrive);
            float risk = Unit(personality.RiskPreference);
            float saving = Unit(personality.SavingTendency);

            float hunger = needs.GetNeedScore(NeedKind.Hunger, BaseHunger * basicSoft);
            float fatigue = needs.GetNeedScore(NeedKind.Fatigue, BaseFatigue * basicSoft);
            float wealthBase = BaseWealth * (0.5f + (0.5f * work)) * (0.75f + (0.5f * saving)) * otherSoft;
            float wealth = needs.GetNeedScore(NeedKind.Wealth, wealthBase);
            float safetyBase = BaseSafety * (1.1f - (0.2f * risk)) * otherSoft;
            float safety = needs.GetNeedScore(NeedKind.Safety, safetyBase);
            return new NeedScores(hunger, fatigue, wealth, safety);
        }

        private float SoftBasicNeedMultiplier(float? prosperityIndex)
        {
            float prosperity;
            if (!TryReadProsperity(prosperityIndex, out prosperity))
            {
                return 1f;
            }

            float floor = ProsperityBasicNeedFloor;
            if (float.IsNaN(floor) || float.IsInfinity(floor) || floor < MinimumProsperityFloor)
            {
                floor = MinimumProsperityFloor;
            }

            if (floor > 1f)
            {
                floor = 1f;
            }

            return 1f - ((1f - floor) * prosperity);
        }

        private static float SoftOtherNeedMultiplier(float? prosperityIndex)
        {
            float prosperity;
            if (!TryReadProsperity(prosperityIndex, out prosperity))
            {
                return 1f;
            }

            return 1f + (0.15f * prosperity);
        }

        private AgentAction MapNeedToAction(NeedKind need, in AgentDecisionContext context)
        {
            AgentNeeds needs = context.Needs ?? new AgentNeeds();
            PersonalityScalars personality = context.Personality ?? new PersonalityScalars();
            switch (need)
            {
                case NeedKind.Hunger:
                    return AgentAction.Eat;
                case NeedKind.Fatigue:
                    return AgentAction.Sleep;
                case NeedKind.Wealth:
                    if (!context.IsEmployed)
                    {
                        return context.HasOpenJobsAvailable ? AgentAction.SeekJob : AgentAction.Idle;
                    }

                    return AgentAction.Work;
                case NeedKind.Safety:
                    if (context.IsEmployed
                        && needs.GetUrgency(NeedKind.Safety) >= QuitSafetyUrgencyThreshold
                        && Unit(personality.RiskPreference) >= QuitRiskPreferenceThreshold)
                    {
                        return AgentAction.Quit;
                    }

                    return AgentAction.Shop;
                default:
                    return AgentAction.Idle;
            }
        }

        private static bool TryReadProsperity(float? prosperityIndex, out float prosperity)
        {
            if (!prosperityIndex.HasValue
                || float.IsNaN(prosperityIndex.Value)
                || float.IsInfinity(prosperityIndex.Value))
            {
                prosperity = 0f;
                return false;
            }

            float value = prosperityIndex.Value;
            if (value < 0f)
            {
                value = 0f;
            }

            if (value > 1f)
            {
                value = 1f;
            }

            prosperity = value;
            return true;
        }

        private static float Unit(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0.5f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }

    /// <summary>
    /// 四项 NeedScore。并列时靠前的需求胜出：饥饿、疲劳、财富、安全。
    /// </summary>
    public readonly struct NeedScores
    {
        public NeedScores(float hunger, float fatigue, float wealth, float safety)
        {
            Hunger = hunger;
            Fatigue = fatigue;
            Wealth = wealth;
            Safety = safety;
        }

        public float Hunger { get; }

        public float Fatigue { get; }

        public float Wealth { get; }

        public float Safety { get; }

        public float Best
        {
            get
            {
                float best = Hunger;
                if (Fatigue > best)
                {
                    best = Fatigue;
                }

                if (Wealth > best)
                {
                    best = Wealth;
                }

                if (Safety > best)
                {
                    best = Safety;
                }

                return best;
            }
        }

        public NeedKind Winner
        {
            get
            {
                NeedKind winner = NeedKind.Hunger;
                float best = Hunger;
                if (Fatigue > best)
                {
                    best = Fatigue;
                    winner = NeedKind.Fatigue;
                }

                if (Wealth > best)
                {
                    best = Wealth;
                    winner = NeedKind.Wealth;
                }

                if (Safety > best)
                {
                    winner = NeedKind.Safety;
                }

                return winner;
            }
        }
    }
}
