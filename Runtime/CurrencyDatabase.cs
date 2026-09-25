using System.Collections.Generic;
using Runtime;
using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>
    /// Every currency of the game plus the auto save settings. Lives in Resources, so a build ships all
    /// listed currencies even when no scene references them. New Currency assets are added by the editor.
    /// A currency outside the database still works, it's only missing from Wallet.All.
    /// </summary>
    public class CurrencyDatabase : ResourcesScriptableObject<CurrencyDatabase>
    {
        [field: SerializeField] public List<Currency> Currencies { get; private set; } = new();

        [field: Header("Auto Save")]
        [field: SerializeField] public bool AutoSave { get; private set; } = true;

        [field: SerializeField, Min(0.1f)] public float AutoSaveInterval { get; private set; } = 5f;

        [field: SerializeField, Tooltip("Save when the app loses focus or is paused (mobile)")]
        public bool SaveOnPause { get; private set; } = true;

        [field: SerializeField] public bool SaveOnQuit { get; private set; } = true;
    }
}
