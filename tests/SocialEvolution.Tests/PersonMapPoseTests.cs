using System;
using System.Collections.Generic;
using SocialEvolution.Agents;
using SocialEvolution.Bootstrap;
using SocialEvolution.Core;
using SocialEvolution.Domain;
using SocialEvolution.Labor;
using SocialEvolution.Stats;
using SocialEvolution.World;
using Xunit;

namespace SocialEvolution.Tests
{
    public class PersonMapPoseTests
    {
        private static readonly BuildingId HomeId = new BuildingId(5);
        private static readonly BuildingId FactoryId = new BuildingId(3);

        [Fact]
        public void OpeningSeed_EnumeratesEveryCitizenOutdoorsOnStableCells()
        {
            GameSession session = GameSession.CreateNew();
            Assert.Null(session.MapPresence);
            AgentsHourDriver driver = DriverFor(session.World, out _);
            IPersonMapPresence presence = session.AttachMapPresence(driver);

            Assert.Equal(OpeningSeed.CitizenCount, presence.Count);
            IReadOnlyList<IPersonMapPose> poses = presence.Poses;
            Assert.Equal(OpeningSeed.CitizenCount, poses.Count);

            var seen = new HashSet<int>();
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            for (int i = 0; i < poses.Count; i++)
            {
                IPersonMapPose pose = poses[i];
                Assert.Equal(session.World.Citizens[i].Id, pose.CitizenId);
                Assert.False(pose.InBuilding);
                int cellX;
                int cellY;
                PersonMapCells.OutdoorCell(pose.CitizenId, out cellX, out cellY);
                Assert.Equal(cellX, pose.CellX);
                Assert.Equal(cellY, pose.CellY);
                Assert.True(seen.Add((cellX * 1000) + cellY));
                if (cellX < minX)
                {
                    minX = cellX;
                }

                if (cellX > maxX)
                {
                    maxX = cellX;
                }

                if (cellY < minY)
                {
                    minY = cellY;
                }

                if (cellY > maxY)
                {
                    maxY = cellY;
                }
            }

            Assert.Equal(120, seen.Count);
            Assert.Equal(PersonMapCells.OriginX, poses[0].CellX);
            Assert.Equal(PersonMapCells.OriginY, poses[0].CellY);
            Assert.Equal(21, poses[119].CellX);
            Assert.Equal(20, poses[119].CellY);
            Assert.InRange(PersonMapCells.CityCenterX, minX, maxX);
            Assert.InRange(PersonMapCells.CityCenterY, minY, maxY);
            Assert.InRange(minX, 0, DemoMapCellSpace.MapWidth - 1);
            Assert.InRange(maxX, 0, DemoMapCellSpace.MapWidth - 1);
            Assert.InRange(minY, 0, DemoMapCellSpace.MapHeight - 1);
            Assert.InRange(maxY, 0, DemoMapCellSpace.MapHeight - 1);

            float worldX;
            float worldY;
            float worldZ;
            DemoMapCellSpace.ToWorld(0, 0, out worldX, out worldY, out worldZ);
            Assert.Equal(-19.5f, worldX);
            Assert.Equal(-11.5f, worldY);
            Assert.Equal(0f, worldZ);
            DemoMapCellSpace.ToWorld(poses[0].CellX, poses[0].CellY, out worldX, out worldY, out worldZ);
            Assert.Equal(poses[0].CellX - 19.5f, worldX);
            Assert.Equal(poses[0].CellY - 11.5f, worldY);
            Assert.Equal(-9.5f, worldX);
            Assert.Equal(-0.5f, worldY);
            Assert.Equal(0f, worldZ);
            DemoMapCellSpace.ToWorld(21, 20, out worldX, out worldY, out worldZ);
            Assert.Equal(1.5f, worldX);
            Assert.Equal(8.5f, worldY);
            Assert.Equal(0f, worldZ);
            Assert.Equal(-20f, DemoMapCellSpace.MapOriginX);
            Assert.Equal(-12f, DemoMapCellSpace.MapOriginY);
            Assert.Equal(1f, DemoMapCellSpace.CellSizeX);
            Assert.Equal(1f, DemoMapCellSpace.CellSizeY);

            IPersonMapPose? missing;
            Assert.False(presence.TryGet(new CitizenId(999), out missing));
            Assert.Null(missing);

            IPersonMapPose? first;
            Assert.True(presence.TryGet(new CitizenId(1), out first));
            Assert.NotNull(first);
            Assert.Equal(poses[0].CellX, first!.CellX);
            Assert.Equal(poses[0].CellY, first.CellY);
            Assert.False(first.InBuilding);
        }

        [Fact]
        public void InBuilding_IsTrueOnlyWhileWorkingAtAWorkplace()
        {
            Citizen worker = CitizenOf(4);
            Citizen other = CitizenOf(5);
            WorldState world = WorldOf(worker, other);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "工人", 1, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(new JobConditions(postingId, 8f, 1f, WorkShift.Day));

            var script = new FixedDecision { Next = AgentAction.Work };
            var brain = new AgentBrain(worker, market, script);
            var idle = new AgentBrain(other, market, new FixedDecision { Next = AgentAction.Idle });
            var driver = new AgentsHourDriver(new[] { brain });
            var presence = new PersonMapPresence(driver);
            var time = new TimeService();
            driver.Register(time);

            IPersonMapPose? pose;
            Assert.True(presence.TryGet(worker.Id, out pose));
            Assert.False(pose!.InBuilding);
            Assert.False(brain.IsInsideWorkplace);
            Assert.Equal(AgentAction.None, brain.LastAction);

            time.StepHours(1);
            Assert.Equal(AgentAction.Work, brain.LastAction);
            Assert.False(brain.IsEmployed);
            Assert.False(Read(presence, worker.Id).InBuilding);

            Assert.Equal(LaborApplyResult.Hired, market.TryApply(worker, postingId, time.Now));
            int cellX = Read(presence, worker.Id).CellX;
            int cellY = Read(presence, worker.Id).CellY;

            AgentAction[] outdoors =
            {
                AgentAction.Eat,
                AgentAction.Sleep,
                AgentAction.Shop,
                AgentAction.Idle,
                AgentAction.SeekJob,
            };
            for (int i = 0; i < outdoors.Length; i++)
            {
                script.Next = outdoors[i];
                time.StepHours(1);
                IPersonMapPose outdoor = Read(presence, worker.Id);
                Assert.Equal(outdoors[i], brain.LastAction);
                Assert.True(brain.IsEmployed);
                Assert.False(brain.IsInsideWorkplace);
                Assert.False(outdoor.InBuilding);
                Assert.Equal(cellX, outdoor.CellX);
                Assert.Equal(cellY, outdoor.CellY);
            }

            script.Next = AgentAction.Work;
            time.StepHours(1);
            IPersonMapPose working = Read(presence, worker.Id);
            Assert.Equal(AgentAction.Work, brain.LastAction);
            Assert.True(brain.IsInsideWorkplace);
            Assert.True(working.InBuilding);
            Assert.Equal(cellX, working.CellX);
            Assert.Equal(cellY, working.CellY);

            script.Next = AgentAction.Quit;
            time.StepHours(1);
            Assert.Equal(AgentAction.Quit, brain.LastAction);
            Assert.False(brain.IsEmployed);
            Assert.False(Read(presence, worker.Id).InBuilding);

            script.Next = AgentAction.Work;
            time.StepHours(1);
            Assert.Equal(AgentAction.Work, brain.LastAction);
            Assert.False(Read(presence, worker.Id).InBuilding);

            Assert.Equal(LaborApplyResult.Hired, market.TryApply(worker, postingId, time.Now));
            script.Next = AgentAction.Work;
            time.StepHours(1);
            Assert.True(Read(presence, worker.Id).InBuilding);
            Assert.True(world.RemoveJobPosting(FactoryId, postingId));
            Assert.Equal(AgentAction.Work, brain.LastAction);
            Assert.False(brain.IsInsideWorkplace);
            Assert.False(Read(presence, worker.Id).InBuilding);
            Assert.Equal(cellX, Read(presence, worker.Id).CellX);
            Assert.Equal(cellY, Read(presence, worker.Id).CellY);

            driver.Add(idle);
            Assert.Equal(2, presence.Count);
            Assert.False(Read(presence, other.Id).InBuilding);
            Assert.True(driver.Remove(idle));
            IPersonMapPose? gone;
            Assert.False(presence.TryGet(other.Id, out gone));
            Assert.Null(gone);
        }

        [Fact]
        public void Changed_FiresOncePerHourAfterTheAction_AndPollWorksWithoutIt()
        {
            Citizen person = CitizenOf(7);
            WorldState world = WorldOf(person);
            var postingId = new JobPostingId(1);
            world.SetJobPosting(FactoryId, postingId, "工人", 1, 20L, 0, SkillIds.Manufacturing);
            var market = new LaborMarket(world);
            market.SetConditions(new JobConditions(postingId, 8f, 1f, WorkShift.Day));
            Assert.Equal(LaborApplyResult.Hired, market.TryApply(person, postingId, GameDateTime.Origin));

            var script = new FixedDecision { Next = AgentAction.Work };
            var brain = new AgentBrain(person, market, script);
            var driver = new AgentsHourDriver(new[] { brain });
            var presence = new PersonMapPresence(driver);
            var time = new TimeService();
            driver.Register(time);

            int events = 0;
            bool insideAtEvent = false;
            presence.Changed += when =>
            {
                events++;
                insideAtEvent = Read(presence, person.Id).InBuilding;
                Assert.Equal(time.Now, when);
            };

            presence.FollowHours();
            presence.FollowHours();
            Assert.Equal(0, events);
            Assert.Equal(1, time.StepHours(1));
            Assert.Equal(1, events);
            Assert.True(insideAtEvent);
            Assert.True(Read(presence, person.Id).InBuilding);

            presence.UnfollowHours();
            presence.UnfollowHours();
            script.Next = AgentAction.Sleep;
            Assert.Equal(1, time.StepHours(1));
            Assert.Equal(1, events);
            Assert.Equal(AgentAction.Sleep, brain.LastAction);
            Assert.True(brain.IsEmployed);
            Assert.False(Read(presence, person.Id).InBuilding);
        }

        [Fact]
        public void GameSession_AttachMapPresence_ReplacesThePreviousListener()
        {
            GameSession session = GameSession.CreateNew();
            AgentsHourDriver driver = DriverFor(session.World, out _);
            IPersonMapPresence first = session.AttachMapPresence(driver);
            int firstEvents = 0;
            first.Changed += _ => firstEvents++;

            IPersonMapPresence second = session.AttachMapPresence(driver);
            Assert.Same(second, session.MapPresence);
            Assert.NotSame(first, second);
            int secondEvents = 0;
            second.Changed += _ => secondEvents++;

            driver.Register(session.Time);
            session.Time.StepHours(1);
            Assert.Equal(0, firstEvents);
            Assert.Equal(1, secondEvents);
            Assert.Equal(OpeningSeed.CitizenCount, second.Poses.Count);
            Assert.False(second.Poses[0].InBuilding);
        }

        [Fact]
        public void EnsureReadout_RegistersTheHourDriverOnce()
        {
            GameSession session = GameSession.CreateNew();
            DemoReadout first = session.EnsureReadout();
            DemoReadout second = session.EnsureReadout();
            Assert.Same(first, second);
            Assert.Same(first.Agents, second.Agents);
            Assert.Equal(OpeningSeed.CitizenCount, first.Agents.BrainCount);
            session.Time.StepHours(1);
            Assert.Equal(1, first.Agents.HoursProcessed);
        }

        [Fact]
        public void RejectsNullDriver()
        {
            GameSession session = GameSession.CreateNew();
            Assert.Throws<ArgumentNullException>(() => new PersonMapPresence(null!));
            Assert.Throws<ArgumentNullException>(() => session.AttachMapPresence(null!));
        }

        private static IPersonMapPose Read(IPersonMapPresence presence, CitizenId id)
        {
            IPersonMapPose? pose;
            Assert.True(presence.TryGet(id, out pose));
            Assert.NotNull(pose);
            return pose!;
        }

        private static AgentsHourDriver DriverFor(WorldState world, out LaborMarket market)
        {
            market = new LaborMarket(world);
            var brains = new List<AgentBrain>(world.Citizens.Count);
            for (int i = 0; i < world.Citizens.Count; i++)
            {
                brains.Add(new AgentBrain(world.Citizens[i], market));
            }

            return new AgentsHourDriver(brains);
        }

        private static Citizen CitizenOf(int id)
        {
            return new Citizen(
                new CitizenId(id),
                "市民" + id.ToString("000"),
                30,
                EducationLevel.Secondary,
                Array.Empty<SkillLevel>(),
                1f,
                0L,
                HomeId);
        }

        private static WorldState WorldOf(params Citizen[] citizens)
        {
            var housing = new Building(HomeId, BuildingKind.Housing, "公共住宅", 30);
            var factory = new Building(FactoryId, BuildingKind.Factory, "工具工坊");
            var sites = new BuildingSite[]
            {
                new BuildingSite(housing, new Inventory(200)),
                new BuildingSite(factory, new Inventory(100)),
            };
            return new WorldState(citizens, sites, new Treasury(0));
        }

        private sealed class FixedDecision : IDecisionMaker
        {
            public AgentAction Next { get; set; }

            public AgentAction Decide(in AgentDecisionContext context)
            {
                return Next;
            }
        }
    }
}
