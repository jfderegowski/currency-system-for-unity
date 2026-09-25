using System;
using System.Collections.Generic;
using System.Threading;
using fefek5.SaveDataVariable.Runtime;
using fefek5.Toys.Runtime.Attributes;
using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>
    /// One currency and the player's balance of it. The balance lives in memory and in a SaveVar;
    /// changes stay in memory until Save, Wallet.SaveAll or the CurrencyAutoSaver writes them to disk.
    /// Detailed events (OnEarn, OnSpend) are raised before OnChanged, and modules hear about a change
    /// before outside listeners.
    /// </summary>
    [CreateAssetMenu(fileName = "New Currency", menuName = "Currency/Currency")]
    public class Currency : ScriptableObject
    {
        #region Inspector Fields

        [field: SerializeField] public string DisplayName { get; private set; }

        [field: SerializeField] public Sprite Icon { get; private set; }

        [field: SerializeField, Min(0)] public int StartingAmount { get; private set; }

        [field: SerializeField, Min(0), Tooltip("0 = no limit")]
        public int MaxAmount { get; private set; }

        [SerializeField] private SaveVar<int> _value = new("Currencies.json", new SaveKey("CURRENCY_NAME"));

        [field: SerializeReference, SelectType]
        public ICurrencyFormatter Formatter { get; private set; } = new AbbreviatedFormatter();

        [field: SerializeReference, SelectType]
        public List<CurrencyModule> Modules { get; private set; } = new();

        #endregion

        #region Properties

        public int Value
        {
            get
            {
                EnsureLoaded();
                return _cached;
            }
        }

        /// <summary>The balance changed and was not written to disk yet.</summary>
        public bool IsDirty => _value.IsDirty;

        public SaveKey SaveKey => _value.SaveKey;

        /// <summary>Save file, relative to Application.persistentDataPath.</summary>
        public string SaveRelativePath => _value.RelativePath;

        internal string SavePath => _value.Path;

        private int Limit => MaxAmount > 0 ? MaxAmount : int.MaxValue;

        #endregion

        #region Events

        /// <summary>New balance after Earn, TrySpend or Set.</summary>
        public event Action<int> OnChanged;

        /// <summary>Amount actually added (after modules and MaxAmount) and where it came from.</summary>
        public event Action<int, CurrencySource> OnEarn;

        /// <summary>Amount taken and what it was spent on (can be null).</summary>
        public event Action<int, SpendReason> OnSpend;

        /// <summary>New balance after SetWithoutNotify, for listeners that only display it (UI).</summary>
        public event Action<int> OnChangedWithoutNotify;

        #endregion

        #region Runtime State

        [NonSerialized] private int _cached;
        [NonSerialized] private bool _loaded;

        #endregion

        public bool CanAfford(int amount) => amount <= Value;

        public void Earn(int amount, CurrencySource source)
        {
            if (amount <= 0) return;

            if (!source)
                Debug.LogWarning($"{name}: Earn({amount}) without a CurrencySource.", this);

            var oldValue = Value;

            foreach (var module in Modules)
            {
                if (module == null) continue;

                try
                {
                    amount = module.ModifyEarn(this, amount, source);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }

            if (amount <= 0) return;

            var limit = Limit;
            var newValue = oldValue >= limit || amount > limit - oldValue ? limit : oldValue + amount;
            var realAmount = newValue - oldValue;

            if (realAmount <= 0) return;

            Apply(newValue);

            ForEachModule(module => module.OnEarn(this, realAmount, source));
            OnEarn?.Invoke(realAmount, source);
            Wallet.RaiseEarn(this, realAmount, source);

            RaiseChanged();
        }

        /// <summary>Takes the amount only if the balance covers all of it.</summary>
        /// <returns>True when the amount was taken (always for 0).</returns>
        public bool TrySpend(int amount, SpendReason reason = null)
        {
            if (amount < 0) return false;
            if (amount == 0) return true;
            if (!CanAfford(amount)) return false;

            Apply(Value - amount);

            ForEachModule(module => module.OnSpend(this, amount, reason));
            OnSpend?.Invoke(amount, reason);
            Wallet.RaiseSpend(this, amount, reason);

            RaiseChanged();

            return true;
        }

        /// <summary>Overwrites the balance. Raises OnChanged, but neither OnEarn nor OnSpend.</summary>
        public void Set(int value)
        {
            Apply(Math.Clamp(value, 0, Limit));

            RaiseChanged();
        }

        /// <summary>Overwrites the balance (loading, migrating a save). Raises only OnChangedWithoutNotify.</summary>
        public void SetWithoutNotify(int value)
        {
            Apply(Math.Clamp(value, 0, Limit));

            OnChangedWithoutNotify?.Invoke(_cached);
        }

        #region Save

        /// <summary>Writes the balance to disk, if it changed.</summary>
        public void Save()
        {
            if (_value.IsDirty)
                _value.Push();
        }

        public async Awaitable SaveAsync(CancellationToken cancellationToken = default)
        {
            if (_value.IsDirty)
                await _value.PushAsync(cancellationToken);
        }

        #endregion

        public string Format() => (Formatter ?? PlainFormatter.Default).Format(Value);

        public override string ToString() => $"{name}: {Value}";

        #region Runtime State

        private void EnsureLoaded()
        {
            if (_loaded) return;

            _loaded = true;
            _cached = _value.GetSaveData().GetKey(_value.SaveKey, StartingAmount);

            Wallet.Register(this);

            ForEachModule(module => module.Initialize(this));
        }

        // Only the in-memory side; disk is written by Save or the auto saver.
        // SaveVar.SetValue would write the whole file on every change.
        private void Apply(int value)
        {
            EnsureLoaded();

            _cached = value;
            _value.SetValueWithoutNotifying(value);
        }

        private void RaiseChanged()
        {
            var value = _cached;

            ForEachModule(module => module.OnChanged(this, value));
            OnChanged?.Invoke(value);
        }

        private void ForEachModule(Action<CurrencyModule> action)
        {
            foreach (var module in Modules)
            {
                if (module == null) continue;

                try
                {
                    action(module);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        /// <summary>
        /// Forgets the loaded balance, unsaved changes, listeners and modules' state.
        /// Wallet calls it when entering Play Mode, since the asset outlives Play Mode in the editor.
        /// </summary>
        internal void ResetRuntimeState()
        {
            if (_loaded)
                ForEachModule(module => module.Dispose());

            if (_value.IsDirty)
                _value.Pull();

            _loaded = false;
            _cached = 0;

            OnChanged = null;
            OnEarn = null;
            OnSpend = null;
            OnChangedWithoutNotify = null;
        }

        /// <summary>Replaces the save file and key. Used by the editor and tests.</summary>
        internal void SetSave(string relativePath, SaveKey saveKey) =>
            _value = new SaveVar<int>(relativePath, saveKey);

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (MaxAmount > 0 && StartingAmount > MaxAmount)
                StartingAmount = MaxAmount;
        }
#endif
    }
}
