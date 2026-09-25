using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>What currency was spent on (an upgrade, a skin...). Optional in TrySpend.</summary>
    [CreateAssetMenu(fileName = "New Spend Reason", menuName = "Currency/Spend Reason")]
    public class SpendReason : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; }

        [field: SerializeField] public Sprite Icon { get; private set; }
    }
}
