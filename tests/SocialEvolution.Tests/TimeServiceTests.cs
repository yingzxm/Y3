using SocialEvolution.Core;
using Xunit;

namespace SocialEvolution.Tests
{
    public class TimeServiceTests
    {
        [Fact]
        public void DayIsTwentyFourHours()
        {
            Assert.Equal(24, GameDateTime.HoursPerDay);
            Assert.Equal(new GameDateTime(1, 0), GameDateTime.Origin);
            Assert.Equal(new GameDateTime(1, 23), GameDateTime.Origin.AddHours(23));
            Assert.Equal(new GameDateTime(2, 0), GameDateTime.Origin.AddHours(24));
            Assert.Equal(new GameDateTime(2, 1), GameDateTime.Origin.AddHours(25));
        }

        [Fact]
        public void RejectsInvalidDates()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameDateTime(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameDateTime(1, 24));
            Assert.Throws<ArgumentOutOfRangeException>(() => GameDateTime.Origin.AddHours(-1));
        }

        [Fact]
        public void StepHours_FiresHourThenDayAtMidnight()
        {
            var time = new TimeService();
            var log = new List<string>();
            time.RegisterGameHour(now => log.Add("H " + now));
            time.RegisterGameDay(now => log.Add("D " + now));

            Assert.Equal(25, time.StepHours(25));

            Assert.Equal(new GameDateTime(2, 1), time.Now);
            Assert.Equal(25, log.Count(entry => entry.StartsWith("H ", StringComparison.Ordinal)));
            Assert.Equal(new[] { "D D2 00:00" }, log.Where(entry => entry.StartsWith("D ", StringComparison.Ordinal)).ToArray());

            int midnight = log.IndexOf("H D2 00:00");
            Assert.True(midnight >= 0);
            Assert.Equal("D D2 00:00", log[midnight + 1]);
        }

        [Fact]
        public void StepHours_ZeroAndUnregister_DoNotExtraFire()
        {
            var time = new TimeService();
            int calls = 0;
            Action<GameDateTime> callback = _ => calls++;
            time.RegisterGameHour(callback);
            time.RegisterGameHour(callback);

            Assert.Equal(0, time.StepHours(0));
            Assert.Equal(0, calls);

            Assert.Equal(1, time.StepHours(1));
            Assert.Equal(1, calls);

            time.UnregisterGameHour(callback);
            time.StepHours(1);
            Assert.Equal(1, calls);
            Assert.Equal(new GameDateTime(1, 2), time.Now);
        }

        [Fact]
        public void AdvanceRealtime_HonorsPauseAndScales()
        {
            var time = new TimeService();
            Assert.Equal(TimeScales.X1, time.TimeScale);
            Assert.False(time.IsPaused);

            Assert.Equal(1, time.AdvanceRealtime(1f));
            Assert.Equal(0, time.AdvanceRealtime(0.5f));
            Assert.Equal(1, time.AdvanceRealtime(0.5f));
            Assert.Equal(new GameDateTime(1, 2), time.Now);

            time.SetTimeScale(TimeScales.X5);
            Assert.Equal(2, time.AdvanceRealtime(0.5f));
            Assert.Equal(3, time.AdvanceRealtime(0.5f));
            Assert.Equal(new GameDateTime(1, 7), time.Now);

            time.Pause();
            Assert.True(time.IsPaused);
            Assert.Equal(TimeScales.X5, time.TimeScale);
            Assert.Equal(0, time.AdvanceRealtime(10f));
            Assert.Equal(new GameDateTime(1, 7), time.Now);

            Assert.Equal(1, time.StepHours(1));
            Assert.Equal(new GameDateTime(1, 8), time.Now);

            time.Resume();
            time.SetTimeScale(TimeScales.X1);
            Assert.Equal(1, time.AdvanceRealtime(1f));
        }

        [Fact]
        public void SetTimeScale_RejectsNonPositiveAndKeepsPrevious()
        {
            var time = new TimeService();
            time.SetTimeScale(TimeScales.X5);

            Assert.Throws<ArgumentOutOfRangeException>(() => time.SetTimeScale(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => time.SetTimeScale(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => time.SetTimeScale(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => time.SetTimeScale(float.PositiveInfinity));
            Assert.Equal(TimeScales.X5, time.TimeScale);

            time.SetTimeScale(TimeScales.X20);
            Assert.Equal(TimeScales.X20, time.TimeScale);
            Assert.Contains(TimeScales.X1, TimeScales.Presets);
            Assert.Contains(TimeScales.X5, TimeScales.Presets);
        }

        [Fact]
        public void HooksCannotReenterTheClock()
        {
            var time = new TimeService();
            time.RegisterGameHour(_ => time.StepHours(1));

            Assert.Throws<InvalidOperationException>(() => time.StepHours(1));
            Assert.Equal(new GameDateTime(1, 1), time.Now);
        }

        [Fact]
        public void UnregisterMissingCallback_IsIgnored()
        {
            var time = new TimeService();
            time.UnregisterGameHour(_ => { });
            time.UnregisterGameDay(_ => { });
            Assert.Equal(new GameDateTime(1, 3), time.Now.AddHours(0).AddHours(3));
            Assert.Equal(3, time.StepHours(3));
        }
    }
}
