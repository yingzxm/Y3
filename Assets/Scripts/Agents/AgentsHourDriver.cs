#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Labor;

namespace SocialEvolution.Agents
{
    /// <summary>
    /// 每个游戏时让已登记的 <see cref="AgentBrain"/> 各执行一个行动。
    /// 挂到 <see cref="ISimStepHooks.RegisterGameHour"/>，不在 <c>Update</c> 里调用。
    /// 先辞职。这一小时如果有人辞职，还没入职的人会再决策一次，以便申请刚空出来的岗位。
    /// 已经在职、而且这次不辞职的人，沿用第一次的决策。
    /// 每个实际执行的行动写入 <see cref="Log"/>，一位市民这一小时一行。
    /// </summary>
    public sealed class AgentsHourDriver
    {
        private readonly List<AgentBrain> _brains = new List<AgentBrain>();
        private JobSeekContext _seekContext = new JobSeekContext();

        public AgentsHourDriver()
            : this(Array.Empty<AgentBrain>())
        {
        }

        public AgentsHourDriver(IEnumerable<AgentBrain> brains)
            : this(brains, new CitizenActionLog())
        {
        }

        public AgentsHourDriver(IEnumerable<AgentBrain> brains, CitizenActionLog log)
        {
            if (brains == null)
            {
                throw new ArgumentNullException(nameof(brains));
            }

            Log = log ?? throw new ArgumentNullException(nameof(log));
            foreach (AgentBrain brain in brains)
            {
                Add(brain);
            }
        }

        /// <summary>传给本小时每次 <see cref="AgentBrain.Tick"/> 的软繁荣指数。空表示不调整。</summary>
        public float? ProsperityIndex { get; set; }

        public JobSeekContext SeekContext
        {
            get => _seekContext;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _seekContext = value;
            }
        }

        public int BrainCount => _brains.Count;

        /// <summary>已经处理过的游戏时次数。注册之前为 0。</summary>
        public int HoursProcessed { get; private set; }

        /// <summary>每位市民最近的行动。求职、辞职、上班、进食、睡眠、购物、闲置都会记。</summary>
        public CitizenActionLog Log { get; }

        public void Add(AgentBrain brain)
        {
            if (brain == null)
            {
                throw new ArgumentNullException(nameof(brain));
            }

            if (!_brains.Contains(brain))
            {
                _brains.Add(brain);
            }
        }

        public bool Remove(AgentBrain brain)
        {
            if (brain == null)
            {
                throw new ArgumentNullException(nameof(brain));
            }

            return _brains.Remove(brain);
        }

        public bool TryGetBrain(CitizenId citizenId, out AgentBrain? brain)
        {
            for (int i = 0; i < _brains.Count; i++)
            {
                if (_brains[i].CitizenId == citizenId)
                {
                    brain = _brains[i];
                    return true;
                }
            }

            brain = null;
            return false;
        }

        public void OnGameHour(GameDateTime now)
        {
            HoursProcessed++;
            AgentBrain[] batch = _brains.ToArray();
            var chosen = new AgentAction[batch.Length];
            var quitThisHour = new bool[batch.Length];
            bool anyoneQuit = false;
            for (int i = 0; i < batch.Length; i++)
            {
                chosen[i] = batch[i].SelectAction(ProsperityIndex);
                if (chosen[i] == AgentAction.Quit)
                {
                    ExecuteAndRecord(batch[i], chosen[i], now);
                    quitThisHour[i] = true;
                    anyoneQuit = true;
                }
            }

            for (int i = 0; i < batch.Length; i++)
            {
                if (quitThisHour[i])
                {
                    continue;
                }

                AgentAction action = chosen[i];
                if (anyoneQuit && !batch[i].IsEmployed)
                {
                    action = batch[i].SelectAction(ProsperityIndex);
                }

                ExecuteAndRecord(batch[i], action, now);
            }
        }

        private void ExecuteAndRecord(AgentBrain brain, AgentAction action, GameDateTime now)
        {
            brain.Execute(action, now, _seekContext);
            Log.Append(brain.CitizenId, now, action, CitizenLogText.Format(now, action));
        }

        public void Register(ISimStepHooks hooks)
        {
            if (hooks == null)
            {
                throw new ArgumentNullException(nameof(hooks));
            }

            hooks.RegisterGameHour(OnGameHour);
        }

        public void Unregister(ISimStepHooks hooks)
        {
            if (hooks == null)
            {
                throw new ArgumentNullException(nameof(hooks));
            }

            hooks.UnregisterGameHour(OnGameHour);
        }
    }
}
