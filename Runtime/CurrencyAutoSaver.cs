using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>
    /// Writes unsaved balances to disk every few seconds, on pause and on quit.
    /// Created on its own after the first scene loads; settings come from CurrencyDatabase.
    /// </summary>
    [AddComponentMenu("")]
    public class CurrencyAutoSaver : MonoBehaviour
    {
        private CurrencyDatabase _database;
        private float _nextSaveTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var database = CurrencyDatabase.Instance;

            if (!database || !database.AutoSave) return;

            var gameObject = new GameObject("[CurrencyAutoSaver]");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<CurrencyAutoSaver>()._database = database;
        }

        private void Start() => _nextSaveTime = Time.unscaledTime + _database.AutoSaveInterval;

        private void Update()
        {
            if (Time.unscaledTime < _nextSaveTime) return;

            _nextSaveTime = Time.unscaledTime + _database.AutoSaveInterval;

            Wallet.SaveAll();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _database.SaveOnPause)
                Wallet.SaveAll();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _database.SaveOnPause)
                Wallet.SaveAll();
        }

        // Synchronous, an async save would not finish before the process ends
        private void OnApplicationQuit()
        {
            if (_database.SaveOnQuit)
                Wallet.SaveAll();
        }
    }
}
