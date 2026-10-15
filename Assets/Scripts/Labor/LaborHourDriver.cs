#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Core;
using SocialEvolution.Domain;

namespace SocialEvolution.Labor
{
    /// <summary>
    /// 每个游戏时处理一次已提交的辞职和求职。
    /// 通过 <see cref="Register"/> 挂到 <see cref="ISimStepHooks.RegisterGameHour"/>，不按帧跑。
    /// </summary>
    public sealed class LaborHourDriver
    {
        private readonly ILaborMarket _market;
        private readonly JobSeeker _seeker;
        private readonly List<Intent> _queue = new List<Intent>();

        public LaborHourDriver(ILaborMarket market, IJobSeekScorer scorer)
        {
            if (market == null)
            {
                throw new ArgumentNullException(nameof(market));
            }

            _market = market;
            _seeker = new JobSeeker(market, scorer);
        }

        public void RequestApply(IPersonReadModel person, JobSeekContext context)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            if (context == null)
            {
                context = new JobSeekContext();
            }

            _queue.Add(new Intent(IntentKind.Apply, person, context));
        }

        public void RequestQuit(IPersonReadModel person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            _queue.Add(new Intent(IntentKind.Quit, person, new JobSeekContext()));
        }

        /// <summary>先辞职，再求职。同一小时里可以辞掉旧岗再去申请空缺。</summary>
        public void OnGameHour(GameDateTime now)
        {
            Intent[] batch = _queue.ToArray();
            _queue.Clear();

            for (int i = 0; i < batch.Length; i++)
            {
                if (batch[i].Kind == IntentKind.Quit)
                {
                    _market.TryQuit(batch[i].Person);
                }
            }

            for (int i = 0; i < batch.Length; i++)
            {
                if (batch[i].Kind == IntentKind.Apply)
                {
                    JobPostingId? chosen;
                    _seeker.TrySeekBest(batch[i].Person, now, batch[i].Context, out chosen);
                }
            }
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

        private enum IntentKind
        {
            Apply = 0,
            Quit = 1,
        }

        private sealed class Intent
        {
            public Intent(IntentKind kind, IPersonReadModel person, JobSeekContext context)
            {
                Kind = kind;
                Person = person;
                Context = context;
            }

            public IntentKind Kind { get; }

            public IPersonReadModel Person { get; }

            public JobSeekContext Context { get; }
        }
    }
}
