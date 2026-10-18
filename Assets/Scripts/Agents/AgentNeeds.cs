#nullable enable

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 满足度在 [0, 1]，1 表示这项需求已经满足。
    /// 紧急度 = 1 − 满足度。NeedScore = BaseScore × 紧急度。
    /// 繁荣指数不在这里开关需求；软权重只在决策器里乘。
    /// </summary>
    public sealed class AgentNeeds
    {
        public const float DefaultMaxFatigue = 100f;

        private float _hunger = 1f;
        private float _fatigueSatisfaction = 1f;
        private float _fatigueLoad;
        private float _maxFatigue = DefaultMaxFatigue;
        private float _wealth = 0.5f;
        private float _safety = 0.5f;

        /// <summary>1 表示不饿。</summary>
        public float HungerSatisfaction
        {
            get => _hunger;
            set => _hunger = Clamp01(value);
        }

        /// <summary>1 表示不累。和 <see cref="FatigueLoad"/> 一起改。</summary>
        public float FatigueSatisfaction
        {
            get => _fatigueSatisfaction;
            set => SetSatisfaction(NeedKind.Fatigue, value);
        }

        /// <summary>由财富和期望比较后写入。本模块不改 <c>long</c> 财富。</summary>
        public float WealthSatisfaction
        {
            get => _wealth;
            set => _wealth = Clamp01(value);
        }

        /// <summary>含住所和人身安全。1 表示够安全。</summary>
        public float SafetySatisfaction
        {
            get => _safety;
            set => _safety = Clamp01(value);
        }

        /// <summary>疲劳负荷。满足度 = 1 − 负荷 / 上限，上限以上仍记实际负荷。</summary>
        public float FatigueLoad => _fatigueLoad;

        public float MaxFatigue => _maxFatigue;

        public float GetSatisfaction(NeedKind kind)
        {
            switch (kind)
            {
                case NeedKind.Hunger:
                    return Clamp01(_hunger);
                case NeedKind.Fatigue:
                    return Clamp01(_fatigueSatisfaction);
                case NeedKind.Wealth:
                    return Clamp01(_wealth);
                case NeedKind.Safety:
                    return Clamp01(_safety);
                default:
                    return 1f;
            }
        }

        public void SetSatisfaction(NeedKind kind, float value)
        {
            value = Clamp01(value);
            switch (kind)
            {
                case NeedKind.Hunger:
                    _hunger = value;
                    break;
                case NeedKind.Fatigue:
                    _fatigueSatisfaction = value;
                    _fatigueLoad = (1f - value) * _maxFatigue;
                    break;
                case NeedKind.Wealth:
                    _wealth = value;
                    break;
                case NeedKind.Safety:
                    _safety = value;
                    break;
            }
        }

        public float GetUrgency(NeedKind kind) => 1f - GetSatisfaction(kind);

        public float GetNeedScore(NeedKind kind, float baseScore) => baseScore * GetUrgency(kind);

        /// <summary>
        /// 用疲劳负荷回写满足度。负荷低于 0 或非数时记 0。
        /// </summary>
        public void SyncFatigueFromLoad(float fatigueLoad, float maxFatigue = DefaultMaxFatigue)
        {
            if (float.IsNaN(maxFatigue) || float.IsInfinity(maxFatigue) || maxFatigue <= 0f)
            {
                maxFatigue = DefaultMaxFatigue;
            }

            if (float.IsNaN(fatigueLoad) || fatigueLoad < 0f)
            {
                fatigueLoad = 0f;
            }

            _maxFatigue = maxFatigue;
            _fatigueLoad = fatigueLoad;
            float ratio = fatigueLoad / maxFatigue;
            if (ratio < 0f)
            {
                ratio = 0f;
            }

            if (ratio > 1f)
            {
                ratio = 1f;
            }

            _fatigueSatisfaction = 1f - ratio;
        }

        private static float Clamp01(float value)
        {
            if (float.IsNaN(value) || value < 0f)
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
}
