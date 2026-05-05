using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Blitztext.Core;

namespace Blitztext.UI;

/// <summary>Eigennamen-Tab: selectable list with Add / Delete buttons.</summary>
public class NounsTab : UserControl
{
    private readonly AppConfig _config;

    private StackPanel?       _listPanel;
    private Button?           _deleteBtn;
    private readonly List<string>  _items    = [];
    private readonly HashSet<int>  _selected = [];
    private readonly List<Border>  _rows     = [];

    public NounsTab(AppConfig config)
    {
        _config = config;
        Content = BuildUi();
    }

    private UIElement BuildUi()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var outer = new StackPanel { Margin = new Thickness(8, 4, 8, 8) };
        scroll.Content = outer;

        var card = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 10, 14, 10) };
        card.SetResourceReference(Border.BackgroundProperty, "CardBackground");

        var inner = new StackPanel();
        card.Child = inner;

        var header = new TextBlock { Text = "Eigennamen" };
        header.SetResourceReference(TextBlock.StyleProperty, "SectionHeader");

        var hint = new TextBlock
        {
            Text = "Helfen Whisper, Markennamen, Personen und Fachbegriffe korrekt zu erkennen. " +
                   "Klicken zum Markieren, mehrere gleichzeitig möglich.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8),
        };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");

        // ── list area ──────────────────────────────────────────────────
        var listBorder = new Border { BorderThickness = new Thickness(1) };
        listBorder.SetResourceReference(Border.BorderBrushProperty, "InputBorder");
        listBorder.SetResourceReference(Border.BackgroundProperty,  "InputBackground");
        listBorder.CornerRadius = new CornerRadius(4);

        _listPanel = new StackPanel();
        listBorder.Child = _listPanel;

        // ── buttons ────────────────────────────────────────────────────
        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var addBtn = new Button { Content = "+ Hinzufügen" };
        addBtn.Click += OnAdd;
        btnRow.Children.Add(addBtn);

        _deleteBtn = new Button
        {
            Content = "✕ Löschen",
            Margin  = new Thickness(8, 0, 0, 0),
            IsEnabled = false,
        };
        _deleteBtn.SetResourceReference(StyleProperty, "SecondaryButton");
        _deleteBtn.Click += OnDelete;
        btnRow.Children.Add(_deleteBtn);

        inner.Children.Add(header);
        inner.Children.Add(hint);
        inner.Children.Add(listBorder);
        inner.Children.Add(btnRow);

        outer.Children.Add(card);
        return scroll;
    }

    // ── data ───────────────────────────────────────────────────────────

    private void RebuildList()
    {
        _listPanel!.Children.Clear();
        _rows.Clear();
        _selected.Clear();
        UpdateDeleteButton();

        for (int i = 0; i < _items.Count; i++)
            AppendRow(i, _items[i]);
    }

    private void AppendRow(int index, string text)
    {
        var lbl = new TextBlock
        {
            Text = text,
            Padding = new Thickness(10, 6, 10, 6),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        lbl.SetResourceReference(TextBlock.FontFamilyProperty, "Cascadia Code, Consolas");
        lbl.SetResourceReference(TextBlock.ForegroundProperty, "ForegroundBrush");

        var row = new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
        };
        row.SetResourceReference(Border.BorderBrushProperty, "SeparatorBrush");
        row.SetResourceReference(Border.BackgroundProperty,  "InputBackground");
        row.Child = lbl;

        var idx = index; // capture
        row.MouseLeftButtonUp += (_, _) => ToggleRow(idx, row, lbl);

        _rows.Add(row);
        _listPanel!.Children.Add(row);
    }

    private void ToggleRow(int index, Border row, TextBlock lbl)
    {
        if (_selected.Contains(index))
        {
            _selected.Remove(index);
            row.SetResourceReference(Border.BackgroundProperty, "InputBackground");
            lbl.SetResourceReference(TextBlock.ForegroundProperty, "ForegroundBrush");
        }
        else
        {
            _selected.Add(index);
            row.SetResourceReference(Border.BackgroundProperty, "AccentBrush");
            lbl.Foreground = GetReadableForeground(row.Background);
        }
        UpdateDeleteButton();
    }

    private static Brush GetReadableForeground(Brush? background)
    {
        if (background is SolidColorBrush solid)
        {
            var color = solid.Color;
            var brightness = ((color.R * 299) + (color.G * 587) + (color.B * 114)) / 1000;
            return new SolidColorBrush(brightness >= 140 ? Colors.Black : Colors.White);
        }

        return new SolidColorBrush(Colors.White);
    }

    private void UpdateDeleteButton()
    {
        int count = _selected.Count;
        _deleteBtn!.IsEnabled = count > 0;
        _deleteBtn.Content = count > 0 ? $"✕ Löschen ({count})" : "✕ Löschen";
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        var toKeep = _items
            .Select((n, i) => (n, i))
            .Where(x => !_selected.Contains(x.i))
            .Select(x => x.n)
            .ToList();
        _items.Clear();
        _items.AddRange(toKeep);
        RebuildList();
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        // Inline input row
        var entry = new TextBox
        {
            Margin = new Thickness(6, 4, 6, 4),
        };
        entry.SetResourceReference(StyleProperty, "Mono");

        var row = new Border
        {
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = entry,
        };
        row.SetResourceReference(Border.BorderBrushProperty, "SeparatorBrush");
        row.SetResourceReference(Border.BackgroundProperty,  "InputBackground");
        _listPanel!.Children.Add(row);

        entry.Focus();

        void Commit()
        {
            var text = entry.Text.Trim();
            _listPanel.Children.Remove(row);
            if (!string.IsNullOrEmpty(text))
            {
                _items.Add(text);
                RebuildList();
            }
        }

        entry.KeyDown += (_, ke) => { if (ke.Key == Key.Return) Commit(); };
        entry.LostFocus += (_, _) => Commit();
    }

    // ── public ─────────────────────────────────────────────────────────

    public void Load()
    {
        _items.Clear();
        _items.AddRange(_config.ProperNouns);
        RebuildList();
    }

    public void Save(AppConfig config)
    {
        config.ProperNouns = _items.Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
    }
}
