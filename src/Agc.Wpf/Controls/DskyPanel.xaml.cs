using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Agc.Core;

namespace Agc.Wpf.Controls;

/// <summary>
/// DSKY 화면(램프, COMP ACTY/PROG/VERB/NOUN/R1~R3)과 키패드를 그리는 컨트롤.
/// 배치는 실제 Block II DSKY를 따른다:
/// - 램프 2열 x 7행: (UPLINK ACTY, TEMP) (NO ATT, GIMBAL LOCK) (STBY, PROG) (KEY REL, RESTART)
///   (OPR ERR, TRACKER) ( - , ALT) ( - , VEL)
/// - 숫자 표시 5줄: COMP ACTY + PROG / VERB + NOUN / R1 / R2 / R3
/// - 키패드 7열 x 3행, 키 19개. VERB/NOUN(맨 왼쪽)과 ENTR/RSET(맨 오른쪽)은 반 칸 아래에 놓여 3행의 가운데에 걸친다.
/// 로직은 갖지 않는다: <see cref="Display"/>를 받아 그리고, 키 클릭을 <see cref="KeyPressed"/>로 알릴 뿐이다.
/// Owner: MainWindow. Lifetime: 창과 동일. UI 스레드 전용.
/// </summary>
public partial class DskyPanel : UserControl
{
    public static readonly DependencyProperty DisplayProperty = DependencyProperty.Register(
        nameof(Display), typeof(DskyDisplay), typeof(DskyPanel),
        new PropertyMetadata(null, (d, e) => ((DskyPanel)d).ApplyDisplay((DskyDisplay?)e.NewValue)));

    /// <summary>램프 점등 색상 계열.</summary>
    private enum LampColor
    {
        White,
        Amber,
        Green,
    }

    private static Brush FrozenBrush(Color color)
    {
        // 정적 브러시는 여러 스레드/컨트롤이 공유하므로 반드시 동결한다.
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static readonly Brush LampOffBackground = FrozenBrush(Color.FromRgb(0x20, 0x22, 0x26));
    private static readonly Brush LampOffForeground = FrozenBrush(Color.FromRgb(0x6F, 0x74, 0x7B));
    private static readonly Brush LampWhite = FrozenBrush(Color.FromRgb(0xF2, 0xF2, 0xF2));
    private static readonly Brush LampAmber = FrozenBrush(Color.FromRgb(0xFF, 0xB0, 0x00));
    private static readonly Brush LampGreen = FrozenBrush(Color.FromRgb(0x4B, 0xE0, 0x8E));
    private static readonly Brush LampOnForeground = Brushes.Black;

    /// <summary>램프 패널 배치(2열, 행 우선 순서). null은 빈 칸. 오른쪽 열은 호박색 경고등이다.</summary>
    private static readonly (string Label, DskyLamps Flag, LampColor Color)?[] LampLayout =
    [
        ("UPLINK ACTY", DskyLamps.UplinkActy, LampColor.White), ("TEMP", DskyLamps.Temp, LampColor.Amber),
        ("NO ATT", DskyLamps.NoAtt, LampColor.White), ("GIMBAL LOCK", DskyLamps.GimbalLock, LampColor.Amber),
        ("STBY", DskyLamps.Stby, LampColor.White), ("PROG", DskyLamps.Prog, LampColor.Amber),
        ("KEY REL", DskyLamps.KeyRel, LampColor.White), ("RESTART", DskyLamps.Restart, LampColor.Amber),
        ("OPR ERR", DskyLamps.OprErr, LampColor.White), ("TRACKER", DskyLamps.Tracker, LampColor.Amber),
        null, ("ALT", DskyLamps.Alt, LampColor.Amber),
        null, ("VEL", DskyLamps.Vel, LampColor.Amber),
    ];

    /// <summary>
    /// 키패드 배치: (시작 행 단위, 열, 행 폭, 라벨, 키). 한 행 = 2 단위이므로
    /// 일반 키는 시작 행이 0/2/4, VERB(1)/NOUN(3)/ENTR(1)/RSET(3)은 반 칸(1 단위) 아래에서 시작한다.
    /// </summary>
    private static readonly (int Row, int Col, int RowSpan, string Label, DskyKey Key)[] KeyLayout =
    [
        (1, 0, 2, "VERB", DskyKey.Verb),
        (3, 0, 2, "NOUN", DskyKey.Noun),
        (0, 1, 2, "+", DskyKey.Plus),
        (2, 1, 2, "−", DskyKey.Minus),
        (4, 1, 2, "0", DskyKey.Digit0),
        (0, 2, 2, "7", DskyKey.Digit7), (0, 3, 2, "8", DskyKey.Digit8), (0, 4, 2, "9", DskyKey.Digit9),
        (2, 2, 2, "4", DskyKey.Digit4), (2, 3, 2, "5", DskyKey.Digit5), (2, 4, 2, "6", DskyKey.Digit6),
        (4, 2, 2, "1", DskyKey.Digit1), (4, 3, 2, "2", DskyKey.Digit2), (4, 4, 2, "3", DskyKey.Digit3),
        (0, 5, 2, "CLR", DskyKey.Clear),
        (2, 5, 2, "PRO", DskyKey.Proceed),
        (4, 5, 2, "KEY REL", DskyKey.KeyRelease),
        (1, 6, 2, "ENTR", DskyKey.Enter),
        (3, 6, 2, "RSET", DskyKey.Reset),
    ];

    private const int KeypadRowUnits = 6;
    private const int KeypadColumns = 7;
    private const double KeyLongLabelFontSize = 14;
    private const double KeyShortLabelFontSize = 24;
    private const int LongLabelLength = 2;
    private const double LampFontSize = 11;

    private readonly Dictionary<DskyLamps, (Border Box, TextBlock Text, LampColor Color)> _lamps = [];

    public DskyPanel()
    {
        InitializeComponent();
        BuildLamps();
        BuildKeypad();
    }

    /// <summary>키패드 버튼이 눌렸을 때 발생한다.</summary>
    public event Action<DskyKey>? KeyPressed;

    public DskyDisplay? Display
    {
        get => (DskyDisplay?)GetValue(DisplayProperty);
        set => SetValue(DisplayProperty, value);
    }

    private void BuildLamps()
    {
        foreach (var item in LampLayout)
        {
            if (item is not { } lamp)
            {
                LampGrid.Children.Add(new Border());
                continue;
            }

            LampGrid.Children.Add(CreateLamp(lamp.Label, lamp.Flag, lamp.Color));
        }

        // COMP ACTY는 램프 패널이 아니라 숫자 표시 패널 왼쪽 위에 있는 녹색 표시등이다.
        CompActyHost.Child = CreateLamp("COMP ACTY", DskyLamps.CompActy, LampColor.Green);
    }

    private Border CreateLamp(string label, DskyLamps flag, LampColor color)
    {
        var text = new TextBlock
        {
            Text = label,
            FontSize = LampFontSize,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = LampOffForeground,
        };
        var box = new Border
        {
            Margin = new Thickness(2),
            CornerRadius = new CornerRadius(2),
            Background = LampOffBackground,
            Child = text,
        };
        _lamps[flag] = (box, text, color);
        return box;
    }

    private void BuildKeypad()
    {
        for (var r = 0; r < KeypadRowUnits; r++)
        {
            KeypadGrid.RowDefinitions.Add(new RowDefinition());
        }

        for (var c = 0; c < KeypadColumns; c++)
        {
            KeypadGrid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        foreach (var (row, col, rowSpan, label, key) in KeyLayout)
        {
            var button = new Button
            {
                Content = label,
                Margin = new Thickness(4),
                Style = (Style)FindResource("KeyButtonStyle"),
                FontSize = label.Length > LongLabelLength ? KeyLongLabelFontSize : KeyShortLabelFontSize,
                // 키보드 입력은 창이 처리하므로 버튼이 포커스를 가져가지 않게 한다.
                Focusable = false,
            };
            var captured = key;
            button.Click += (_, _) => KeyPressed?.Invoke(captured);
            Grid.SetRow(button, row);
            Grid.SetColumn(button, col);
            Grid.SetRowSpan(button, rowSpan);
            KeypadGrid.Children.Add(button);
        }
    }

    private void ApplyDisplay(DskyDisplay? display)
    {
        if (display is null)
        {
            return;
        }

        ProgDisplay.Text = display.Prog;
        VerbDisplay.Text = display.Verb;
        NounDisplay.Text = display.Noun;
        R1Display.Text = display.R1;
        R2Display.Text = display.R2;
        R3Display.Text = display.R3;

        foreach (var (flag, lamp) in _lamps)
        {
            var on = display.Lamps.HasFlag(flag);
            lamp.Box.Background = on ? OnBrush(lamp.Color) : LampOffBackground;
            lamp.Text.Foreground = on ? LampOnForeground : LampOffForeground;
        }
    }

    private static Brush OnBrush(LampColor color)
    {
        return color switch
        {
            LampColor.Amber => LampAmber,
            LampColor.Green => LampGreen,
            _ => LampWhite,
        };
    }
}
