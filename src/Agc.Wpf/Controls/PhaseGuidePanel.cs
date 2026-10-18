using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Agc.Core;
using Agc.Mission;

namespace Agc.Wpf.Controls;

/// <summary>
/// 단계 가이드 패널: 현재 미션 단계에서 무슨 일이 일어나는지, AGC가 무엇을 하는지, 그리고 승무원이 AGC를
/// 어떻게 조작하는지를 단계별로 보여 준다. 각 조작 단계는 "조작 / 목적 / 영향"을 함께 설명하고,
/// 이 시뮬레이터가 지원하는 DSKY 키 입력은 [실행] 버튼으로 바로 눌러 볼 수 있다.
/// - 내용은 <see cref="PhaseGuide"/> 데이터(근거와 확인 수준 표시 포함)에서 오며, 이 컨트롤은 그리기만 한다.
/// - Owner: MainWindow. Lifetime: 창과 동일. UI 스레드 전용.
/// - [실행] 버튼은 <see cref="RunKeysRequested"/>로 키 문자열을 알릴 뿐 DSKY를 직접 만지지 않는다.
/// </summary>
public sealed class PhaseGuidePanel : UserControl
{
    private static readonly Brush PanelBackground = Solid(0x14, 0x17, 0x1C);
    private static readonly Brush CardBackground = Solid(0x1C, 0x20, 0x27);
    private static readonly Brush CardBorder = Solid(0x2F, 0x35, 0x3F);
    private static readonly Brush TextMain = Solid(0xE6, 0xE8, 0xEB);
    private static readonly Brush TextMuted = Solid(0xA9, 0xB1, 0xBC);
    private static readonly Brush KeysBackground = Solid(0x0B, 0x0E, 0x12);
    private static readonly Brush KeysText = Solid(0x6E, 0xF0, 0xB0);
    private static readonly Brush PurposeColor = Solid(0xFF, 0xC8, 0x57);
    private static readonly Brush EffectColor = Solid(0x6C, 0xB6, 0xFF);
    private static readonly Brush ActionColor = Solid(0xC9, 0xD1, 0xDB);

    private const double BodyFontSize = 13;
    private const double SmallFontSize = 11;

    private readonly StackPanel _content = new() { Margin = new Thickness(12, 8, 12, 12) };
    private readonly ScrollViewer _scroll;
    private readonly List<Button> _runButtons = [];
    private int _stepCount;

    public PhaseGuidePanel()
    {
        _scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _content,
        };
        Content = new Border
        {
            Background = PanelBackground,
            BorderBrush = CardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = _scroll,
        };
    }

    /// <summary>[실행] 버튼을 눌렀을 때. 인자는 실행할 DSKY 키 문자열(예: "V16N63E").</summary>
    public event Action<string>? RunKeysRequested;

    /// <summary>지금 보이는 가이드의 조작 단계 수.</summary>
    internal int StepCount => _stepCount;

    /// <summary>화면에 있는 [실행] 버튼들(테스트용).</summary>
    internal IReadOnlyList<Button> RunButtons => _runButtons;

    /// <summary>지금 보이는 모든 글자(테스트용).</summary>
    internal string AllText => string.Concat(Descendants<TextBlock>(_content).Select(t => TextOf(t) + " "));

    /// <summary>TextBlock의 글자. Run 인라인으로 만든 것(조작/목적/영향 줄)은 Text가 비어 있으므로 인라인을 이어 붙인다.</summary>
    private static string TextOf(TextBlock block)
    {
        return block.Inlines.Count > 0 && string.IsNullOrEmpty(block.Text)
            ? string.Concat(block.Inlines.OfType<System.Windows.Documents.Run>().Select(r => r.Text))
            : block.Text;
    }

    /// <summary>
    /// 가이드를 보여 준다. guide가 null이면 "가이드 없음" 안내를 보여 준다.
    /// </summary>
    public void Show(PhaseGuide? guide, string phaseName)
    {
        _content.Children.Clear();
        _runButtons.Clear();
        _stepCount = 0;

        _content.Children.Add(Header(phaseName));
        if (guide is null)
        {
            _content.Children.Add(Paragraph("이 단계의 가이드가 아직 없습니다.", TextMuted));
            return;
        }

        _content.Children.Add(SectionTitle("무슨 일이 일어나나"));
        _content.Children.Add(Paragraph(guide.Summary, TextMain));

        _content.Children.Add(SectionTitle("AGC는 무엇을 하나"));
        foreach (var role in guide.AgcRole)
        {
            _content.Children.Add(Bullet(role));
        }

        _content.Children.Add(SectionTitle("조작 순서 — 무엇을, 왜, 어떤 영향이"));
        for (var i = 0; i < guide.Steps.Count; i++)
        {
            _content.Children.Add(StepCard(i + 1, guide.Steps[i]));
            _stepCount++;
        }

        _content.Children.Add(new TextBlock
        {
            Text = $"자세한 설명: {guide.Reference}   ·   확인 수준 표시는 근거 자료의 확인 정도입니다(확인 / 부분 / 일반 절차 / 미확인).",
            FontSize = SmallFontSize,
            Foreground = TextMuted,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0),
        });
        _scroll.ScrollToTop();
    }

    // ---- 구성 요소 ----

    private static UIElement Header(string phaseName)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
        panel.Children.Add(new TextBlock { Text = "단계 가이드  ·  ", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = TextMuted });
        panel.Children.Add(new TextBlock { Text = phaseName, FontSize = 15, FontWeight = FontWeights.Bold, Foreground = TextMain });
        return panel;
    }

    private static UIElement SectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = Solid(0x7F, 0xD0, 0xFF),
            Margin = new Thickness(0, 10, 0, 4),
        };
    }

    private static UIElement Paragraph(string text, Brush brush)
    {
        return new TextBlock { Text = text, FontSize = BodyFontSize, Foreground = brush, TextWrapping = TextWrapping.Wrap, LineHeight = 19 };
    }

    private static UIElement Bullet(string text)
    {
        var grid = new Grid { Margin = new Thickness(0, 1, 0, 1) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(new TextBlock { Text = "•", Foreground = TextMuted, FontSize = BodyFontSize });
        var body = Paragraph(text, TextMain);
        Grid.SetColumn(body, 1);
        grid.Children.Add(body);
        return grid;
    }

    private UIElement StepCard(int number, GuideStep step)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var badge = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            Background = KindColor(step.Kind),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = number.ToString(),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        grid.Children.Add(badge);

        var body = new StackPanel();
        Grid.SetColumn(body, 1);
        grid.Children.Add(body);

        // 제목 줄: 제목 + 종류/확인수준 칩
        var titleRow = new DockPanel { LastChildFill = true };
        var chips = new StackPanel { Orientation = Orientation.Horizontal };
        DockPanel.SetDock(chips, Dock.Right);
        chips.Children.Add(Chip(KindLabel(step.Kind), KindColor(step.Kind)));
        chips.Children.Add(Chip(ConfidenceLabel(step.Confidence), ConfidenceColor(step.Confidence)));
        titleRow.Children.Add(chips);
        titleRow.Children.Add(new TextBlock { Text = step.Title, FontSize = BodyFontSize, FontWeight = FontWeights.SemiBold, Foreground = TextMain, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(titleRow);

        if (step.Kind == GuideStepKind.Dsky && step.Keys is not null)
        {
            body.Children.Add(KeysRow(step));
        }

        body.Children.Add(Labeled("조작", step.Action, ActionColor));
        body.Children.Add(Labeled("목적", step.Purpose, PurposeColor));
        body.Children.Add(Labeled("영향", step.Effect, EffectColor));

        return new Border
        {
            Background = CardBackground,
            BorderBrush = CardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 8),
            Child = grid,
        };
    }

    private UIElement KeysRow(GuideStep step)
    {
        var row = new DockPanel { Margin = new Thickness(0, 5, 0, 3), LastChildFill = true };
        if (step.Runnable)
        {
            var keys = step.Keys!;
            var run = new Button
            {
                Content = "▶ 실행",
                Padding = new Thickness(10, 2, 10, 2),
                Margin = new Thickness(8, 0, 0, 0),
                Focusable = false,
                ToolTip = "이 시뮬레이터의 DSKY에서 위 키 순서를 그대로 누릅니다.",
            };
            run.Click += (_, _) => RunKeysRequested?.Invoke(keys);
            DockPanel.SetDock(run, Dock.Right);
            row.Children.Add(run);
            _runButtons.Add(run);
        }
        else
        {
            var note = new TextBlock
            {
                Text = "시뮬레이터 미지원",
                FontSize = SmallFontSize,
                Foreground = TextMuted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
            };
            DockPanel.SetDock(note, Dock.Right);
            row.Children.Add(note);
        }

        row.Children.Add(new Border
        {
            Background = KeysBackground,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3, 8, 3),
            Child = new TextBlock
            {
                Text = DskyKeyNotation.Describe(step.Keys!),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = KeysText,
                TextWrapping = TextWrapping.Wrap,
            },
        });
        return row;
    }

    private static UIElement Labeled(string label, string text, Brush labelBrush)
    {
        var block = new TextBlock { FontSize = 12.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0), LineHeight = 18 };
        block.Inlines.Add(new System.Windows.Documents.Run(label + "  ") { FontWeight = FontWeights.Bold, Foreground = labelBrush });
        block.Inlines.Add(new System.Windows.Documents.Run(text) { Foreground = TextMain });
        return block;
    }

    private static UIElement Chip(string text, Brush background)
    {
        return new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(7, 1, 7, 1),
            Margin = new Thickness(5, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock { Text = text, FontSize = 10.5, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Black },
        };
    }

    // ---- 라벨/색 ----

    private static string KindLabel(GuideStepKind kind)
    {
        return kind switch
        {
            GuideStepKind.Dsky => "DSKY",
            GuideStepKind.Crew => "승무원 조작",
            GuideStepKind.Ground => "지상 교신",
            _ => "관찰",
        };
    }

    private static Brush KindColor(GuideStepKind kind)
    {
        return kind switch
        {
            GuideStepKind.Dsky => Solid(0x3D, 0xDC, 0x97),
            GuideStepKind.Crew => Solid(0x6C, 0xB6, 0xFF),
            GuideStepKind.Ground => Solid(0xFF, 0xC8, 0x57),
            _ => Solid(0xA9, 0xB1, 0xBC),
        };
    }

    private static string ConfidenceLabel(GuideConfidence confidence)
    {
        return confidence switch
        {
            GuideConfidence.Confirmed => "확인",
            GuideConfidence.Partial => "부분",
            GuideConfidence.General => "일반 절차",
            _ => "미확인",
        };
    }

    private static Brush ConfidenceColor(GuideConfidence confidence)
    {
        return confidence switch
        {
            GuideConfidence.Confirmed => Solid(0x7B, 0xE0, 0x9A),
            GuideConfidence.Partial => Solid(0xF2, 0xE0, 0x6B),
            GuideConfidence.General => Solid(0xC9, 0xD1, 0xDB),
            _ => Solid(0xFF, 0x8A, 0x80),
        };
    }

    private static Brush Solid(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in Descendants<T>(child))
            {
                yield return nested;
            }
        }

    }
}
