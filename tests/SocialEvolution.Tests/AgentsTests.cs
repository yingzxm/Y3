using System;
using System.IO;
using System.Reflection;
using SocialEvolution.Agents;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using SocialEvolution.World;
using Xunit;

namespace SocialEvolution.Tests
{
    public class AgentsTests
    {
        private static readonly BuildingId HomeId = new BuildingId(5);
        private static readonly BuildingId FactoryId = new BuildingId(3);
        private static readonly BuildingId FarmId = new BuildingId(2);

        [Fact]
        public void MaxNeed_PicksTheSingleHighestNeed()
        {
            var maker = new MaxNeedDecisionMaker();
            Assert.Equal(AgentAction.Eat, maker.Decide(Context(Sat(0f, 1f, 1f, 1f), employed: true, openJobs: true)));
            Assert.Equal(AgentAction.Sleep, maker.Decide(Context(Sat(1f, 0f, 1f, 1f), employed: true, openJobs: true)));
            Assert.Equal(AgentAction.Work, maker.Decide(Context(Sat(1f, 1f, 0f, 1f), employed: true, openJobs: true)));
            Assert.Equal(AgentAction.SeekJob, maker.Decide(Context(Sat(1f, 1f, 0f, 1f), employed: false, openJobs: true)));
            Assert.Equal(AgentAction.Idle, maker.Decide(Context(Sat(1f, 1f, 0f, 1f), employed: false, openJobs: false)));
            Assert.Equal(AgentAction.Shop, maker.Decide(Context(Sat(1f, 1f, 1f, 0f), employed: false, openJobs: true)));
            Assert.Equal(
                AgentAction.Quit,
                maker.Decide(Context(Sat(1f, 1f, 1f, 0f), employed: true, openJobs: true, risk: 1f)));
            Assert.Equal(
                AgentAction.Shop,
                maker.Decide(Context(Sat(1f, 1f, 1f, 0f), employed: true, openJobs: true, risk: 0f)));
        }

        [Fact]
        public void TiedScores_KeepTheEarlierNeed()
        {
            var maker = new MaxNeedDecisionMaker
            {
                BaseHunger = 1f,
                BaseFatigue = 1f,
                BaseWealth = 0f,
                BaseSafety = 0f,
            };
            AgentDecisionContext tied = Context(Sat(0f, 0f, 0f, 0f), employed: true, openJobs: true);
            Assert.Equal(AgentAction.Eat, maker.Decide(in tied));
            Assert.Equal(NeedKind.Hunger, new NeedScores(0.5f, 0.5f, 0.5f, 0.5f).Winner);
            Assert.Equal(NeedKind.Wealth, new NeedScores(0f, 0f, 0.5f, 0.5f).Winner);

            maker.BaseHunger = 0f;
            Assert.Equal(AgentAction.Sleep, maker.Decide(in tied));
        }

        [Fact]
        public void Prosperity_SoftensHungerWithoutTurningItOff()
        {
            var maker = new MaxNeedDecisionMaker();
            AgentNeeds starving = Sat(0f, 1f, 1f, 1f);
            AgentDecisionContext atPeak = Context(starving, employed: true, openJobs: true, prosperity: 1f);
            Assert.Equal(AgentAction.Eat, maker.Decide(in atPeak));
            float hungerAtPeak = maker.Evaluate(in atPeak).Hunger;
            float defaultSoft = 1f - ((1f - 0.7f) * 1f);
            Assert.True(hungerAtPeak > 0f);
            Assert.Equal(1f * defaultSoft, hungerAtPeak);

            maker.ProsperityBasicNeedFloor = 0f;
            AgentDecisionContext floored = Context(starving, employed: false, openJobs: false, prosperity: 1f);
            Assert.Equal(AgentAction.Eat, maker.Decide(in floored));
            float floor = MaxNeedDecisionMaker.MinimumProsperityFloor;
            float flooredSoft = 1f - ((1f - floor) * 1f);
            Assert.Equal(flooredSoft, maker.Evaluate(in floored).Hunger);
            Assert.True(maker.Evaluate(in floored).Hunger > 0f);

            AgentNeeds close = Sat(0.25f, 1f, 0f, 1f);
            AgentDecisionContext calm = Context(close, employed: true, openJobs: true, prosperity: null);
            AgentDecisionContext booming = Context(close, employed: true, openJobs: true, prosperity: 1f);
            Assert.Equal(AgentAction.Eat, maker.Decide(in calm));
            maker.ProsperityBasicNeedFloor = 0.7f;
            Assert.Equal(AgentAction.Work, maker.Decide(in booming));
            float softened = maker.Evaluate(in booming).Hunger;
            Assert.True(softened > 0f);
            Assert.Equal(0.75f * defaultSoft, softened);

            AgentDecisionContext notANumber = Context(close, employed: true, openJobs: true, prosperity: float.NaN);
            Assert.Equal(maker.Decide(in calm), maker.Decide(in notANumber));
        }

        [Fact]
        public void Personality_SoftWeightsWealthAgainstSafety()
        {
            var maker = new MaxNeedDecisionMaker();
            AgentNeeds needs = Sat(1f, 1f, 0f, 0.4f);
            AgentAction thrifty = maker.Decide(Context(needs, employed: true, openJobs: true, work: 0.5f, saving: 1f, risk: 0f));
            AgentAction spendthrift = maker.Decide(Context(needs, employed: true, openJobs: true, work: 0.5f, saving: 0f, risk: 0f));
            Assert.Equal(AgentAction.Work, thrifty);
            Assert.Equal(AgentAction.Shop, spendthrift);

            AgentNeeds driveNeeds = Sat(1f, 1f, 0f, 0.4f);
            Assert.Equal(
                AgentAction.Shop,
                maker.Decide(Context(driveNeeds, employed: true, openJobs: true, work: 0f, saving: 0.5f, risk: 0f)));
            Assert.Equal(
                AgentAction.Work,
                maker.Decide(Context(driveNeeds, employed: true, openJobs: true, work: 1f, saving: 0.5f, risk: 0f)));
        }

        [Fact]
        public void NeedScore_IsBaseTimesUrgency_AndFatigueLoadRoundTrips()
        {
            var needs = new AgentNeeds();
            needs.SetSatisfaction(NeedKind.Hunger, 0.25f);
            Assert.Equal(0.75f, needs.GetUrgency(NeedKind.Hunger));
            Assert.Equal(1.5f, needs.GetNeedScore(NeedKind.Hunger, 2f));

            needs.SyncFatigueFromLoad(40f, 100f);
            Assert.Equal(40f, needs.FatigueLoad);
            Assert.Equal(0.6f, needs.FatigueSatisfaction);
            needs.FatigueSatisfaction = 0.25f;
            Assert.Equal(75f, needs.FatigueLoad);
            needs.SyncFatigueFromLoad(-5f, 100f);
            Assert.Equal(0f, needs.FatigueLoad);
            Assert.Equal(1f, needs.FatigueSatisfaction);
        }

        [Fact]
        public void Tick_SeeksWithInjectedWeights_AndStampsTheGameHour()
        {
            Citizen person = CitizenOf(8);
            WorldState world = WorldOf(person);
            var farmer = new JobPostingId(1);
            var worker = new JobPostingId(2);
            world.SetJobPosting(FarmId, farmer, "农民", 1, 100L, 0, SkillIds.Farming);
            world.SetJobPosting(FactoryId, worker, "工人", 1, 10L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(farmer, 12f, 1f));
            market.SetConditions(Shift(worker, 4f, 1f));

            var brain = new AgentBrain(person, market, needs: Sat(1f, 1f, 0f, 1f), jobSeekWeights: Weights(1f, 0f));
            GameDateTime hiredAt = new GameDateTime(1, 3);
            Assert.Equal(AgentAction.SeekJob, brain.Tick(hiredAt));
            JobEmployment? employment;
            Assert.True(market.TryGetEmployment(person.Id, out employment));
            Assert.NotNull(employment);
            Assert.Equal(farmer, employment!.PostingId);
            Assert.Equal(hiredAt, employment.HiredAt);
            Assert.Equal(3L, hiredAt.TotalHours);
            Assert.Equal(1, market.GetFilledCount(farmer));
            Assert.Equal(0, market.GetFilledCount(worker));
        }

        [Fact]
        public void Tick_WorkUsesFatigueFormula_SleepRecovers_EatAndShopStayLocal()
        {
            Citizen person = CitizenOf(4, health: 2f);
            WorldState world = WorldOf(person);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "工人", 1, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1.5f));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, postingId, GameDateTime.Origin));

            var brain = new AgentBrain(person, market, needs: Sat(1f, 1f, 0f, 1f));
            Assert.Equal(AgentAction.Work, brain.Tick(new GameDateTime(1, 1)));
            float once = FatigueFormula.Compute(0f, 8f, 1.5f, 2f, 0f);
            Assert.Equal(once, brain.Needs.FatigueLoad);
            Assert.Equal(1f - (once / AgentNeeds.DefaultMaxFatigue), brain.Needs.FatigueSatisfaction);

            Assert.Equal(AgentAction.Work, brain.Tick(new GameDateTime(1, 2)));
            float twice = FatigueFormula.Compute(once, 8f, 1.5f, 2f, 0f);
            Assert.Equal(twice, brain.Needs.FatigueLoad);

            brain.Needs.SetSatisfaction(NeedKind.Wealth, 1f);
            brain.Needs.SyncFatigueFromLoad(AgentNeeds.DefaultMaxFatigue);
            Assert.Equal(AgentAction.Sleep, brain.Tick(new GameDateTime(1, 3)));
            float slept = FatigueFormula.Compute(AgentNeeds.DefaultMaxFatigue, 0f, 0f, 2f, AgentBrain.DefaultSleepRecovery);
            Assert.Equal(slept, brain.Needs.FatigueLoad);
            Assert.True(market.TryGetEmployment(person.Id, out _));

            brain.Needs.SetSatisfaction(NeedKind.Fatigue, 1f);
            brain.Needs.SetSatisfaction(NeedKind.Hunger, 0f);
            long wealth = person.Wealth;
            Assert.Equal(AgentAction.Eat, brain.Tick(new GameDateTime(1, 4), prosperityIndex: 1f));
            Assert.Equal(AgentBrain.DefaultEatSatisfactionGain, brain.Needs.HungerSatisfaction);
            Assert.Equal(wealth, person.Wealth);

            brain.Needs.SetSatisfaction(NeedKind.Hunger, 1f);
            brain.Needs.SetSatisfaction(NeedKind.Safety, 0f);
            brain.Personality.RiskPreference = 0f;
            Assert.Equal(AgentAction.Shop, brain.Tick(new GameDateTime(1, 5)));
            Assert.Equal(AgentBrain.DefaultShopSafetyGain, brain.Needs.SafetySatisfaction);
            Assert.Equal(wealth, person.Wealth);
            Assert.Equal(1, market.GetFilledCount(postingId));
        }

        [Fact]
        public void Tick_QuitUsesTryQuit_AndDoesNotTakeASecondJob()
        {
            Citizen person = CitizenOf(6);
            WorldState world = WorldOf(person);
            var first = new JobPostingId(1);
            var second = new JobPostingId(2);
            world.SetJobPosting(FarmId, first, "农民", 1, 15L, 0, SkillIds.Farming);
            world.SetJobPosting(FactoryId, second, "工人", 1, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(first, 8f, 1f));
            market.SetConditions(Shift(second, 8f, 1f));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, first, GameDateTime.Origin));

            var quitting = new AgentBrain(
                person,
                market,
                needs: Sat(1f, 1f, 1f, 0f),
                personality: new PersonalityScalars(1f, 0.5f, 0.5f));
            Assert.Equal(AgentAction.Quit, quitting.Tick(new GameDateTime(1, 1)));
            Assert.False(quitting.IsEmployed);
            Assert.Equal(1, market.GetVacantSlots(first));
            Assert.Equal(0, market.GetFilledCount(first));

            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, first, new GameDateTime(1, 2)));
            var forced = new FixedDecision { Next = AgentAction.SeekJob };
            var seeker = new AgentBrain(person, market, forced, Sat(1f, 1f, 0f, 1f));
            Assert.Equal(AgentAction.SeekJob, seeker.Tick(new GameDateTime(1, 3)));
            Assert.Equal(1, forced.Calls);
            JobEmployment? employment;
            Assert.True(market.TryGetEmployment(person.Id, out employment));
            Assert.Equal(first, employment!.PostingId);
            Assert.Equal(0, market.GetFilledCount(second));
            Assert.Equal(1, market.GetVacantSlots(second));
        }

        [Fact]
        public void HourDriver_TicksOncePerGameHour_OnTheSameHookAsLabor()
        {
            Citizen person = CitizenOf(11);
            Citizen other = CitizenOf(12);
            WorldState world = WorldOf(person, other);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "工人", 2, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1f));
            var time = new TimeService();
            var brain = new AgentBrain(person, market, needs: Sat(1f, 1f, 0f, 1f));
            var driver = new AgentsHourDriver(new[] { brain });

            driver.Register(time);
            driver.Register(time);
            Assert.Equal(0, driver.HoursProcessed);
            Assert.False(brain.IsEmployed);

            Assert.Equal(1, time.StepHours(1));
            Assert.Equal(1, driver.HoursProcessed);
            Assert.Equal(AgentAction.SeekJob, brain.LastAction);
            Assert.True(brain.IsEmployed);
            JobEmployment? hired;
            Assert.True(market.TryGetEmployment(person.Id, out hired));
            Assert.Equal(time.Now, hired!.HiredAt);
            Assert.Equal(1, market.GetFilledCount(postingId));

            Assert.Equal(3, time.StepHours(3));
            Assert.Equal(4, driver.HoursProcessed);
            Assert.Equal(1, market.GetFilledCount(postingId));

            driver.Unregister(time);
            var late = new AgentBrain(other, market, needs: Sat(1f, 1f, 0f, 1f));
            driver.Add(late);
            time.StepHours(1);
            Assert.Equal(4, driver.HoursProcessed);
            Assert.False(late.IsEmployed);
            Assert.Equal(1, market.GetFilledCount(postingId));
        }

        [Fact]
        public void HourDriver_QuitsBeforeSeek_SoTheSameHourCanFillTheOpenedSeat()
        {
            Citizen leaver = CitizenOf(1);
            Citizen joiner = CitizenOf(2);
            WorldState world = WorldOf(leaver, joiner);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "工人", 1, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1f));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(leaver, postingId, GameDateTime.Origin));

            var quitting = new AgentBrain(
                leaver,
                market,
                needs: Sat(1f, 1f, 1f, 0f),
                personality: new PersonalityScalars(1f, 0.2f, 0.2f));
            var joining = new AgentBrain(joiner, market, needs: Sat(1f, 1f, 0f, 1f));
            var time = new TimeService();
            var driver = new AgentsHourDriver(new[] { joining, quitting });
            driver.Register(time);

            time.StepHours(1);
            Assert.Equal(AgentAction.Quit, quitting.LastAction);
            Assert.Equal(AgentAction.SeekJob, joining.LastAction);
            Assert.False(quitting.IsEmployed);
            Assert.True(joining.IsEmployed);
            Assert.Equal(0, market.GetVacantSlots(postingId));
            Assert.Equal(1, market.GetFilledCount(postingId));
            JobEmployment? employment;
            Assert.True(market.TryGetEmployment(joiner.Id, out employment));
            Assert.Equal(new GameDateTime(1, 1), employment!.HiredAt);
        }

        [Fact]
        public void HourDriver_PassesProsperityAsASoftWeightOnly()
        {
            Citizen person = CitizenOf(3);
            WorldState world = WorldOf(person);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "工人", 1, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1f));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, postingId, GameDateTime.Origin));

            AgentNeeds needs = Sat(0.25f, 1f, 0f, 1f);
            var brain = new AgentBrain(person, market, needs: needs);
            var calm = new TimeService();
            var calmDriver = new AgentsHourDriver(new[] { brain });
            calmDriver.Register(calm);
            calm.StepHours(1);
            Assert.Equal(AgentAction.Eat, brain.LastAction);
            Assert.Equal(0.25f + AgentBrain.DefaultEatSatisfactionGain, brain.Needs.HungerSatisfaction);
            Assert.Equal(0f, brain.Needs.FatigueLoad);

            needs.SetSatisfaction(NeedKind.Hunger, 0.25f);
            var booming = new TimeService();
            var boomingDriver = new AgentsHourDriver();
            boomingDriver.Add(brain);
            boomingDriver.ProsperityIndex = 1f;
            boomingDriver.Register(booming);
            booming.StepHours(1);
            Assert.Equal(AgentAction.Work, brain.LastAction);
            Assert.Equal(0.25f, brain.Needs.HungerSatisfaction);
            Assert.True(brain.Needs.FatigueLoad > 0f);
        }

        [Fact]
        public void DirectTick_DecidesOnce_AndUnemployedWorkDoesNotInventFatigue()
        {
            Citizen person = CitizenOf(9);
            WorldState world = WorldOf(person);
            var market = new LaborMarket(world);
            var script = new FixedDecision { Next = AgentAction.Work };
            var brain = new AgentBrain(person, market, script);
            Assert.Equal(AgentAction.None, brain.LastAction);
            Assert.Equal(AgentAction.Work, brain.Tick(GameDateTime.Origin));
            Assert.Equal(1, script.Calls);
            Assert.Equal(0f, brain.Needs.FatigueLoad);
            Assert.Equal(AgentAction.Work, brain.LastAction);
        }

        [Fact]
        public void RejectsNullArguments()
        {
            Citizen person = CitizenOf(1);
            WorldState world = WorldOf(person);
            var market = new LaborMarket(world);
            Assert.Throws<ArgumentNullException>(() => new AgentBrain(null!, market));
            Assert.Throws<ArgumentNullException>(() => new AgentBrain(person, null!));
            var brain = new AgentBrain(person, market);
            Assert.Throws<ArgumentNullException>(() => brain.BindPerson(null!));
            Assert.Throws<ArgumentNullException>(() => new AgentsHourDriver(null!));
            var driver = new AgentsHourDriver();
            Assert.Throws<ArgumentNullException>(() => driver.Add(null!));
            Assert.Throws<ArgumentNullException>(() => driver.Register(null!));
            Assert.Throws<ArgumentNullException>(() => driver.Unregister(null!));
            Assert.Throws<ArgumentNullException>(() => driver.SeekContext = null!);
        }

        [Fact]
        public void PublicSurface_HasNoAssignmentLlmOrEngine()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (Type type in typeof(AgentBrain).Assembly.GetTypes())
            {
                if (type.Name.IndexOf('<') >= 0)
                {
                    continue;
                }

                Assert.DoesNotContain("Assign", type.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Recruitment", type.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Company", type.Name, StringComparison.OrdinalIgnoreCase);
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    if (method.Name.IndexOf('<') >= 0)
                    {
                        continue;
                    }

                    Assert.DoesNotContain("Assign", method.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("HireCitizen", method.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("ForceEmploy", method.Name, StringComparison.OrdinalIgnoreCase);
                }
            }

            string root = RepoRoot();
            foreach (string file in Directory.GetFiles(Path.Combine(root, "Assets", "Scripts", "Agents"), "*.cs"))
            {
                string text = File.ReadAllText(file);
                Assert.DoesNotContain("UnityEngine", text, StringComparison.Ordinal);
                Assert.DoesNotContain("MonoBehaviour", text, StringComparison.Ordinal);
                Assert.DoesNotContain("AssignCitizen", text, StringComparison.Ordinal);
                Assert.DoesNotContain("AssignPerson", text, StringComparison.Ordinal);
                Assert.DoesNotContain("OpenAI", text, StringComparison.Ordinal);
                Assert.DoesNotContain("ChatCompletion", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"farming\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"manufacturing\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"trade\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"administration\"", text, StringComparison.Ordinal);
            }
        }

        private static AgentDecisionContext Context(
            AgentNeeds needs,
            bool employed,
            bool openJobs,
            float? prosperity = null,
            float risk = 0.5f,
            float work = 0.5f,
            float saving = 0.5f,
            PersonalityScalars? personality = null)
        {
            return new AgentDecisionContext(
                needs,
                personality ?? new PersonalityScalars(risk, work, saving),
                employed,
                openJobs,
                prosperity);
        }

        private static AgentNeeds Sat(float hunger, float fatigue, float wealth, float safety)
        {
            return new AgentNeeds
            {
                HungerSatisfaction = hunger,
                FatigueSatisfaction = fatigue,
                WealthSatisfaction = wealth,
                SafetySatisfaction = safety,
            };
        }

        private static JobSeekScoreWeights Weights(float wage, float hoursPenalty)
        {
            JobSeekScoreWeights weights = JobSeekScoreWeights.DemoDefaults;
            weights.Wage = wage;
            weights.HoursPenalty = hoursPenalty;
            weights.CommutePenalty = 0f;
            weights.IntensityPenalty = 0f;
            weights.Safety = 0f;
            weights.Development = 0f;
            weights.Status = 0f;
            weights.SkillMatch = 0f;
            weights.Stability = 0f;
            weights.FullSkillMatch = 1f;
            return weights;
        }

        private static JobConditions Shift(JobPostingId postingId, float workHours, float intensity)
        {
            return new JobConditions(postingId, workHours, intensity, WorkShift.Day);
        }

        private static Citizen CitizenOf(int id, params SkillLevel[] skills)
        {
            return CitizenOf(id, 30, EducationLevel.Secondary, 1f, skills);
        }

        private static Citizen CitizenOf(int id, float health, params SkillLevel[] skills)
        {
            return CitizenOf(id, 30, EducationLevel.Secondary, health, skills);
        }

        private static Citizen CitizenOf(int id, int age, EducationLevel education, float health, params SkillLevel[] skills)
        {
            return new Citizen(
                new CitizenId(id),
                "市民" + id.ToString("000"),
                age,
                education,
                skills,
                health,
                0L,
                HomeId);
        }

        private static WorldState WorldOf(params Citizen[] citizens)
        {
            var housing = new Building(HomeId, BuildingKind.Housing, "公共住宅", 30);
            var factory = new Building(FactoryId, BuildingKind.Factory, "工具工坊");
            var farm = new Building(FarmId, BuildingKind.Farm, "城郊农场");
            var sites = new BuildingSite[]
            {
                new BuildingSite(housing, new Inventory(200)),
                new BuildingSite(factory, new Inventory(100)),
                new BuildingSite(farm, new Inventory(100)),
            };
            return new WorldState(citizens, sites, new Treasury(0));
        }

        private static string RepoRoot()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Assets", "Scripts", "Domain")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("找不到仓库根目录。");
        }

        private sealed class FixedDecision : IDecisionMaker
        {
            public AgentAction Next { get; set; }

            public int Calls { get; private set; }

            public AgentAction Decide(in AgentDecisionContext context)
            {
                Calls++;
                return Next;
            }
        }
    }
}
