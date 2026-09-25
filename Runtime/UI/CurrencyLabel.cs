using fefek5.Toys.Runtime.Attributes;
using TMPro;
using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>Shows a currency's balance in a TMP_Text and keeps it up to date.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class CurrencyLabel : MonoBehaviour
    {
        [SerializeField] private Currency currency;

        [SerializeField, Tooltip("{0} is the formatted balance, e.g. \"{0}$\"")]
        private string format = "{0}";

        [SerializeReference, SelectType, Tooltip("Empty = the currency's formatter")]
        private ICurrencyFormatter overrideFormatter;

        private TMP_Text _text;

        public Currency Currency
        {
            get => currency;
            set
            {
                if (currency == value) return;

                Unsubscribe();
                currency = value;

                if (isActiveAndEnabled)
                    Subscribe();
            }
        }

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable() => Subscribe();

        private void OnDisable() => Unsubscribe();

        public void Refresh()
        {
            if (!currency)
            {
                _text.text = string.Empty;
                return;
            }

            var value = overrideFormatter != null ? overrideFormatter.Format(currency.Value) : currency.Format();

            _text.text = string.Format(format, value);
        }

        private void Subscribe()
        {
            if (currency)
            {
                currency.OnChanged += HandleChanged;
                currency.OnChangedWithoutNotify += HandleChanged;
            }

            Refresh();
        }

        private void Unsubscribe()
        {
            if (!currency) return;

            currency.OnChanged -= HandleChanged;
            currency.OnChangedWithoutNotify -= HandleChanged;
        }

        private void HandleChanged(long _) => Refresh();
    }
}
