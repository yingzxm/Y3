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
    /// 从在册市民里点选一位，读出现金、工作、最急需求和最近日志。
    /// 不派工，也不改需求。
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
            if (employed && employment != null)
            {
                LaborJobPosting? posting;
                if (_labor.TryGetPosting(employment.PostingId, out posting) && posting != null)
                {
                    jobTitle = posting.Title;
                    wage = posting.WagePerHour;
                }
            }

            bool hasTopNeed = false;
            NeedKind topNeed = NeedKind.Hunger;
            float urgency = 0f;
            AgentBrain? brain;
            if (_agents.TryGetBrain(id, out brain) && brain != null)
            {
                PickTopNeed(brain.Needs, out topNeed, out urgency);
                hasTopNeed = urgency > 0f;
            }

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
                _agents.Log.GetRecent(id));
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

        private static void PickTopNeed(AgentNeeds needs, out NeedKind kind, out float urgency)
        {
            kind = NeedKind.Hunger;
            urgency = needs.GetUrgency(NeedKind.Hunger);
            Consider(NeedKind.Fatigue, ref kind, ref urgency, needs);
            Consider(NeedKind.Wealth, ref kind, ref urgency, needs);
            Consider(NeedKind.Safety, ref kind, ref urgency, needs);
        }

        private static void Consider(NeedKind candidate, ref NeedKind kind, ref float urgency, AgentNeeds needs)
        {
            float next = needs.GetUrgency(candidate);
            if (next > urgency)
            {
                urgency = next;
                kind = candidate;
            }
        }
    }
}
