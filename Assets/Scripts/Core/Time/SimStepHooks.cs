#nullable enable
using System;
using System.Collections.Generic;

namespace SocialEvolution.Core
{
    internal sealed class SimStepHooks : ISimStepHooks
    {
        private readonly List<Action<GameDateTime>> _hours = new List<Action<GameDateTime>>();
        private readonly List<Action<GameDateTime>> _days = new List<Action<GameDateTime>>();

        public void RegisterGameHour(Action<GameDateTime> callback) => Add(_hours, callback);

        public void UnregisterGameHour(Action<GameDateTime> callback) => Remove(_hours, callback);

        public void RegisterGameDay(Action<GameDateTime> callback) => Add(_days, callback);

        public void UnregisterGameDay(Action<GameDateTime> callback) => Remove(_days, callback);

        internal void RaiseHour(GameDateTime now) => Invoke(_hours, now);

        internal void RaiseDay(GameDateTime now) => Invoke(_days, now);

        private static void Add(List<Action<GameDateTime>> list, Action<GameDateTime> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (!list.Contains(callback))
            {
                list.Add(callback);
            }
        }

        private static void Remove(List<Action<GameDateTime>> list, Action<GameDateTime> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            list.Remove(callback);
        }

        private static void Invoke(List<Action<GameDateTime>> list, GameDateTime now)
        {
            if (list.Count == 0)
            {
                return;
            }

            Action<GameDateTime>[] snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i](now);
            }
        }
    }
}
