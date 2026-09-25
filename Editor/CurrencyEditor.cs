using System.Linq;
using fefek5.SaveDataVariable.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace fefek5.Currency.Editor
{
    using Runtime;

    [CustomEditor(typeof(Currency), true)]
    public class CurrencyEditor : UnityEditor.Editor
    {
        private const string DefaultKey = "CURRENCY_NAME";

        private Currency Currency => (Currency)target;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var keyInfo = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            var useNameButton = new Button(UseAssetNameAsKey) { text = "Use asset name as save key" };

            root.Add(keyInfo);
            root.Add(useNameButton);

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            root.Add(CreatePlayModeSection());

            UpdateKeyInfo(keyInfo, useNameButton);
            root.TrackSerializedObjectValue(serializedObject, _ => UpdateKeyInfo(keyInfo, useNameButton));

            return root;
        }

        #region Save Key

        private void UpdateKeyInfo(HelpBox keyInfo, VisualElement useNameButton)
        {
            var currency = Currency;
            var saveKey = currency.SaveKey;

            var sharing = FindCurrencies()
                .Where(other => other != currency
                                && other.SaveRelativePath == currency.SaveRelativePath
                                && other.SaveKey == saveKey)
                .Select(other => other.name)
                .ToList();

            var message = saveKey == DefaultKey
                ? $"The save key is still the default {DefaultKey}."
                : string.Empty;

            if (sharing.Count > 0)
                message += (message.Length > 0 ? "\n" : string.Empty) +
                           $"Same save key and file as {string.Join(", ", sharing)}: they share one balance.";

            keyInfo.text = message;
            keyInfo.style.display = message.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            useNameButton.style.display = saveKey == currency.name ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void UseAssetNameAsKey()
        {
            var currency = Currency;

            Undo.RecordObject(currency, "Use Asset Name As Save Key");
            currency.SetSave(currency.SaveRelativePath, new SaveKey(currency.name));
            EditorUtility.SetDirty(currency);

            serializedObject.Update();
        }

        internal static Currency[] FindCurrencies() => AssetDatabase.FindAssets("t:" + nameof(Currency))
            .Select(guid => AssetDatabase.LoadAssetAtPath<Currency>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(currency => currency)
            .ToArray();

        #endregion

        #region Play Mode

        private VisualElement CreatePlayModeSection()
        {
            var section = new VisualElement { style = { marginTop = 10 } };

            var header = new Label("Play Mode") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            var balance = new Label();
            var amount = new LongField("Amount") { value = 100 };
            var source = new ObjectField("Source") { objectType = typeof(CurrencySource) };

            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };

            buttons.Add(new Button(() => Currency.Earn(amount.value, (CurrencySource)source.value)) { text = "Earn" });
            buttons.Add(new Button(() => Spend(amount.value)) { text = "Spend" });
            buttons.Add(new Button(() => Currency.Set(amount.value)) { text = "Set" });
            buttons.Add(new Button(() => Currency.Set(Currency.StartingAmount)) { text = "Reset to starting" });
            buttons.Add(new Button(() => Currency.Save()) { text = "Save" });

            section.Add(header);
            section.Add(balance);
            section.Add(amount);
            section.Add(source);
            section.Add(buttons);

            section.schedule.Execute(() =>
            {
                var playing = EditorApplication.isPlaying;

                section.style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;

                if (playing)
                    balance.text = $"Balance: {Currency.Value} ({Currency.Format()})" +
                                   (Currency.IsDirty ? " - unsaved" : string.Empty);
            }).Every(100);

            return section;
        }

        private void Spend(long amount)
        {
            if (!Currency.TrySpend(amount))
                Debug.LogWarning($"{Currency.name}: cannot spend {amount}, the balance is {Currency.Value}.", Currency);
        }

        #endregion
    }
}
