#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.World;
using DomainJobPosting = SocialEvolution.Domain.JobPosting;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 挂在 <see cref="WorldState"/> 编制上的劳动力市场。
    /// 在职名单只在这里。空缺 = 编制 − 在职，不足 0 时记 0。
    /// </summary>
    public sealed class LaborMarket : ILaborMarket
    {
        private readonly WorldState _world;
        private readonly Dictionary<int, PublishedPosting> _byPosting = new Dictionary<int, PublishedPosting>();
        private readonly Dictionary<int, JobEmployment> _byCitizen = new Dictionary<int, JobEmployment>();

        public LaborMarket(WorldState world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            _world = world;
        }

        public void SetConditions(JobConditions conditions)
        {
            if (conditions == null)
            {
                throw new ArgumentNullException(nameof(conditions));
            }

            Reconcile();
            DomainJobPosting? source = FindDomainPosting(conditions.PostingId.Value);
            if (source == null)
            {
                throw new ArgumentException("没有这个岗位编制。", nameof(conditions));
            }

            PublishedPosting? existing;
            if (_byPosting.TryGetValue(source.Id.Value, out existing))
            {
                existing.Conditions = conditions;
                existing.Source = source;
                existing.View = new JobPosting(source, conditions);
                return;
            }

            _byPosting.Add(source.Id.Value, new PublishedPosting(source, conditions));
        }

        public bool TryGetPosting(JobPostingId postingId, out JobPosting? posting)
        {
            Reconcile();
            PublishedPosting? slot;
            if (_byPosting.TryGetValue(postingId.Value, out slot))
            {
                posting = slot.View;
                return true;
            }

            posting = null;
            return false;
        }

        public IReadOnlyList<JobPosting> GetPostings()
        {
            Reconcile();
            var list = new List<JobPosting>(_byPosting.Count);
            foreach (KeyValuePair<int, PublishedPosting> pair in _byPosting)
            {
                list.Add(pair.Value.View);
            }

            list.Sort(CompareById);
            return list;
        }

        public IReadOnlyList<JobPosting> GetOpenPostings()
        {
            Reconcile();
            var list = new List<JobPosting>();
            foreach (KeyValuePair<int, PublishedPosting> pair in _byPosting)
            {
                if (Vacant(pair.Value) > 0)
                {
                    list.Add(pair.Value.View);
                }
            }

            list.Sort(CompareById);
            return list;
        }

        public int GetFilledCount(JobPostingId postingId)
        {
            return Require(postingId).Employments.Count;
        }

        public int GetVacantSlots(JobPostingId postingId)
        {
            return Vacant(Require(postingId));
        }

        public IReadOnlyList<JobEmployment> GetEmployments(JobPostingId postingId)
        {
            List<JobEmployment> employments = Require(postingId).Employments;
            var copy = new JobEmployment[employments.Count];
            for (int i = 0; i < employments.Count; i++)
            {
                copy[i] = employments[i];
            }

            return copy;
        }

        public bool TryGetEmployment(CitizenId citizenId, out JobEmployment? employment)
        {
            Reconcile();
            JobEmployment? found;
            if (_byCitizen.TryGetValue(citizenId.Value, out found) && found != null)
            {
                employment = found;
                return true;
            }

            employment = null;
            return false;
        }

        public LaborApplyResult TryApply(IPersonReadModel applicant, JobPostingId postingId, GameDateTime now)
        {
            if (applicant == null)
            {
                throw new ArgumentNullException(nameof(applicant));
            }

            EnsureCitizen(applicant);
            Reconcile();

            PublishedPosting? slot;
            if (!_byPosting.TryGetValue(postingId.Value, out slot))
            {
                return LaborApplyResult.PostingNotFound;
            }

            if (_byCitizen.ContainsKey(applicant.Id.Value))
            {
                return LaborApplyResult.RejectedAlreadyEmployed;
            }

            if (Vacant(slot) <= 0)
            {
                return LaborApplyResult.RejectedNoVacancy;
            }

            if (!slot.View.IsEligible(applicant))
            {
                return LaborApplyResult.RejectedIneligible;
            }

            var employment = new JobEmployment(applicant, postingId, now);
            slot.Employments.Add(employment);
            _byCitizen.Add(applicant.Id.Value, employment);
            return LaborApplyResult.Hired;
        }

        public bool TryQuit(IPersonReadModel applicant)
        {
            if (applicant == null)
            {
                throw new ArgumentNullException(nameof(applicant));
            }

            EnsureCitizen(applicant);
            Reconcile();

            JobEmployment? employment;
            if (!_byCitizen.TryGetValue(applicant.Id.Value, out employment) || employment == null)
            {
                return false;
            }

            RemoveEmployment(employment);
            return true;
        }

        private PublishedPosting Require(JobPostingId postingId)
        {
            Reconcile();
            PublishedPosting? slot;
            if (!_byPosting.TryGetValue(postingId.Value, out slot) || slot == null)
            {
                throw new InvalidOperationException("这个岗位还没有登记劳动条件。");
            }

            return slot;
        }

        private void EnsureCitizen(IPersonReadModel applicant)
        {
            Citizen? citizen;
            if (!_world.TryGetCitizen(applicant.Id, out citizen) || citizen == null)
            {
                throw new ArgumentException("申请人不在本局市民里。", nameof(applicant));
            }
        }

        private static int Vacant(PublishedPosting slot)
        {
            int vacant = slot.Source.Openings - slot.Employments.Count;
            return vacant < 0 ? 0 : vacant;
        }

        private void RemoveEmployment(JobEmployment employment)
        {
            _byCitizen.Remove(employment.CitizenId.Value);
            PublishedPosting? slot;
            if (_byPosting.TryGetValue(employment.PostingId.Value, out slot) && slot != null)
            {
                slot.Employments.Remove(employment);
            }
        }

        private void Reconcile()
        {
            List<int>? removed = null;
            foreach (KeyValuePair<int, PublishedPosting> pair in _byPosting)
            {
                DomainJobPosting? live = FindDomainPosting(pair.Key);
                if (live == null)
                {
                    if (removed == null)
                    {
                        removed = new List<int>();
                    }

                    removed.Add(pair.Key);
                    continue;
                }

                if (!ReferenceEquals(live, pair.Value.Source))
                {
                    ClearEmployments(pair.Value);
                    pair.Value.Source = live;
                    pair.Value.View = new JobPosting(live, pair.Value.Conditions);
                }
            }

            if (removed == null)
            {
                return;
            }

            for (int i = 0; i < removed.Count; i++)
            {
                PublishedPosting? slot;
                if (_byPosting.TryGetValue(removed[i], out slot) && slot != null)
                {
                    ClearEmployments(slot);
                    _byPosting.Remove(removed[i]);
                }
            }
        }

        private void ClearEmployments(PublishedPosting slot)
        {
            for (int i = 0; i < slot.Employments.Count; i++)
            {
                _byCitizen.Remove(slot.Employments[i].CitizenId.Value);
            }

            slot.Employments.Clear();
        }

        private DomainJobPosting? FindDomainPosting(int postingId)
        {
            IReadOnlyList<BuildingSite> sites = _world.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                IReadOnlyList<DomainJobPosting> postings = sites[i].Building.JobPostings;
                for (int j = 0; j < postings.Count; j++)
                {
                    if (postings[j].Id.Value == postingId)
                    {
                        return postings[j];
                    }
                }
            }

            return null;
        }

        private static int CompareById(JobPosting left, JobPosting right)
        {
            return left.Id.Value.CompareTo(right.Id.Value);
        }

        private sealed class PublishedPosting
        {
            public PublishedPosting(DomainJobPosting source, JobConditions conditions)
            {
                Source = source;
                Conditions = conditions;
                View = new JobPosting(source, conditions);
                Employments = new List<JobEmployment>();
            }

            public DomainJobPosting Source { get; set; }

            public JobConditions Conditions { get; set; }

            public JobPosting View { get; set; }

            public List<JobEmployment> Employments { get; }
        }
    }
}
