using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace fefek5.Currency.Samples
{
    using Runtime;

    /// <summary>Buys something for a Price. Only clickable while the player can afford it.</summary>
    [RequireComponent(typeof(Button))]
    public class ShopButton : MonoBehaviour
    {
        [SerializeField] private Price price = new();
        [SerializeField] private SpendReason reason;
        [SerializeField] private UnityEvent onBought;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Buy);
        }

        private void OnEnable()
        {
            foreach (var (currency, _) in price.Amounts)
            {
                if (!currency) continue;

                currency.OnChanged += HandleChanged;
                currency.OnChangedWithoutNotify += HandleChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            foreach (var (currency, _) in price.Amounts)
            {
                if (!currency) continue;

                currency.OnChanged -= HandleChanged;
                currency.OnChangedWithoutNotify -= HandleChanged;
            }
        }

        private void Buy()
        {
            if (price.TrySpend(reason))
                onBought.Invoke();
        }

        private void Refresh() => _button.interactable = price.CanAfford();

        private void HandleChanged(long _) => Refresh();
    }
}
