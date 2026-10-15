#nullable enable
using System;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using LaborJobPosting = SocialEvolution.Labor.JobPosting;

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 一个市民的需求、人格和本小时行动。
    /// <see cref="Tick"/> 由小时相位调用一次，不在每一帧调用。
    /// 求职走 <see cref="JobSeeker"/>（评分后 <c>TryApply</c>），辞职走 <c>TryQuit</c>。
    /// </summary>
    public sealed class AgentBrain
    {
        public const float DefaultEatSatisfactionGain = 0.4f;
        public const float DefaultShopSafetyGain = 0.2f;
        public const float DefaultSleepRecovery = 50f;

        private readonly ILaborMarket _laborMarket;
        private readonly IDecisionMaker _decisionMaker;
        private readonly JobSeeker _seeker;
        private IPersonReadModel _person;

        public AgentBrain(
            IPersonReadModel person,
            ILaborMarket laborMarket,
            IDecisionMaker? decisionMaker = null,
            AgentNeeds? needs = null,
            PersonalityScalars? personality = null,
            JobSeekScoreWeights? jobSeekWeights = null)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (laborMarket == null)
            {
                throw new ArgumentNullException(nameof(laborMarket));
            }

            _person = person;
            _laborMarket = laborMarket;
            _decisionMaker = decisionMaker ?? new MaxNeedDecisionMaker();
            Needs = needs ?? new AgentNeeds();
            Personality = personality ?? new PersonalityScalars();
            JobSeekScoreWeights weights = jobSeekWeights ?? JobSeekScoreWeights.DemoDefaults;
            _seeker = new JobSeeker(laborMarket, new SimpleJobSeekScorer(weights));
        }

        public CitizenId CitizenId => _person.Id;

        public IPersonReadModel Person => _person;

        public bool IsEmployed
        {
            get
            {
                JobEmployment? employment;
                return _laborMarket.TryGetEmployment(_person.Id, out employment) && employment != null;
            }
        }

        public AgentNeeds Needs { get; }

        public PersonalityScalars Personality { get; }

        public AgentAction LastAction { get; private set; } = AgentAction.None;

        public float EatSatisfactionGain { get; set; } = DefaultEatSatisfactionGain;

        public float ShopSafetyGain { get; set; } = DefaultShopSafetyGain;

        public float SleepRecovery { get; set; } = DefaultSleepRecovery;

        public void BindPerson(IPersonReadModel person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            _person = person;
        }

        /// <summary>
        /// 只决策，不改需求，也不动劳动力市场。
        /// </summary>
        public AgentAction SelectAction(float? prosperityIndex = null)
        {
            bool employed = _laborMarket.TryGetEmployment(_person.Id, out JobEmployment? employment) && employment != null;
            bool openJobs = _laborMarket.GetOpenPostings().Count > 0;
            var context = new AgentDecisionContext(Needs, Personality, employed, openJobs, prosperityIndex);
            return _decisionMaker.Decide(in context);
        }

        /// <summary>
        /// 执行已经选定的一个行动。<paramref name="now"/> 写进入职时间。
        /// </summary>
        public void Execute(AgentAction action, GameDateTime now, JobSeekContext? seekContext = null)
        {
            switch (action)
            {
                case AgentAction.SeekJob:
                    JobSeekContext context = seekContext ?? new JobSeekContext();
                    JobPostingId? chosen;
                    _seeker.TrySeekBest(_person, now, context, out chosen);
                    break;
                case AgentAction.Quit:
                    _laborMarket.TryQuit(_person);
                    break;
                case AgentAction.Work:
                    ApplyWorkFatigue();
                    break;
                case AgentAction.Eat:
                    Needs.HungerSatisfaction = Clamp01(Needs.HungerSatisfaction + EatSatisfactionGain);
                    break;
                case AgentAction.Sleep:
                    ApplySleepRecovery();
                    break;
                case AgentAction.Shop:
                    Needs.SafetySatisfaction = Clamp01(Needs.SafetySatisfaction + ShopSafetyGain);
                    break;
                case AgentAction.Idle:
                case AgentAction.None:
                default:
                    break;
            }

            LastAction = action;
        }

        /// <summary>
        /// 一个游戏时：决策并执行一个行动。
        /// </summary>
        public AgentAction Tick(GameDateTime now, JobSeekContext? seekContext = null, float? prosperityIndex = null)
        {
            AgentAction action = SelectAction(prosperityIndex);
            Execute(action, now, seekContext);
            return action;
        }

        private void ApplyWorkFatigue()
        {
            JobEmployment? employment;
            if (!_laborMarket.TryGetEmployment(_person.Id, out employment) || employment == null)
            {
                return;
            }

            LaborJobPosting? posting;
            if (!_laborMarket.TryGetPosting(employment.PostingId, out posting) || posting == null)
            {
                return;
            }

            float next = FatigueFormula.Compute(
                Needs.FatigueLoad,
                posting.WorkHours,
                posting.WorkIntensity,
                _person.HealthFactor,
                0f);
            Needs.SyncFatigueFromLoad(next);
        }

        private void ApplySleepRecovery()
        {
            float next = FatigueFormula.Compute(
                Needs.FatigueLoad,
                0f,
                0f,
                _person.HealthFactor,
                SleepRecovery);
            Needs.SyncFatigueFromLoad(next);
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
