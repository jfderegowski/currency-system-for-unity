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

            [NonSerialized] internal int Pending;
        }

        [Serializable]
        public class ReasonStat
        {
            public SpendReason Reason;
            public IntStat Stat;

            [NonSerialized] internal int Pending;
        }

        [SerializeField] private IntStat earnedTotal;
        [SerializeField] private IntStat spentTotal;
        [SerializeField] private List<SourceStat> earnedBySource = new();

        [SerializeField, Tooltip("Spending without a reason only counts into spentTotal")]
        private List<ReasonStat> spentByReason = new();

        [SerializeField, Min(0), Tooltip("Seconds between pushes")]
        private float pushInterval = 30f;

        [NonSerialized] private int _earnedPending;
        [NonSerialized] private int _spentPending;
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

            foreach (var reasonStat in spentByReason)
                reasonStat.Pending = 0;
        }

        public override void OnEarn(Currency currency, int amount, CurrencySource source)
        {
            _earnedPending += amount;

            foreach (var sourceStat in earnedBySource)
                if (sourceStat.Source == source)
                    sourceStat.Pending += amount;

            PushIfDue();
        }

        public override void OnSpend(Currency currency, int amount, SpendReason reason)
        {
            _spentPending += amount;

            if (reason)
                foreach (var reasonStat in spentByReason)
                    if (reasonStat.Reason == reason)
                        reasonStat.Pending += amount;

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

            foreach (var reasonStat in spentByReason)
                Add(reasonStat.Stat, ref reasonStat.Pending);
        }

        private static void Add(IntStat stat, ref int pending)
        {
            if (!stat || pending <= 0) return;

            stat.Value = pending > int.MaxValue - stat.Value ? int.MaxValue : stat.Value + pending;
            pending = 0;

            _ = stat.PushAsync();
        }
    }
}
