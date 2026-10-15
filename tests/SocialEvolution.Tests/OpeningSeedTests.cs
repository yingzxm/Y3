using System;
using System.Collections.Generic;
using System.Reflection;
using SocialEvolution.Bootstrap;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Economy;
using SocialEvolution.World;
using Xunit;

namespace SocialEvolution.Tests
{
    public class OpeningSeedTests
    {
        [Fact]
        public void Create_MatchesP0OpeningNumbers()
        {
            WorldState world = OpeningSeed.Create();

            Assert.Equal(120, OpeningSeed.CitizenCount);
            Assert.Equal(120, world.Citizens.Count);
            Assert.Equal(500_000L, OpeningSeed.InitialTreasury);
            Assert.Equal(500_000L, world.Treasury.Balance);
            Assert.Equal(2_400, OpeningSeed.GovernmentFood);

            for (int i = 0; i < world.Citizens.Count; i++)
            {
                Assert.Equal(i + 1, world.Citizens[i].Id.Value);
                Assert.Equal("市民" + (i + 1).ToString("000"), world.Citizens[i].Name);
            }

            Assert.Equal(8, world.Sites.Count);
            Assert.Equal(1, Count(world, BuildingKind.Farm));
            Assert.Equal(1, Count(world, BuildingKind.Factory));
            Assert.Equal(1, Count(world, BuildingKind.Market));
            Assert.Equal(1, Count(world, BuildingKind.Government));
            Assert.Equal(OpeningSeed.HousingCount, Count(world, BuildingKind.Housing));

            int residentCapacity = 0;
            foreach (BuildingSite site in world.Sites)
            {
                if (site.Building.Kind == BuildingKind.Housing)
                {
                    residentCapacity += site.Building.HousingResidentCapacity;
                    Assert.Empty(site.Building.JobPostings);
                }
            }

            Assert.Equal(OpeningSeed.CitizenCount, residentCapacity);

            BuildingId government = new BuildingId(OpeningSeed.GovernmentBuildingId);
            Assert.Equal(OpeningSeed.GovernmentFood, world.GetQuantity(government, Good.Food));
            Assert.Equal(OpeningSeed.GovernmentWood, world.GetQuantity(government, Good.Wood));
            Assert.Equal(OpeningSeed.GovernmentTools, world.GetQuantity(government, Good.Tool));

            BuildingId farm = new BuildingId(OpeningSeed.FarmBuildingId);
            Assert.Equal(OpeningSeed.FarmFood, world.GetQuantity(farm, Good.Food));
            Assert.Equal(0, world.GetQuantity(farm, Good.Wood));
            Assert.Equal(OpeningSeed.FarmTools, world.GetQuantity(farm, Good.Tool));

            BuildingId factory = new BuildingId(OpeningSeed.FactoryBuildingId);
            Assert.Equal(OpeningSeed.FactoryWood, world.GetQuantity(factory, Good.Wood));
            Assert.Equal(0, world.GetQuantity(factory, Good.Food));
            Assert.Equal(0, world.GetQuantity(factory, Good.Tool));

            BuildingId market = new BuildingId(OpeningSeed.MarketBuildingId);
            Assert.Equal(0, world.GetQuantity(market, Good.Food));
            Assert.Equal(0, world.GetQuantity(market, Good.Wood));
            Assert.Equal(0, world.GetQuantity(market, Good.Tool));
        }

        [Fact]
        public void JobPostings_KeepOpeningsWhenReconfigured()
        {
            WorldState world = OpeningSeed.Create();
            int postings = 0;
            foreach (BuildingSite site in world.Sites)
            {
                foreach (JobPosting posting in site.Building.JobPostings)
                {
                    postings++;
                    Assert.True(posting.Openings > 0);
                    Assert.True(SkillIds.IsKnown(posting.RequiredSkillId));
                    Assert.Null(typeof(JobPosting).GetProperty("VacantSlots"));
                    Assert.Null(typeof(JobPosting).GetProperty("FilledCount"));
                }
            }

            Assert.Equal(5, postings);

            BuildingId farm = new BuildingId(OpeningSeed.FarmBuildingId);
            JobPosting updated = world.SetJobPosting(
                farm,
                new JobPostingId(OpeningSeed.FarmerPostingId),
                "农民",
                7,
                40L,
                9,
                SkillIds.Farming);

            Assert.Equal(7, updated.Openings);
            Assert.Equal(40L, updated.WagePerHour);
            Assert.Equal(9, updated.MinSkillLevel);
            Assert.Equal(SkillIds.Farming, updated.RequiredSkillId);
            Assert.Equal(120, world.Citizens.Count);

            Assert.Throws<InvalidOperationException>(() => world.SetJobPosting(
                new BuildingId(OpeningSeed.FactoryBuildingId),
                new JobPostingId(OpeningSeed.FarmerPostingId),
                "农民",
                1,
                1L,
                0,
                SkillIds.Farming));

            Assert.True(world.RemoveJobPosting(farm, new JobPostingId(OpeningSeed.FarmLeadPostingId)));
            Assert.False(world.RemoveJobPosting(farm, new JobPostingId(OpeningSeed.FarmLeadPostingId)));
            Assert.Equal(120, world.Citizens.Count);
        }

        [Fact]
        public void SteppingTheClock_RunsTheMarketWithoutTouchingTreasury()
        {
            GameSession session = GameSession.CreateNew();
            long balance = session.World.Treasury.Balance;
            int food = session.World.GetQuantity(new BuildingId(OpeningSeed.GovernmentBuildingId), Good.Food);
            int citizens = session.World.Citizens.Count;

            var openings = new Dictionary<int, int>();
            var homes = new BuildingId[citizens];
            for (int i = 0; i < citizens; i++)
            {
                homes[i] = session.World.Citizens[i].ResidenceId;
            }

            foreach (BuildingSite site in session.World.Sites)
            {
                foreach (JobPosting posting in site.Building.JobPostings)
                {
                    openings[posting.Id.Value] = posting.Openings;
                }
            }

            session.Time.SetTimeScale(TimeScales.X5);
            Assert.Equal(5, session.Time.AdvanceRealtime(1f));
            session.Time.StepHours(48);

            Assert.Equal(balance, session.World.Treasury.Balance);
            Assert.Equal(food, session.World.GetQuantity(new BuildingId(OpeningSeed.GovernmentBuildingId), Good.Food));
            Assert.Equal(53 * StateProduction.FarmFoodPerHour, session.World.GetQuantity(new BuildingId(OpeningSeed.MarketBuildingId), Good.Food));
            Assert.Equal(citizens, session.World.Citizens.Count);
            for (int i = 0; i < citizens; i++)
            {
                Assert.Equal(homes[i], session.World.Citizens[i].ResidenceId);
            }

            foreach (BuildingSite site in session.World.Sites)
            {
                foreach (JobPosting posting in site.Building.JobPostings)
                {
                    Assert.Equal(openings[posting.Id.Value], posting.Openings);
                }
            }
        }

        [Fact]
        public void Citizens_HaveDeterministicProfilesAndFillFourHouses()
        {
            WorldState world = OpeningSeed.Create();
            var perHouse = new Dictionary<int, int>();
            for (int i = 0; i < world.Citizens.Count; i++)
            {
                Citizen citizen = world.Citizens[i];
                IPersonReadModel person = citizen;
                Assert.Equal(citizen.Id, person.Id);
                Assert.Equal(OpeningSeed.AgeOf(i), citizen.Age);
                Assert.Equal(OpeningSeed.EducationOf(i), citizen.Education);
                Assert.Equal(OpeningSeed.HealthFactorOf(i), citizen.HealthFactor);
                Assert.Equal(OpeningSeed.WealthOf(i), citizen.Wealth);
                Assert.Equal(OpeningSeed.ResidenceOf(i), citizen.ResidenceId);
                SkillLevel[] expectedSkills = OpeningSeed.SkillsOf(i);
                Assert.Equal(expectedSkills.Length, citizen.Skills.Count);
                for (int skillIndex = 0; skillIndex < expectedSkills.Length; skillIndex++)
                {
                    Assert.Equal(expectedSkills[skillIndex], citizen.Skills[skillIndex]);
                }
                if (citizen.Age < 16)
                {
                    Assert.Empty(citizen.Skills);
                }
                else
                {
                    Assert.NotEmpty(citizen.Skills);
                }

                int houseId = citizen.ResidenceId.Value;
                int count;
                if (!perHouse.TryGetValue(houseId, out count))
                {
                    count = 0;
                }

                perHouse[houseId] = count + 1;
            }

            Assert.Equal(OpeningSeed.HousingCount, perHouse.Count);
            for (int house = 0; house < OpeningSeed.HousingCount; house++)
            {
                Assert.Equal(OpeningSeed.HousingResidentCapacity, perHouse[OpeningSeed.FirstHousingBuildingId + house]);
            }

            WorldState again = OpeningSeed.Create();
            Assert.Equal(world.Citizens[40].Wealth, again.Citizens[40].Wealth);
            Assert.Equal(world.Citizens[40].ResidenceId, again.Citizens[40].ResidenceId);
            Assert.Equal(world.Citizens[40].Skills.Count, again.Citizens[40].Skills.Count);
        }

        [Fact]
        public void WorldState_RejectsNonHousingResidence()
        {
            var sites = new[]
            {
                new BuildingSite(
                    new Building(new BuildingId(2), BuildingKind.Farm, "农场"),
                    new Inventory(10)),
                new BuildingSite(
                    new Building(new BuildingId(5), BuildingKind.Housing, "住宅", 1),
                    new Inventory(10)),
            };
            var citizen = new Citizen(
                new CitizenId(1),
                "甲",
                30,
                EducationLevel.Secondary,
                new SkillLevel[] { new SkillLevel(SkillIds.Farming, 1) },
                1f,
                100L,
                new BuildingId(2));

            Assert.Throws<ArgumentException>(() => new WorldState(new[] { citizen }, sites, new Treasury(0L)));
        }

        private static int Count(WorldState world, BuildingKind kind)
        {
            int count = 0;
            foreach (BuildingSite site in world.Sites)
            {
                if (site.Building.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
