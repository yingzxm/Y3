#nullable enable
using System;
using System.Collections.Generic;
using SocialEvolution.Agents;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using SocialEvolution.World;
using DomainJobPosting = SocialEvolution.Domain.JobPosting;

namespace SocialEvolution.Stats
{
    /// <summary>
    /// 场景可以调用的读面：城市统计和市民点选。
    /// <see cref="Attach"/> 给每个编制登记白班条件，为每位市民建 <see cref="AgentBrain"/>，
    /// 并把 <see cref="AgentsHourDriver"/> 挂到 <see cref="ITimeService"/>。行动日志由驱动在每个游戏时写入。
    /// <c>GameSession.CreateNew</c> 本身仍然只挂空回调。场景在创建会话之后调用 <see cref="Attach"/>。
    /// </summary>
    public sealed class DemoReadout
    {
        public const float DemoShiftHours = 8f;

        private readonly IMidPriceSource _prices;
        private Func<IMidPriceSource>? _livePrices;

        public DemoReadout(WorldState world, ILaborMarket labor, AgentsHourDriver agents, IMidPriceSource prices)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Labor = labor ?? throw new ArgumentNullException(nameof(labor));
            Agents = agents ?? throw new ArgumentNullException(nameof(agents));
            _prices = prices ?? throw new ArgumentNullException(nameof(prices));
            Picker = new CitizenPicker(world, labor, agents);
            Housing = new HousingDetailReader(world);
        }

        public WorldState World { get; }

        public ILaborMarket Labor { get; }

        public AgentsHourDriver Agents { get; }

        public CitizenPicker Picker { get; }

        public HousingDetailReader Housing { get; }

        public static DemoReadout Attach(ITimeService time, WorldState world)
        {
            return Attach(time, world, GoodMidPrices.DemoOpening());
        }

        public static DemoReadout Attach(ITimeService time, WorldState world, IMidPriceSource prices)
        {
            if (time == null)
            {
                throw new ArgumentNullException(nameof(time));
            }

            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (prices == null)
            {
                throw new ArgumentNullException(nameof(prices));
            }

            var labor = new LaborMarket(world);
            PublishDayShift(labor, world);
            var brains = new List<AgentBrain>(world.Citizens.Count);
            for (int i = 0; i < world.Citizens.Count; i++)
            {
                brains.Add(new AgentBrain(world.Citizens[i], labor));
            }

            var agents = new AgentsHourDriver(brains);
            agents.Register(time);
            var readout = new DemoReadout(world, labor, agents, prices);
            if (world.Citizens.Count > 0)
            {
                readout.Select(world.Citizens[0].Id);
            }

            return readout;
        }

        /// <summary>每次 <see cref="Read"/> 重新取中间价。市场接入后读 <c>GetState(good).MidPrice</c>。</summary>
        public void UseLivePrices(Func<IMidPriceSource> readEachTime)
        {
            if (readEachTime == null)
            {
                throw new ArgumentNullException(nameof(readEachTime));
            }

            _livePrices = readEachTime;
        }

        public CityStats Read()
        {
            IMidPriceSource prices = _prices;
            if (_livePrices != null)
            {
                prices = _livePrices() ?? throw new InvalidOperationException("中间价读取返回了空。");
            }

            return CityStatsReader.Read(World, Labor, prices);
        }

        public bool Select(CitizenId id)
        {
            return Picker.Select(id);
        }

        public bool SelectNext()
        {
            return Picker.SelectNext();
        }

        public bool SelectPrevious()
        {
            return Picker.SelectPrevious();
        }

        public bool TryGetSelected(out CitizenView view)
        {
            return Picker.TryGetSelected(out view);
        }

        public bool TryGetThreeRecentLogs(out string first, out string second, out string third)
        {
            CitizenView view;
            if (!Picker.TryGetSelected(out view))
            {
                first = string.Empty;
                second = string.Empty;
                third = string.Empty;
                return false;
            }

            return view.TryGetThreeRecentLogs(out first, out second, out third);
        }

        /// <summary>把世界上每个编制登记成白班，工时 <see cref="DemoShiftHours"/>，强度 1。不改编制人数。</summary>
        public static void PublishDayShift(ILaborMarket labor, WorldState world)
        {
            if (labor == null)
            {
                throw new ArgumentNullException(nameof(labor));
            }

            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            IReadOnlyList<BuildingSite> sites = world.Sites;
            for (int i = 0; i < sites.Count; i++)
            {
                IReadOnlyList<DomainJobPosting> postings = sites[i].Building.JobPostings;
                for (int j = 0; j < postings.Count; j++)
                {
                    DomainJobPosting posting = postings[j];
                    labor.SetConditions(new JobConditions(posting.Id, DemoShiftHours, 1f, WorkShift.Day));
                }
            }
        }
    }
}
