#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Agents;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using SocialEvolution.World;
using LaborJobPosting = SocialEvolution.Labor.JobPosting;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 从在册市民里点选一位，读出现金、工作、住所、学历、需求前三、心理标量和最近日志。
    /// 不派工，也不改需求。点选仍走 <see cref="Select"/>；详情用 <see cref="TryRead"/>。
    /// </summary>
    public sealed class CitizenPicker
    {
        private readonly WorldState _world;
        private readonly ILaborMarket _labor;
        private readonly AgentsHourDriver _agents;
        private CitizenId? _selected;

        public CitizenPicker(WorldState world, ILaborMarket labor, AgentsHourDriver agents)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _labor = labor ?? throw new ArgumentNullException(nameof(labor));
            _agents = agents ?? throw new ArgumentNullException(nameof(agents));
        }

        public CitizenId? SelectedId => _selected;

        public bool Select(CitizenId id)
        {
            Citizen? citizen;
            if (!_world.TryGetCitizen(id, out citizen) || citizen == null)
            {
                return false;
            }

            _selected = id;
            return true;
        }

        public void Clear()
        {
            _selected = null;
        }

        public bool SelectNext()
        {
            return Move(1);
        }

        public bool SelectPrevious()
        {
            return Move(-1);
        }

        public bool TryGetSelected(out CitizenView view)
        {
            if (!_selected.HasValue)
            {
                view = default;
                return false;
            }

            return TryRead(_selected.Value, out view);
        }

        public bool TryRead(CitizenId id, out CitizenView view)
        {
            Citizen? citizen;
            if (!_world.TryGetCitizen(id, out citizen) || citizen == null)
            {
                view = default;
                return false;
            }

            bool employed = _labor.TryGetEmployment(id, out JobEmployment? employment) && employment != null;
            string jobTitle = string.Empty;
            long wage = 0L;
            bool hasWorkplace = false;
            BuildingId workplaceId = default;
            if (employed && employment != null)
            {
                LaborJobPosting? posting;
                if (_labor.TryGetPosting(employment.PostingId, out posting) && posting != null)
                {
                    jobTitle = posting.Title;
                    wage = posting.WagePerHour;
                    hasWorkplace = true;
                    workplaceId = posting.BuildingId;
                }
            }

            CitizenNeedSlot need1 = default;
            CitizenNeedSlot need2 = default;
            CitizenNeedSlot need3 = default;
            bool hasPsychology = false;
            float risk = 0f;
            float work = 0f;
            float saving = 0f;
            AgentBrain? brain;
            if (_agents.TryGetBrain(id, out brain) && brain != null)
            {
                RankTopNeeds(brain.Needs, out need1, out need2, out need3);
                hasPsychology = true;
                risk = brain.Personality.RiskPreference;
                work = brain.Personality.WorkDrive;
                saving = brain.Personality.SavingTendency;
            }

            bool hasTopNeed = need1.Present && need1.Urgency > 0f;
            NeedKind topNeed = need1.Present ? need1.Kind : NeedKind.Hunger;
            float urgency = need1.Present ? need1.Urgency : 0f;

            view = new CitizenView(
                citizen.Id,
                citizen.Name,
                citizen.Wealth,
                employed,
                jobTitle,
                wage,
                hasTopNeed,
                topNeed,
                urgency,
                _agents.Log.GetRecent(id),
                citizen.ResidenceId,
                ResidenceLabelOf(citizen.ResidenceId),
                citizen.Education,
                hasWorkplace,
                workplaceId,
                need1,
                need2,
                need3,
                hasPsychology,
                risk,
                work,
                saving);
            return true;
        }

        private bool Move(int step)
        {
            IReadOnlyList<Citizen> citizens = _world.Citizens;
            if (citizens.Count == 0)
            {
                return false;
            }

            int index;
            if (!_selected.HasValue || !TryIndex(_selected.Value, out index))
            {
                index = step < 0 ? citizens.Count - 1 : 0;
            }
            else
            {
                int count = citizens.Count;
                index = (index + step) % count;
                if (index < 0)
                {
                    index += count;
                }
            }

            _selected = citizens[index].Id;
            return true;
        }

        private bool TryIndex(CitizenId id, out int index)
        {
            IReadOnlyList<Citizen> citizens = _world.Citizens;
            for (int i = 0; i < citizens.Count; i++)
            {
                if (citizens[i].Id == id)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private string ResidenceLabelOf(BuildingId residenceId)
        {
            BuildingSite? site;
            if (_world.TryGetSite(residenceId, out site) && site != null)
            {
                string name = site.Building.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return residenceId.ToString();
        }

        /// <summary>
        /// 紧急度从高到低取三项。并列时靠前的需求留在前面：饥饿、疲劳、财富、安全。
        /// </summary>
        private static void RankTopNeeds(
            AgentNeeds needs,
            out CitizenNeedSlot first,
            out CitizenNeedSlot second,
            out CitizenNeedSlot third)
        {
            var kinds = new NeedKind[4];
            var urgency = new float[4];
            kinds[0] = NeedKind.Hunger;
            kinds[1] = NeedKind.Fatigue;
            kinds[2] = NeedKind.Wealth;
            kinds[3] = NeedKind.Safety;
            for (int i = 0; i < kinds.Length; i++)
            {
                urgency[i] = needs.GetUrgency(kinds[i]);
            }

            for (int i = 1; i < kinds.Length; i++)
            {
                NeedKind kind = kinds[i];
                float score = urgency[i];
                int j = i;
                while (j > 0 && score > urgency[j - 1])
                {
                    kinds[j] = kinds[j - 1];
                    urgency[j] = urgency[j - 1];
                    j--;
                }

                kinds[j] = kind;
                urgency[j] = score;
            }

            first = new CitizenNeedSlot(true, kinds[0], urgency[0]);
            second = new CitizenNeedSlot(true, kinds[1], urgency[1]);
            third = new CitizenNeedSlot(true, kinds[2], urgency[2]);
        }
    }
}
