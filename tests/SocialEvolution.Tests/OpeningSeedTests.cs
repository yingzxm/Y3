using SocialEvolution.Bootstrap;
using SocialEvolution.Core;
using SocialEvolution.Domain;
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
                Assert.Equal($"市民{i + 1:000}", world.Citizens[i].Name);
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
        public void JobPostings_StayVacantWhenReconfigured()
        {
            WorldState world = OpeningSeed.Create();
            int postings = 0;
            foreach (BuildingSite site in world.Sites)
            {
                foreach (JobPosting posting in site.Building.JobPostings)
                {
                    postings++;
                    Assert.Equal(posting.Openings, posting.VacantSlots);
                    Assert.True(posting.Openings > 0);
                }
            }

            Assert.Equal(5, postings);

            BuildingId farm = new BuildingId(OpeningSeed.FarmBuildingId);
            JobPosting updated = world.SetJobPosting(
                farm,
                new JobPostingId(OpeningSeed.FarmerPostingId),
                "农民",
                7,
                40,
                9);

            Assert.Equal(7, updated.Openings);
            Assert.Equal(7, updated.VacantSlots);
            Assert.Equal(40, updated.WagePerHour);
            Assert.Equal(9, updated.MinSkillLevel);
            Assert.Equal(120, world.Citizens.Count);

            Assert.Throws<InvalidOperationException>(() => world.SetJobPosting(
                new BuildingId(OpeningSeed.FactoryBuildingId),
                new JobPostingId(OpeningSeed.FarmerPostingId),
                "农民",
                1,
                1,
                0));

            Assert.True(world.RemoveJobPosting(farm, new JobPostingId(OpeningSeed.FarmLeadPostingId)));
            Assert.False(world.RemoveJobPosting(farm, new JobPostingId(OpeningSeed.FarmLeadPostingId)));
            Assert.Equal(120, world.Citizens.Count);
        }

        [Fact]
        public void SteppingTheClock_DoesNotRunEconomy()
        {
            GameSession session = GameSession.CreateNew();
            long balance = session.World.Treasury.Balance;
            int food = session.World.GetQuantity(new BuildingId(OpeningSeed.GovernmentBuildingId), Good.Food);
            int citizens = session.World.Citizens.Count;

            session.Time.SetTimeScale(TimeScales.X5);
            Assert.Equal(5, session.Time.AdvanceRealtime(1f));
            session.Time.StepHours(48);

            Assert.Equal(balance, session.World.Treasury.Balance);
            Assert.Equal(food, session.World.GetQuantity(new BuildingId(OpeningSeed.GovernmentBuildingId), Good.Food));
            Assert.Equal(citizens, session.World.Citizens.Count);
            foreach (BuildingSite site in session.World.Sites)
            {
                foreach (JobPosting posting in site.Building.JobPostings)
                {
                    Assert.Equal(posting.Openings, posting.VacantSlots);
                }
            }
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
