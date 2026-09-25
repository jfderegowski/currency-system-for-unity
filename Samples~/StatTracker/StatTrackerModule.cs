using System;
using System.Collections.Generic;
using fefek5.Stats.Runtime;
using UnityEngine;

namespace fefek5.Currency.Samples
{
    using Runtime;

    /// <summary>
    /// Counts earned and spent currency into IntStats (e.g. Steam stats). Amounts are collected in memory and
    /// pushed every pushInterval seconds and on quit, not on every earn - passive income and clicks earn
    /// several times per second.
    /// </summary>
    [Serializable]
    public class StatTrackerModule : CurrencyModule
    {
        [Serializable]
        public class SourceStat
        {
            public CurrencySource Source;
            public IntStat Stat;

            [NonSerialized] internal long Pending;
        }

        [SerializeField] private IntStat earnedTotal;
        [SerializeField] private IntStat spentTotal;
        [SerializeField] private List<SourceStat> earnedBySource = new();

        [SerializeField, Min(1), Tooltip("1000 counts the stat in thousands. Steam INT stats are int32 (~2.1 billion)")]
        private long divisor = 1;

        [SerializeField, Min(0), Tooltip("Seconds between pushes")]
        private float pushInterval = 30f;

        [NonSerialized] private long _earnedPending;
        [NonSerialized] private long _spentPending;
        [NonSerialized] private float _nextPushTime;

        public override void Initialize(Currency currency)
        {
            _nextPushTime = Time.unscaledTime + pushInterval;
            Application.quitting += Push;
        }

        public override void Dispose()
        {
            Application.quitting -= Push;
            _earnedPending = 0;
            _spentPending = 0;

            foreach (var sourceStat in earnedBySource)
                sourceStat.Pending = 0;
        }

        public override void OnEarn(Currency currency, long amount, CurrencySource source)
        {
            _earnedPending += amount;

            foreach (var sourceStat in earnedBySource)
                if (sourceStat.Source == source)
                    sourceStat.Pending += amount;

            PushIfDue();
        }

        public override void OnSpend(Currency currency, long amount, SpendReason reason)
        {
            _spentPending += amount;

            PushIfDue();
        }

        private void PushIfDue()
        {
            if (Time.unscaledTime < _nextPushTime) return;

            Push();
        }

        private void Push()
        {
            _nextPushTime = Time.unscaledTime + pushInterval;

            Add(earnedTotal, ref _earnedPending);
            Add(spentTotal, ref _spentPending);

            foreach (var sourceStat in earnedBySource)
                Add(sourceStat.Stat, ref sourceStat.Pending);
        }

        // Moves whole divisor units into the stat; the remainder waits for the next push
        private void Add(IntStat stat, ref long pending)
        {
            if (!stat) return;

            var units = pending / Math.Max(1, divisor);

            if (units <= 0) return;

            pending -= units * Math.Max(1, divisor);
            stat.Value = (int)Math.Min(int.MaxValue, stat.Value + units);

            _ = stat.PushAsync();
        }
    }
}
