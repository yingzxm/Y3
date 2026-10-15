#nullable enable
using System;

namespace SocialEvolution.Core
{
    public sealed class TimeService : ITimeService
    {
        public const float DefaultSecondsPerGameHour = 1f;

        private readonly SimStepHooks _hooks = new SimStepHooks();
        private readonly double _secondsPerGameHour;
        private double _bankedSeconds;
        private float _timeScale = TimeScales.X1;
        private int _stepDepth;

        public TimeService()
            : this(DefaultSecondsPerGameHour, TimeScales.X1)
        {
        }

        public TimeService(float secondsPerGameHour, float timeScale)
        {
            if (!float.IsFinite(secondsPerGameHour) || secondsPerGameHour <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(secondsPerGameHour),
                    secondsPerGameHour,
                    "每个游戏时对应的现实秒数必须是有限正数。");
            }

            _secondsPerGameHour = secondsPerGameHour;
            Now = GameDateTime.Origin;
            SetTimeScale(timeScale);
        }

        public GameDateTime Now { get; private set; }

        public int Day => Now.Day;

        public int Hour => Now.Hour;

        public bool IsPaused { get; private set; }

        public float TimeScale => _timeScale;

        public float SecondsPerGameHour => (float)_secondsPerGameHour;

        public void Pause() => IsPaused = true;

        public void Resume() => IsPaused = false;

        public void SetTimeScale(float scale)
        {
            if (!TimeScales.IsSupported(scale))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scale),
                    scale,
                    "时间倍率必须是有限正数。暂停请调用 Pause。");
            }

            _timeScale = scale;
        }

        public int AdvanceRealtime(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "现实秒数必须是非负有限值。");
            }

            if (deltaSeconds == 0f || IsPaused)
            {
                return 0;
            }

            _bankedSeconds += (double)deltaSeconds * _timeScale;
            double produced = _bankedSeconds / _secondsPerGameHour;
            if (produced >= int.MaxValue)
            {
                throw new OverflowException("单次推进的游戏时过多。");
            }

            int hours = (int)Math.Floor(produced + 1e-9d);
            if (hours <= 0)
            {
                return 0;
            }

            _bankedSeconds -= hours * _secondsPerGameHour;
            if (_bankedSeconds < 0d && _bankedSeconds > -1e-8d)
            {
                _bankedSeconds = 0d;
            }

            return StepHoursCore(hours);
        }

        public int StepHours(int gameHours)
        {
            if (gameHours < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameHours), gameHours, "游戏时不能为负。");
            }

            return StepHoursCore(gameHours);
        }

        public void RegisterGameHour(Action<GameDateTime> callback) => _hooks.RegisterGameHour(callback);

        public void UnregisterGameHour(Action<GameDateTime> callback) => _hooks.UnregisterGameHour(callback);

        public void RegisterGameDay(Action<GameDateTime> callback) => _hooks.RegisterGameDay(callback);

        public void UnregisterGameDay(Action<GameDateTime> callback) => _hooks.UnregisterGameDay(callback);

        private int StepHoursCore(int gameHours)
        {
            if (gameHours == 0)
            {
                return 0;
            }

            if (_stepDepth > 0)
            {
                throw new InvalidOperationException("模拟步进不可重入。请在回调之外推进时钟。");
            }

            _stepDepth++;
            try
            {
                for (int i = 0; i < gameHours; i++)
                {
                    Now = Now.AddHours(1);
                    _hooks.RaiseHour(Now);
                    if (Now.Hour == 0)
                    {
                        _hooks.RaiseDay(Now);
                    }
                }
            }
            finally
            {
                _stepDepth--;
            }

            return gameHours;
        }
    }
}
