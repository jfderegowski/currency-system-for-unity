using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace fefek5.Currency.Editor
{
    using Runtime;

    /// <summary>Every currency of the database with its live balance, in Play Mode.</summary>
    public class CurrencyDebugWindow : EditorWindow
    {
        private readonly List<Currency> _shown = new();
        private readonly List<(Label balance, Label dirty)> _rows = new();

        private HelpBox _playModeInfo;
        private VisualElement _content;
        private ScrollView _list;
        private IntegerField _step;

        [MenuItem("Window/Currencies")]
        private static void Open() => GetWindow<CurrencyDebugWindow>("Currencies");

        private void CreateGUI()
        {
            _playModeInfo = new HelpBox("Enter Play Mode to see the balances.", HelpBoxMessageType.Info);
            _content = new VisualElement { style = { flexGrow = 1 } };

            var toolbar = new Toolbar();
            _step = new IntegerField("Step") { value = 100, style = { minWidth = 200 } };
            toolbar.Add(_step);
            toolbar.Add(new ToolbarButton(Wallet.SaveAll) { text = "Save All" });

            _list = new ScrollView();

            _content.Add(toolbar);
            _content.Add(_list);

            rootVisualElement.Add(_playModeInfo);
            rootVisualElement.Add(_content);

            rootVisualElement.schedule.Execute(Refresh).Every(200);
            Refresh();
        }

        private void Refresh()
        {
            var playing = EditorApplication.isPlaying;

            _playModeInfo.style.display = playing ? DisplayStyle.None : DisplayStyle.Flex;
            _content.style.display = playing ? DisplayStyle.Flex : DisplayStyle.None;

            if (!playing) return;

            var currencies = Wallet.All.Where(currency => currency).ToList();

            if (!currencies.SequenceEqual(_shown))
                RebuildRows(currencies);

            for (var i = 0; i < _shown.Count; i++)
            {
                var currency = _shown[i];

                _rows[i].balance.text = $"{currency.Value} ({currency.Format()})";
                _rows[i].dirty.text = currency.IsDirty ? "unsaved" : string.Empty;
            }
        }

        private void RebuildRows(List<Currency> currencies)
        {
            _shown.Clear();
            _shown.AddRange(currencies);
            _rows.Clear();
            _list.Clear();

            foreach (var currency in currencies)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

                var field = new ObjectField { objectType = typeof(Currency), value = currency, style = { width = 180 } };
                field.SetEnabled(false);

                var balance = new Label { style = { flexGrow = 1, marginLeft = 6 } };
                var dirty = new Label { style = { width = 60 } };

                row.Add(field);
                row.Add(balance);
                row.Add(dirty);
                row.Add(new Button(() => currency.TrySpend(_step.value)) { text = "-" });
                row.Add(new Button(() => currency.Earn(_step.value, null)) { text = "+" });

                _list.Add(row);
                _rows.Add((balance, dirty));
            }
        }
    }
}
