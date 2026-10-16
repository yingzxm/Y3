#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;

namespace SocialEvolution.Agents
{
    /// <summary>
    /// <see cref="IPersonMapPresence"/> 的读取实现。
    /// 户外格由 <see cref="PersonMapCells"/> 按市民编号种下。
    /// <see cref="IPersonMapPose.InBuilding"/> 等于 <see cref="AgentBrain.IsInsideWorkplace"/>。
    /// </summary>
    public sealed class PersonMapPresence : IPersonMapPresence
    {
        private readonly AgentsHourDriver _agents;
        private readonly Action<GameDateTime> _onHourCompleted;
        private bool _following;

        public PersonMapPresence(AgentsHourDriver agents)
        {
            if (agents == null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            _agents = agents;
            _onHourCompleted = HandleHourCompleted;
        }

        public event Action<GameDateTime>? Changed;

        public int Count => _agents.BrainCount;

        public IReadOnlyList<IPersonMapPose> Poses
        {
            get
            {
                AgentBrain[] brains = _agents.CopyBrains();
                var poses = new IPersonMapPose[brains.Length];
                for (int i = 0; i < brains.Length; i++)
                {
                    poses[i] = PoseOf(brains[i]);
                }

                return poses;
            }
        }

        public bool TryGet(CitizenId citizenId, out IPersonMapPose? pose)
        {
            AgentBrain[] brains = _agents.CopyBrains();
            for (int i = 0; i < brains.Length; i++)
            {
                if (brains[i].CitizenId == citizenId)
                {
                    pose = PoseOf(brains[i]);
                    return true;
                }
            }

            pose = null;
            return false;
        }

        public void FollowHours()
        {
            if (_following)
            {
                return;
            }

            _agents.HourCompleted += _onHourCompleted;
            _following = true;
        }

        public void UnfollowHours()
        {
            if (!_following)
            {
                return;
            }

            _agents.HourCompleted -= _onHourCompleted;
            _following = false;
        }

        private void HandleHourCompleted(GameDateTime now)
        {
            Changed?.Invoke(now);
        }

        private static PersonMapPose PoseOf(AgentBrain brain)
        {
            int cellX;
            int cellY;
            PersonMapCells.OutdoorCell(brain.CitizenId, out cellX, out cellY);
            return new PersonMapPose(brain.CitizenId, cellX, cellY, brain.IsInsideWorkplace);
        }
    }
}
