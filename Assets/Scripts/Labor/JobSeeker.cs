#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 自主求职：对空缺岗位打分，再调用 <see cref="ILaborMarket.TryApply"/>。
    /// 不从雇主申请池里挑人。
    /// </summary>
    public sealed class JobSeeker
    {
        private readonly ILaborMarket _market;
        private readonly IJobSeekScorer _scorer;

        public JobSeeker(ILaborMarket market, IJobSeekScorer scorer)
        {
            if (market == null)
            {
                throw new ArgumentNullException(nameof(market));
            }

            if (scorer == null)
            {
                throw new ArgumentNullException(nameof(scorer));
            }

            _market = market;
            _scorer = scorer;
        }

        public LaborApplyResult TrySeekBest(
            IPersonReadModel person,
            GameDateTime now,
            JobSeekContext context,
            out JobPostingId? chosenPostingId)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (context == null)
            {
                context = new JobSeekContext();
            }

            IReadOnlyList<JobPosting> open = _market.GetOpenPostings();
            JobPosting? best = null;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < open.Count; i++)
            {
                JobPosting posting = open[i];
                float score = _scorer.Score(person, posting, context);
                if (float.IsNegativeInfinity(score))
                {
                    continue;
                }

                if (best == null || score > bestScore)
                {
                    best = posting;
                    bestScore = score;
                }
            }

            if (best == null)
            {
                chosenPostingId = null;
                return LaborApplyResult.RejectedIneligible;
            }

            chosenPostingId = best.Id;
            return _market.TryApply(person, best.Id, now);
        }
    }
}
