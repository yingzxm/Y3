using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using SocialEvolution.World;
using Xunit;

namespace SocialEvolution.Tests
{
    public class LaborTests
    {
        private static readonly BuildingId HomeId = new BuildingId(5);
        private static readonly BuildingId FactoryId = new BuildingId(3);
        private static readonly BuildingId FarmId = new BuildingId(2);

        [Fact]
        public void A4_InsufficientSkillThreshold_HiresOnlyQualifiedAndLeavesVacancy()
        {
            const int openings = 4;
            Citizen lowCraft = CitizenOf(1, new SkillLevel(SkillIds.Manufacturing, 4));
            Citizen wrongSkill = CitizenOf(2, new SkillLevel(SkillIds.Farming, 10));
            Citizen unskilled = CitizenOf(3);
            Citizen qualified = CitizenOf(4, new SkillLevel(SkillIds.Manufacturing, 5));
            WorldState world = WorldOf(lowCraft, wrongSkill, unskilled, qualified);
            var postingId = new JobPostingId(1);
            SocialEvolution.Domain.JobPosting posting = world.SetJobPosting(
                FactoryId,
                postingId,
                "工人",
                openings,
                20L,
                5,
                SkillIds.Manufacturing);

            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1f));

            Assert.Equal(LaborApplyResult.RejectedIneligible, market.TryApply(lowCraft, postingId, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.RejectedIneligible, market.TryApply(wrongSkill, postingId, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.RejectedIneligible, market.TryApply(unskilled, postingId, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(qualified, postingId, GameDateTime.Origin));
            Assert.Equal(1, market.GetFilledCount(postingId));
            Assert.Equal(openings - 1, market.GetVacantSlots(postingId));
            Assert.True(market.GetVacantSlots(postingId) > 0);
            Assert.NotEqual(posting.Openings, market.GetVacantSlots(postingId));
            Assert.Null(typeof(SocialEvolution.Domain.JobPosting).GetProperty("VacantSlots"));
            Assert.Null(typeof(SocialEvolution.Domain.JobPosting).GetProperty("FilledCount"));
            Assert.Contains(market.GetOpenPostings(), item => item.Id == postingId);
        }

        [Fact]
        public void QualifiedApplicants_FillOpeningsUntilVacancyIsZero()
        {
            Citizen first = CitizenOf(1, new SkillLevel(SkillIds.Manufacturing, 1));
            Citizen second = CitizenOf(2, new SkillLevel(SkillIds.Manufacturing, 3));
            Citizen third = CitizenOf(3, new SkillLevel(SkillIds.Manufacturing, 9));
            WorldState world = WorldOf(first, second, third);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "工人", 2, 20L, 1, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1f));

            Assert.Equal(LaborApplyResult.Hired, market.TryApply(first, postingId, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(second, postingId, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.RejectedNoVacancy, market.TryApply(third, postingId, GameDateTime.Origin));
            Assert.Equal(2, market.GetFilledCount(postingId));
            Assert.Equal(0, market.GetVacantSlots(postingId));
            Assert.DoesNotContain(market.GetOpenPostings(), item => item.Id == postingId);
        }

        [Fact]
        public void TryApply_RejectsSecondApplicationWhileEmployed()
        {
            Citizen person = CitizenOf(
                5,
                new SkillLevel(SkillIds.Farming, 1),
                new SkillLevel(SkillIds.Manufacturing, 1));
            WorldState world = WorldOf(person);
            var farmer = new JobPostingId(1);
            var worker = new JobPostingId(2);
            world.SetJobPosting(FarmId, farmer, "农民", 2, 15L, 0, SkillIds.Farming);
            world.SetJobPosting(FactoryId, worker, "工人", 2, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(farmer, 8f, 1f));
            market.SetConditions(Shift(worker, 8f, 1f));

            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, farmer, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.RejectedAlreadyEmployed, market.TryApply(person, farmer, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.RejectedAlreadyEmployed, market.TryApply(person, worker, GameDateTime.Origin));
            Assert.Equal(1, market.GetFilledCount(farmer));
            Assert.Equal(0, market.GetFilledCount(worker));
            Assert.Equal(2, market.GetVacantSlots(worker));
        }

        [Fact]
        public void TryQuit_ReopensVacancy()
        {
            Citizen person = CitizenOf(6, new SkillLevel(SkillIds.Administration, 1));
            WorldState world = WorldOf(person);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "公务员", 5, 22L, 1, SkillIds.Administration);
            var market = new LaborMarket(world);
            market.SetConditions(new JobConditions(postingId, 8f, 1.2f, WorkShift.Day));

            Assert.False(market.TryQuit(person));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, postingId, GameDateTime.Origin));
            Assert.Equal(4, market.GetVacantSlots(postingId));
            Assert.True(market.TryQuit(person));
            JobEmployment? gone;
            Assert.False(market.TryGetEmployment(person.Id, out gone));
            Assert.Equal(0, market.GetFilledCount(postingId));
            Assert.Equal(5, market.GetVacantSlots(postingId));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, postingId, new GameDateTime(1, 2)));
            Assert.Equal(4, market.GetVacantSlots(postingId));
        }

        [Fact]
        public void AgeAndEducationThresholds_LeaveTheSlotEmpty()
        {
            Citizen tooYoung = CitizenOf(7, 19, EducationLevel.Higher);
            Citizen tooOld = CitizenOf(8, 61, EducationLevel.Higher);
            Citizen underEducated = CitizenOf(9, 30, EducationLevel.Primary);
            WorldState world = WorldOf(tooYoung, tooOld, underEducated);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "商人", 1, 18L, 0, SkillIds.Trade);
            var market = new LaborMarket(world);
            market.SetConditions(new JobConditions(
                postingId,
                6f,
                1f,
                WorkShift.Evening,
                EducationLevel.Secondary,
                20,
                60,
                1));

            Assert.Equal(LaborApplyResult.RejectedIneligible, market.TryApply(tooYoung, postingId, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.RejectedIneligible, market.TryApply(tooOld, postingId, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.RejectedIneligible, market.TryApply(underEducated, postingId, GameDateTime.Origin));
            Assert.Equal(1, market.GetVacantSlots(postingId));
            Assert.Equal(0, market.GetFilledCount(postingId));
        }

        [Fact]
        public void VacantSlots_FollowLiveDomainOpenings()
        {
            Citizen person = CitizenOf(1);
            WorldState world = WorldOf(person);
            var postingId = new JobPostingId(1);
            SocialEvolution.Domain.JobPosting posting = world.SetJobPosting(
                FactoryId,
                postingId,
                "工人",
                3,
                20L,
                0,
                SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1f));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, postingId, GameDateTime.Origin));
            Assert.Equal(2, market.GetVacantSlots(postingId));

            world.SetJobPosting(FactoryId, postingId, "工人", 5, 30L, 0, SkillIds.Manufacturing);
            Assert.Equal(5, posting.Openings);
            Assert.Equal(30L, posting.WagePerHour);
            Assert.Equal(SkillIds.Manufacturing, posting.RequiredSkillId);
            Assert.Null(typeof(SocialEvolution.Domain.JobPosting).GetProperty("FilledCount"));
            Assert.Equal(1, market.GetFilledCount(postingId));
            Assert.Equal(4, market.GetVacantSlots(postingId));
        }

        [Fact]
        public void RemovedPosting_ReleasesTheEmployee()
        {
            Citizen person = CitizenOf(3);
            WorldState world = WorldOf(person);
            var farmer = new JobPostingId(1);
            var worker = new JobPostingId(2);
            world.SetJobPosting(FarmId, farmer, "农民", 1, 15L, 0, SkillIds.Farming);
            world.SetJobPosting(FactoryId, worker, "工人", 1, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(farmer, 8f, 1f));
            market.SetConditions(Shift(worker, 8f, 1f));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, farmer, GameDateTime.Origin));

            Assert.True(world.RemoveJobPosting(FarmId, farmer));
            JobEmployment? employment;
            Assert.False(market.TryGetEmployment(person.Id, out employment));
            Assert.Equal(LaborApplyResult.PostingNotFound, market.TryApply(person, farmer, GameDateTime.Origin));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, worker, GameDateTime.Origin));
            Assert.Equal(0, market.GetVacantSlots(worker));
        }

        [Fact]
        public void JobSeeker_UsesInjectedWeights()
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

            JobPostingId? chosen;
            LaborApplyResult wageFirst = new JobSeeker(market, new SimpleJobSeekScorer(Weights(1f, 0f)))
                .TrySeekBest(person, GameDateTime.Origin, new JobSeekContext(), out chosen);
            Assert.Equal(LaborApplyResult.Hired, wageFirst);
            Assert.Equal(farmer, chosen);

            Assert.True(market.TryQuit(person));
            LaborApplyResult shortHours = new JobSeeker(market, new SimpleJobSeekScorer(Weights(0f, 1f)))
                .TrySeekBest(person, GameDateTime.Origin, new JobSeekContext(), out chosen);
            Assert.Equal(LaborApplyResult.Hired, shortHours);
            Assert.Equal(worker, chosen);
            Assert.Equal(1, market.GetVacantSlots(farmer));
            Assert.Equal(0, market.GetVacantSlots(worker));
        }

        [Fact]
        public void JobSeeker_SkipsIneligiblePostings()
        {
            Citizen person = CitizenOf(9, new SkillLevel(SkillIds.Farming, 1));
            WorldState world = WorldOf(person);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FarmId, postingId, "农场主管", 1, 25L, 4, SkillIds.Farming);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1f));
            var seeker = new JobSeeker(market, new SimpleJobSeekScorer(JobSeekScoreWeights.DemoDefaults));

            JobPostingId? chosen;
            LaborApplyResult result = seeker.TrySeekBest(person, GameDateTime.Origin, new JobSeekContext(), out chosen);
            Assert.Equal(LaborApplyResult.RejectedIneligible, result);
            Assert.Null(chosen);
            Assert.Equal(0, market.GetFilledCount(postingId));
            Assert.Equal(1, market.GetVacantSlots(postingId));
        }

        [Fact]
        public void HourDriver_AppliesAndQuitsOnTheGameHourHook()
        {
            Citizen person = CitizenOf(11);
            Citizen other = CitizenOf(12);
            WorldState world = WorldOf(person, other);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "工人", 2, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(Shift(postingId, 8f, 1f));
            var time = new TimeService();
            var driver = new LaborHourDriver(market, new SimpleJobSeekScorer(JobSeekScoreWeights.DemoDefaults));

            driver.RequestApply(person, new JobSeekContext());
            Assert.Equal(0, market.GetFilledCount(postingId));
            driver.Register(time);
            Assert.Equal(0, market.GetFilledCount(postingId));

            Assert.Equal(1, time.StepHours(1));
            Assert.Equal(1, market.GetFilledCount(postingId));
            Assert.Equal(1, market.GetVacantSlots(postingId));
            JobEmployment? hired;
            Assert.True(market.TryGetEmployment(person.Id, out hired));
            Assert.NotNull(hired);
            Assert.Equal(time.Now, hired.HiredAt);

            driver.RequestQuit(person);
            driver.RequestApply(person, new JobSeekContext());
            Assert.Equal(1, time.StepHours(1));
            JobEmployment? again;
            Assert.True(market.TryGetEmployment(person.Id, out again));
            Assert.NotNull(again);
            Assert.Equal(new GameDateTime(1, 2), again.HiredAt);
            Assert.Equal(1, market.GetFilledCount(postingId));

            driver.Unregister(time);
            driver.RequestApply(other, new JobSeekContext());
            time.StepHours(1);
            Assert.Equal(1, market.GetFilledCount(postingId));
        }

        [Fact]
        public void FatigueFormula_MatchesTheDesignEquation()
        {
            Assert.Equal(8f, FatigueFormula.Compute(0f, 8f, 1f, 1f, 0f));
            Assert.Equal(18f, FatigueFormula.Compute(0f, 12f, 1.5f, 1f, 0f));
            Assert.Equal(23f, FatigueFormula.Compute(10f, 8f, 1f, 2f, 3f));
        }

        [Fact]
        public void PublicSurface_HasNoForceAssignmentApi()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (Type type in typeof(LaborMarket).Assembly.GetTypes())
            {
                if (type.Name.IndexOf('<') >= 0)
                {
                    continue;
                }

                Assert.DoesNotContain("Assign", type.Name, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Recruitment", type.Name, StringComparison.OrdinalIgnoreCase);
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    if (method.Name.IndexOf('<') >= 0)
                    {
                        continue;
                    }

                    Assert.DoesNotContain("Assign", method.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("HireCitizen", method.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("ForceEmploy", method.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.False(TakesCitizenAndJob(method.GetParameters()), type.FullName + "." + method.Name);
                }

                foreach (ConstructorInfo constructor in type.GetConstructors(flags))
                {
                    Assert.False(TakesCitizenAndJob(constructor.GetParameters()), type.FullName + " ctor");
                }
            }

            string root = RepoRoot();
            foreach (string file in Directory.GetFiles(Path.Combine(root, "Assets", "Scripts", "Labor"), "*.cs"))
            {
                string text = File.ReadAllText(file);
                Assert.DoesNotContain("AssignCitizenToJob", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"farming\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"manufacturing\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"trade\"", text, StringComparison.Ordinal);
                Assert.DoesNotContain("\"administration\"", text, StringComparison.Ordinal);
                string name = Path.GetFileName(file);
                if (name == "SimpleJobSeekScorer.cs" || name == "JobSeeker.cs" || name == "LaborMarket.cs" || name == "LaborHourDriver.cs")
                {
                    Assert.DoesNotMatch(FloatLiteral, text);
                }
            }

            string weights = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "Labor", "JobSeekScoreWeights.cs"));
            Assert.Matches(FloatLiteral, weights);

            string noop = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "Core", "Time", "SimNoOp.cs"));
            Assert.DoesNotContain("TryApply", noop, StringComparison.Ordinal);
            Assert.DoesNotContain("Labor", noop, StringComparison.Ordinal);
        }

        private static bool TakesCitizenAndJob(ParameterInfo[] parameters)
        {
            bool citizen = false;
            bool job = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = parameters[i].ParameterType;
                if (parameterType.IsByRef)
                {
                    Type? element = parameterType.GetElementType();
                    if (element != null)
                    {
                        parameterType = element;
                    }
                }

                Type? underlying = Nullable.GetUnderlyingType(parameterType);
                if (underlying != null)
                {
                    parameterType = underlying;
                }

                if (parameterType == typeof(Citizen) || parameterType == typeof(CitizenId))
                {
                    citizen = true;
                }

                if (parameterType == typeof(SocialEvolution.Domain.JobPosting)
                    || parameterType == typeof(JobPostingId)
                    || parameterType == typeof(SocialEvolution.Labor.JobPosting))
                {
                    job = true;
                }
            }

            return citizen && job;
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
            return CitizenOf(id, 30, EducationLevel.Secondary, skills);
        }

        private static Citizen CitizenOf(int id, int age, EducationLevel education, params SkillLevel[] skills)
        {
            return new Citizen(
                new CitizenId(id),
                "市民" + id.ToString("000"),
                age,
                education,
                skills,
                1f,
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

        private static readonly Regex FloatLiteral = new Regex(@"\d+(\.\d+)?f", RegexOptions.CultureInvariant);
    }
}
