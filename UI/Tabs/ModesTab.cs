using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Blitztext.Core;

namespace Blitztext.UI;

/// <summary>Modi-Tab: Prompt TextBoxen für Plus/Rage/Emoji + Emoji-Density-Slider.</summary>
public class ModesTab : UserControl
{
    private readonly AppConfig _config;

    private TextBox? _plusBox, _rageBox, _emojiBox;
    private Slider?  _densitySlider;
    private TextBlock? _densityLabel;

    private static readonly (string Key, string Label, string Hint)[] ModeInfos =
    [
        ("plus",  "📝  Plus-Modus",  "Formuliert gesprochenen Text schriftlicher um."),
        ("rage",  "😤  Rage-Modus",  "Wandelt wütenden Text in eine höfliche Nachricht."),
        ("emoji", "😊  Emoji-Modus", "Fügt passende Emojis in den Text ein."),
    ];

    public ModesTab(AppConfig config)
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
        var stack  = new StackPanel { Margin = new Thickness(8, 4, 8, 8) };
        scroll.Content = stack;

        foreach (var (key, label, hint) in ModeInfos)
        {
            var box = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping  = TextWrapping.Wrap,
                MinHeight     = 60,
                MaxHeight     = 100,
                IsReadOnly    = true,
                VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalContentAlignment      = VerticalAlignment.Top,
            };
            box.SetResourceReference(StyleProperty, "Input");

            switch (key)
            {
                case "plus":  _plusBox  = box; break;
                case "rage":  _rageBox  = box; break;
                case "emoji": _emojiBox = box; break;
            }

            stack.Children.Add(MakeCard(label, hint, box, key));
        }

        // Emoji Density
        stack.Children.Add(MakeDensityCard());

        return scroll;
    }

    private UIElement MakeDensityCard()
    {
        var inner = new StackPanel();
        inner.Children.Add(MakeHeader("Emoji-Dichte"));
        inner.Children.Add(MakeHint("Wie viele Emojis sollen eingefügt werden?"));

        // Slider row: [Wenige] [====slider====] [Viele] [5]
        var sliderRow = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };
        var lblLow  = MakeHint("Wenige"); lblLow.Margin  = new Thickness(0, 0, 8, 0); lblLow.VerticalAlignment  = VerticalAlignment.Center;
        var lblHigh = MakeHint("Viele");  lblHigh.Margin = new Thickness(8, 0, 4, 0); lblHigh.VerticalAlignment = VerticalAlignment.Center;
        _densityLabel = new TextBlock
        {
            FontWeight    = FontWeights.SemiBold,
            MinWidth      = 20,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _densityLabel.SetResourceReference(TextBlock.ForegroundProperty, "AccentBrush");

        _densitySlider = new Slider
        {
            Minimum             = 1,
            Maximum             = 10,
            TickFrequency       = 1,
            IsSnapToTickEnabled = true,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        _densitySlider.ValueChanged += (_, e) =>
            _densityLabel.Text = ((int)e.NewValue).ToString();

        DockPanel.SetDock(lblLow,         System.Windows.Controls.Dock.Left);
        DockPanel.SetDock(_densityLabel,  System.Windows.Controls.Dock.Right);
        DockPanel.SetDock(lblHigh,        System.Windows.Controls.Dock.Right);
        sliderRow.Children.Add(lblLow);
        sliderRow.Children.Add(_densityLabel);
        sliderRow.Children.Add(lblHigh);
        sliderRow.Children.Add(_densitySlider);   // LastChildFill → füllt Mitte

        inner.Children.Add(sliderRow);
        return WrapCard(inner);
    }

    private UIElement MakeCard(string header, string hint, TextBox box, string key)
    {
        var inner = new StackPanel();

        // Header row: [Header] ... [Bearbeiten/Speichern] [↺ Zurücksetzen]
        var headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 0) };

        var resetBtn = new Button
        {
            Content = "↺ Zurücksetzen",
            Cursor  = System.Windows.Input.Cursors.Hand,
            Margin  = new Thickness(6, 0, 0, 0),
        };
        resetBtn.SetResourceReference(StyleProperty, "SecondaryButton");
        resetBtn.Click += (_, _) => box.Text = _config.GetDefaultPrompt(key);

        var editBtn = new Button
        {
            Content = "Bearbeiten",
            Cursor  = System.Windows.Input.Cursors.Hand,
        };
        editBtn.SetResourceReference(StyleProperty, "SecondaryButton");
        editBtn.Click += (_, _) =>
        {
            if (box.IsReadOnly)
            {
                box.IsReadOnly  = false;
                editBtn.Content = "Speichern";
                box.Focus();
                box.CaretIndex  = box.Text.Length;
            }
            else
            {
                box.IsReadOnly  = true;
                editBtn.Content = "Bearbeiten";
            }
        };

        DockPanel.SetDock(resetBtn, System.Windows.Controls.Dock.Right);
        DockPanel.SetDock(editBtn,  System.Windows.Controls.Dock.Right);
        headerRow.Children.Add(resetBtn);
        headerRow.Children.Add(editBtn);
        headerRow.Children.Add(MakeHeader(header));

        inner.Children.Add(headerRow);
        inner.Children.Add(MakeHint(hint));
        inner.Children.Add(box);
        return WrapCard(inner);
    }

    private static Border WrapCard(UIElement content)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin  = new Thickness(0, 0, 0, 8),
            Child   = content,
        };
        border.SetResourceReference(Border.BackgroundProperty, "CardBackground");
        return border;
    }

    private static TextBlock MakeHeader(string text)
    {
        var tb = new TextBlock { Text = text };
        tb.SetResourceReference(TextBlock.StyleProperty, "SectionHeader");
        return tb;
    }

    private static TextBlock MakeHint(string text)
    {
        var tb = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        tb.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
        tb.FontSize = 11;
        tb.Margin = new Thickness(0, 0, 0, 6);
        return tb;
    }

    public void Load()
    {
        _plusBox!.Text  = _config.GetPrompt("plus");
        _rageBox!.Text  = _config.GetPrompt("rage");
        _emojiBox!.Text = _config.GetPrompt("emoji");
        _densitySlider!.Value = _config.EmojiDensity;
        _densityLabel!.Text   = _config.EmojiDensity.ToString();
    }

    public void Save(AppConfig config)
    {
        config.SetPrompt("plus",  _plusBox!.Text.Trim());
        config.SetPrompt("rage",  _rageBox!.Text.Trim());
        config.SetPrompt("emoji", _emojiBox!.Text.Trim());
        config.EmojiDensity = (int)_densitySlider!.Value;
    }
}
