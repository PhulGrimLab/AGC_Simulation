using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Agc.Wpf.Controls;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: DSKY 화면 컨트롤(SegmentDisplay, DskyPanel)의 렌더링, 실제 DSKY 배치, 키패드 동작.
/// WPF 컨트롤은 STA 스레드가 필요하므로 [STATestMethod]를 사용한다.
/// WPF의 XAML(BAML) 로드는 여러 스레드에서 동시에 수행하면 내부 캐시 경합이 생기므로 직렬로 실행한다.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DskyPanelUiTests
{
    private const int PanelWidth = 590;
    private const int PanelHeight = 646;
    private const int LitGreenThreshold = 0xF0;
    private const int LitBlueMax = 0xC0;
    private const int KeypadRowUnits = 6;

    public TestContext? TestContext { get; set; }

    private static DskyPanel CreateArrangedPanel()
    {
        var panel = new DskyPanel();
        panel.Measure(new Size(PanelWidth, PanelHeight));
        panel.Arrange(new Rect(0, 0, PanelWidth, PanelHeight));
        panel.UpdateLayout();
        return panel;
    }

    private static RenderTargetBitmap Render(FrameworkElement element, int width, int height)
    {
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        return bitmap;
    }

    /// <summary>지정한 영역(전체면 null) 안의 밝은 초록(켜진 세그먼트) 픽셀 수를 센다.</summary>
    private static int CountLitPixels(BitmapSource bitmap, Rect? region = null)
    {
        var area = region is { } r
            ? new Int32Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height)
            : new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
        var stride = area.Width * 4;
        var pixels = new byte[stride * area.Height];
        bitmap.CopyPixels(area, pixels, stride, 0);
        var lit = 0;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            // BGRA: 켜진 세그먼트는 초록 채널이 강하고 파랑 채널은 중간 정도(7C FF B2).
            if (pixels[i + 1] >= LitGreenThreshold && pixels[i] < LitBlueMax)
            {
                lit++;
            }
        }

        return lit;
    }

    /// <summary>요소가 root 안에서 차지하는 영역.</summary>
    private static Rect BoundsIn(FrameworkElement root, FrameworkElement element)
    {
        return element.TransformToAncestor(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    }

    private static T Find<T>(FrameworkElement root, string name) where T : class
    {
        return (root.FindName(name) as T) ?? throw new AssertFailedException($"{name} 없음");
    }

    private static DskyDisplay Display(string prog = "00", string verb = "  ", string noun = "  ",
        string r1 = "      ", string r2 = "      ", string r3 = "      ", DskyLamps lamps = DskyLamps.None)
    {
        return new DskyDisplay(prog, verb, noun, r1, r2, r3, lamps);
    }

    private static SegmentDisplay MeasuredSegment(string text, int digits)
    {
        var display = new SegmentDisplay { Text = text, DigitCount = digits, Height = 60 };
        display.Measure(new Size(double.PositiveInfinity, 60));
        display.Arrange(new Rect(display.DesiredSize));
        return display;
    }

    private static Color BackgroundOf(Border border)
    {
        return ((SolidColorBrush)border.Background).Color;
    }

    private static Border LampBox(UniformGrid grid, string label)
    {
        return grid.Children.OfType<Border>().Single(b => b.Child is TextBlock t && t.Text == label);
    }

    // ---- SegmentDisplay ----

    [STATestMethod]
    [TestCategory("UI-P01")]
    public void SegmentDisplay_Digit8_LightsMoreThanDigit1()
    {
        var eight = MeasuredSegment("8", 1);
        var one = MeasuredSegment("1", 1);

        var eightPixels = CountLitPixels(Render(eight, (int)eight.ActualWidth, 60));
        var onePixels = CountLitPixels(Render(one, (int)one.ActualWidth, 60));

        Assert.IsGreaterThan(onePixels * 2, eightPixels);
        Assert.IsGreaterThan(0, onePixels);
    }

    [STATestMethod]
    [TestCategory("UI-P02")]
    public void SegmentDisplay_BlankText_LightsNothing()
    {
        var blank = MeasuredSegment("   ", 3);

        Assert.AreEqual(0, CountLitPixels(Render(blank, (int)blank.ActualWidth, 60)));
    }

    [STATestMethod]
    [TestCategory("UI-P03")]
    public void SegmentDisplay_PlusLightsMoreThanMinus()
    {
        var plus = MeasuredSegment("+", 1);
        var minus = MeasuredSegment("-", 1);

        Assert.IsGreaterThan(
            CountLitPixels(Render(minus, (int)minus.ActualWidth, 60)),
            CountLitPixels(Render(plus, (int)plus.ActualWidth, 60)));
    }

    // ---- 표시 내용 ----

    [STATestMethod]
    [TestCategory("UI-P04")]
    public void Panel_Display_UpdatesAllSegmentWindows()
    {
        var panel = CreateArrangedPanel();

        panel.Display = Display("63", "16", "63", "+10000", "-01000", "+06076");

        Assert.AreEqual("63", Find<SegmentDisplay>(panel, "ProgDisplay").Text);
        Assert.AreEqual("16", Find<SegmentDisplay>(panel, "VerbDisplay").Text);
        Assert.AreEqual("63", Find<SegmentDisplay>(panel, "NounDisplay").Text);
        Assert.AreEqual("+10000", Find<SegmentDisplay>(panel, "R1Display").Text);
        Assert.AreEqual("-01000", Find<SegmentDisplay>(panel, "R2Display").Text);
        Assert.AreEqual("+06076", Find<SegmentDisplay>(panel, "R3Display").Text);
    }

    [STATestMethod]
    [TestCategory("UI-P05")]
    public void Panel_Lamps_LitColorsFollowRealDsky()
    {
        var panel = CreateArrangedPanel();
        var grid = Find<UniformGrid>(panel, "LampGrid");
        var compActy = (Border)Find<Border>(panel, "CompActyHost").Child;

        panel.Display = Display(lamps: DskyLamps.None);
        var unlit = BackgroundOf(LampBox(grid, "PROG"));
        var compUnlit = BackgroundOf(compActy);

        panel.Display = Display(lamps: DskyLamps.Prog | DskyLamps.OprErr | DskyLamps.CompActy);

        var prog = BackgroundOf(LampBox(grid, "PROG"));
        var oprErr = BackgroundOf(LampBox(grid, "OPR ERR"));
        var comp = BackgroundOf(compActy);
        Assert.AreNotEqual(unlit, prog);
        Assert.AreEqual(Color.FromRgb(0xFF, 0xB0, 0x00), prog, "오른쪽 열 경고등은 호박색");
        Assert.AreEqual(Color.FromRgb(0xF2, 0xF2, 0xF2), oprErr, "왼쪽 열 램프는 흰색");
        Assert.AreNotEqual(compUnlit, comp);
        Assert.IsGreaterThan(comp.R, comp.G, "COMP ACTY는 녹색 계열");
        Assert.AreEqual(unlit, BackgroundOf(LampBox(grid, "TEMP")), "켜지지 않은 램프는 그대로");
    }

    // ---- 깜박임 ----

    [STATestMethod]
    [TestCategory("UI-P06")]
    public void Flash_VerbAndNounBlinkOnlyWhenDisplayRequestsIt()
    {
        var panel = CreateArrangedPanel();
        var verb = Find<SegmentDisplay>(panel, "VerbDisplay");
        var noun = Find<SegmentDisplay>(panel, "NounDisplay");
        var prog = Find<SegmentDisplay>(panel, "ProgDisplay");

        panel.Display = new DskyDisplay("00", "25", "36", "      ", "      ", "      ", DskyLamps.None, VerbNounFlashing: true);
        Assert.AreEqual(1.0, verb.Opacity, "시작 위상은 켜짐");

        panel.AdvanceFlashPhase();
        Assert.AreEqual(0.0, verb.Opacity);
        Assert.AreEqual(0.0, noun.Opacity);
        Assert.AreEqual(1.0, prog.Opacity, "PROG와 레지스터는 깜박이지 않는다");

        panel.AdvanceFlashPhase();
        Assert.AreEqual(1.0, verb.Opacity);
    }

    [STATestMethod]
    [TestCategory("UI-P07")]
    public void Flash_StopsAndStaysVisibleWhenFlashingEnds()
    {
        var panel = CreateArrangedPanel();
        var verb = Find<SegmentDisplay>(panel, "VerbDisplay");
        panel.Display = new DskyDisplay("00", "25", "36", "      ", "      ", "      ", DskyLamps.None, VerbNounFlashing: true);
        panel.AdvanceFlashPhase();
        Assert.AreEqual(0.0, verb.Opacity);

        panel.Display = Display("00", "16", "36");

        Assert.AreEqual(1.0, verb.Opacity, "깜박임이 끝나면 꺼진 위상에서도 다시 켜진다");
        panel.AdvanceFlashPhase();
        Assert.AreEqual(1.0, verb.Opacity, "깜박이지 않는 표시는 위상이 바뀌어도 켜져 있다");
    }

    // ---- 실제 DSKY 배치 ----

    [STATestMethod]
    [TestCategory("UI-P08")]
    public void Layout_LampPanel_HasRealTwoByFourteenOrder()
    {
        var panel = CreateArrangedPanel();
        var grid = Find<UniformGrid>(panel, "LampGrid");

        var labels = grid.Children.OfType<Border>()
            .Select(b => (b.Child as TextBlock)?.Text)
            .ToArray();

        Assert.AreEqual(2, grid.Columns);
        CollectionAssert.AreEqual(
            new string?[]
            {
                "UPLINK ACTY", "TEMP", "NO ATT", "GIMBAL LOCK", "STBY", "PROG", "KEY REL", "RESTART",
                "OPR ERR", "TRACKER", null, "ALT", null, "VEL",
            },
            labels);
    }

    [STATestMethod]
    [TestCategory("UI-P09")]
    public void Layout_DisplayPanel_HasFiveLinesInRealOrder()
    {
        var panel = CreateArrangedPanel();
        var compActy = BoundsIn(panel, Find<Border>(panel, "CompActyHost"));
        var prog = BoundsIn(panel, Find<SegmentDisplay>(panel, "ProgDisplay"));
        var verb = BoundsIn(panel, Find<SegmentDisplay>(panel, "VerbDisplay"));
        var noun = BoundsIn(panel, Find<SegmentDisplay>(panel, "NounDisplay"));
        var r1 = BoundsIn(panel, Find<SegmentDisplay>(panel, "R1Display"));
        var r2 = BoundsIn(panel, Find<SegmentDisplay>(panel, "R2Display"));
        var r3 = BoundsIn(panel, Find<SegmentDisplay>(panel, "R3Display"));

        Assert.IsLessThan(prog.Left, compActy.Right, "1줄: COMP ACTY는 PROG의 왼쪽");
        Assert.IsLessThan(verb.Top, prog.Bottom, "2줄(VERB/NOUN)은 1줄 아래");
        Assert.IsLessThan(noun.Left, verb.Right, "VERB는 NOUN의 왼쪽");
        Assert.IsLessThan(r1.Top, verb.Bottom, "R1은 VERB/NOUN 아래");
        Assert.IsLessThan(r2.Top, r1.Bottom);
        Assert.IsLessThan(r3.Top, r2.Bottom);
    }

    [STATestMethod]
    [TestCategory("UI-P10")]
    public void Layout_Keypad_HasNineteenKeysInSevenColumnsWithHalfRowOffsets()
    {
        var panel = CreateArrangedPanel();
        var keypad = Find<Grid>(panel, "KeypadGrid");
        var keys = keypad.Children.OfType<Button>().ToDictionary(
            b => (string)b.Content,
            b => (Row: Grid.GetRow(b), Col: Grid.GetColumn(b), Span: Grid.GetRowSpan(b)));

        // 한 행 = 2 단위. 일반 키는 0/2/4에서 시작하고 VERB/NOUN/ENTR/RSET은 1/3에서 시작(반 칸 아래).
        var expected = new Dictionary<string, (int Row, int Col, int Span)>
        {
            ["VERB"] = (1, 0, 2), ["NOUN"] = (3, 0, 2),
            ["+"] = (0, 1, 2), ["−"] = (2, 1, 2), ["0"] = (4, 1, 2),
            ["7"] = (0, 2, 2), ["8"] = (0, 3, 2), ["9"] = (0, 4, 2),
            ["4"] = (2, 2, 2), ["5"] = (2, 3, 2), ["6"] = (2, 4, 2),
            ["1"] = (4, 2, 2), ["2"] = (4, 3, 2), ["3"] = (4, 4, 2),
            ["CLR"] = (0, 5, 2), ["PRO"] = (2, 5, 2), ["KEY REL"] = (4, 5, 2),
            ["ENTR"] = (1, 6, 2), ["RSET"] = (3, 6, 2),
        };

        Assert.HasCount(19, keys);
        foreach (var (label, position) in expected)
        {
            Assert.AreEqual(position, keys[label], label);
        }

        Assert.AreEqual(7, keypad.ColumnDefinitions.Count);
        Assert.AreEqual(KeypadRowUnits, keypad.RowDefinitions.Count);

        // VERB~NOUN 묶음(행 1~5)과 ENTR~RSET 묶음의 중심이 3행 전체(0~6)의 중앙(3)에 놓인다.
        Assert.AreEqual(3.0, (keys["VERB"].Row + keys["NOUN"].Row + keys["NOUN"].Span) / 2.0);
        Assert.AreEqual(3.0, (keys["ENTR"].Row + keys["RSET"].Row + keys["RSET"].Span) / 2.0);
    }

    [STATestMethod]
    [TestCategory("UI-P11")]
    public void Layout_Keys_AreSquareWithinTheirCells()
    {
        var panel = CreateArrangedPanel();
        var keypad = Find<Grid>(panel, "KeypadGrid");

        foreach (var button in keypad.Children.OfType<Button>())
        {
            Assert.AreEqual(button.ActualWidth, button.ActualHeight, 1.0, (string)button.Content);
        }
    }

    // ---- 키 동작 ----

    [STATestMethod]
    [TestCategory("UI-P12")]
    public void Panel_Keypad_EveryButtonRaisesItsKey()
    {
        var panel = CreateArrangedPanel();
        var keypad = Find<Grid>(panel, "KeypadGrid");
        var pressed = new List<DskyKey>();
        panel.KeyPressed += pressed.Add;

        var expected = new Dictionary<string, DskyKey>
        {
            ["VERB"] = DskyKey.Verb, ["NOUN"] = DskyKey.Noun, ["+"] = DskyKey.Plus, ["−"] = DskyKey.Minus,
            ["CLR"] = DskyKey.Clear, ["PRO"] = DskyKey.Proceed, ["KEY REL"] = DskyKey.KeyRelease,
            ["ENTR"] = DskyKey.Enter, ["RSET"] = DskyKey.Reset,
        };
        for (var d = 0; d <= 9; d++)
        {
            expected[d.ToString()] = (DskyKey)d;
        }

        foreach (var button in keypad.Children.OfType<Button>())
        {
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.AreEqual(expected[(string)button.Content], pressed[^1], (string)button.Content);
        }

        Assert.HasCount(expected.Count, pressed);
    }

    [STATestMethod]
    [TestCategory("UI-P13")]
    public void Panel_Keypad_ButtonsDoNotStealKeyboardFocus()
    {
        var panel = CreateArrangedPanel();
        var keypad = Find<Grid>(panel, "KeypadGrid");

        Assert.IsTrue(keypad.Children.OfType<Button>().All(b => !b.Focusable));
    }

    // ---- 렌더링 ----

    [STATestMethod]
    [TestCategory("UI-P14")]
    public void Panel_FullDisplay_LitSegmentsAppearOnlyWhereValuesAreShown()
    {
        var panel = CreateArrangedPanel();
        panel.Display = Display("63", "16", "63", "+10000", "-01000", "+06076", DskyLamps.Prog);
        panel.UpdateLayout();
        var bitmap = Render(panel, PanelWidth, PanelHeight);

        var r1 = BoundsIn(panel, Find<SegmentDisplay>(panel, "R1Display"));
        Assert.IsGreaterThan(100, CountLitPixels(bitmap, r1));

        // 값이 없는(공백) 상태로 다시 그리면 같은 영역에 켜진 픽셀이 없어야 한다.
        panel.Display = Display();
        panel.UpdateLayout();
        Assert.AreEqual(0, CountLitPixels(Render(panel, PanelWidth, PanelHeight), r1));

        // 육안 확인용 스냅샷(임시 폴더).
        panel.Display = Display("63", "16", "63", "+10000", "-01000", "+06076", DskyLamps.Prog | DskyLamps.CompActy);
        panel.UpdateLayout();
        var path = Path.Combine(Path.GetTempPath(), "agc_dsky_panel.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(Render(panel, PanelWidth, PanelHeight)));
        using var stream = File.Create(path);
        encoder.Save(stream);
        TestContext?.WriteLine($"snapshot: {path}");
    }
}
