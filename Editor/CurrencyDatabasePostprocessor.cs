using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace fefek5.Currency.Editor
{
    using Runtime;

    /// <summary>Keeps CurrencyDatabase listing every Currency asset of the project.</summary>
    internal class CurrencyDatabasePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var newCurrencies = importedAssets
                .Where(IsAssetOf<Currency>)
                .Select(AssetDatabase.LoadAssetAtPath<Currency>)
                .Where(currency => currency)
                .ToList();

            // A new database starts with every existing currency
            var databaseImported = importedAssets.Any(IsAssetOf<CurrencyDatabase>);

            if (newCurrencies.Count == 0 && deletedAssets.Length == 0 && !databaseImported) return;

            if (databaseImported)
                newCurrencies.AddRange(CurrencyEditor.FindCurrencies());

            foreach (var database in FindDatabases())
            {
                // Nulls are only cleaned up after a delete: while the Library is rebuilt,
                // currencies that are not imported yet look missing too
                var changed = deletedAssets.Length > 0 && database.Currencies.RemoveAll(currency => !currency) > 0;

                foreach (var currency in newCurrencies)
                {
                    if (database.Currencies.Contains(currency)) continue;

                    database.Currencies.Add(currency);
                    changed = true;
                }

                if (changed)
                    Save(database);
            }
        }

        [MenuItem("Tools/Currency/Rebuild Database")]
        private static void RebuildDatabase()
        {
            var database = CurrencyDatabase.Instance;

            Undo.RecordObject(database, "Rebuild Currency Database");

            database.Currencies.Clear();
            database.Currencies.AddRange(CurrencyEditor.FindCurrencies());

            Save(database);

            EditorGUIUtility.PingObject(database);
        }

        private static void Save(CurrencyDatabase database)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssetIfDirty(database);
        }

        private static IEnumerable<CurrencyDatabase> FindDatabases() => AssetDatabase
            .FindAssets("t:" + nameof(CurrencyDatabase))
            .Select(guid => AssetDatabase.LoadAssetAtPath<CurrencyDatabase>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(database => database);

        private static bool IsAssetOf<T>(string path)
        {
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);

            return type != null && typeof(T).IsAssignableFrom(type);
        }
    }
}
