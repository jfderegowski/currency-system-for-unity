using System;
using System.Collections.Generic;
using System.IO;
using fefek5.SaveDataVariable.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace fefek5.Currency.Tests
{
    using Runtime;

    /// <summary>Currencies saved to their own temporary file, destroyed and deleted on Dispose.</summary>
    internal class TestCurrencies : IDisposable
    {
        public const string Key = "Balance";

        private readonly List<Object> _objects = new();

        public Currency Create(long startingAmount = 0, long maxAmount = 0)
        {
            var currency = ScriptableObject.CreateInstance<Currency>();
            currency.name = "TestCurrency";
            currency.SetSave($"CurrencyTests_{Guid.NewGuid():N}.json", new SaveKey(Key));

            var serializedObject = new SerializedObject(currency);
            serializedObject.FindProperty("<StartingAmount>k__BackingField").longValue = startingAmount;
            serializedObject.FindProperty("<MaxAmount>k__BackingField").longValue = maxAmount;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            _objects.Add(currency);

            return currency;
        }

        public CurrencySource CreateSource()
        {
            var source = ScriptableObject.CreateInstance<CurrencySource>();
            _objects.Add(source);

            return source;
        }

        public void Dispose()
        {
            foreach (var obj in _objects)
            {
                if (obj is Currency currency)
                {
                    currency.ResetRuntimeState();

                    if (File.Exists(currency.SavePath))
                        File.Delete(currency.SavePath);
                }

                Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }
    }
}
