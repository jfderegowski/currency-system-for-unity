using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>Where earned currency came from (passive income, a click, a reward...). Passed with every Earn.</summary>
    [CreateAssetMenu(fileName = "New Currency Source", menuName = "Currency/Currency Source")]
    public class CurrencySource : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; }

        [field: SerializeField] public Sprite Icon { get; private set; }
    }
}
