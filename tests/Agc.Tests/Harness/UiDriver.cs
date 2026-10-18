using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Agc.Wpf;
using Agc.Wpf.Controls;

namespace Agc.Tests.Harness;

/// <summary>
/// 메인 창(MainWindow)을 사람이 쓰듯이 조작하고 화면을 읽는 UI 테스트 하네스.
/// - 마우스: 키패드 버튼 클릭(<see cref="Click"/>, <see cref="Press"/>), 재생/배속/슬라이더/단계 이동.
/// - 시간: 창을 화면에 띄우지 않으므로 타이머 대신 <see cref="Step"/>/<see cref="Run"/>으로 시간을 흘려 보낸다.
/// - 읽기: DSKY 창(PROG/VERB/NOUN/R1~R3), 램프, 깜박임, 미션 텍스트.
/// STA 스레드에서만 사용하고, 사용 후 Dispose(창 닫기 + 로거 정리) 한다.
/// </summary>
internal sealed class UiDriver : IDisposable
{
    /// <summary>켜진 램프의 바탕색(흰색/호박색/녹색). 꺼진 램프와 구별하는 데 쓴다.</summary>
    private static readonly Color[] LitLampColors =
    [
        Color.FromRgb(0xF2, 0xF2, 0xF2),
        Color.FromRgb(0xFF, 0xB0, 0x00),
        Color.FromRgb(0x4B, 0xE0, 0x8E),
    ];

    /// <summary>DSKY 램프 라벨 전체(COMP ACTY 포함 13개 + 빈 칸 제외).</summary>
    public static readonly string[] AllLampLabels =
    [
        "COMP ACTY", "UPLINK ACTY", "NO ATT", "STBY", "KEY REL", "OPR ERR",
        "TEMP", "GIMBAL LOCK", "PROG", "RESTART", "TRACKER", "ALT", "VEL",
    ];

    /// <summary>한 번에 흘려 보내는 실시간 간격(타이머 틱 50ms보다 크지만, 60배속에서 12초 단위라 알람 사이 16초보다 짧다).</summary>
    private static readonly TimeSpan StepInterval = TimeSpan.FromMilliseconds(200);

    private readonly MissionTimeline _timeline = Apollo11Mission.Create();
    private bool _closed;

    public UiDriver()
    {
        Window = new MainWindow();
        // 창을 띄우지 않아 Loaded가 일어나지 않으므로 한 번 갱신해서 첫 화면을 만든다.
        Step(TimeSpan.Zero);
    }

    public MainWindow Window { get; }

    /// <summary>
    /// 창을 실제로 화면에 띄우고 이벤트를 처리한다(Loaded, 레이아웃, 실제 DispatcherTimer 시작).
    /// 이후에는 <see cref="Pump"/>로 실제 시간을 흘려 타이머가 동작하게 한다.
    /// </summary>
    public void Show()
    {
        Window.Show();
        Pump(TimeSpan.FromMilliseconds(300));
    }

    /// <summary>
    /// 지정한 실제 시간 동안 WPF 메시지 루프를 돌린다(타이머 틱, 렌더링, 레이아웃이 처리된다).
    /// </summary>
    public static void Pump(TimeSpan duration)
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    /// <summary>창의 현재 모습을 비트맵으로 렌더링한다(창 클라이언트 영역 전체).</summary>
    public System.Windows.Media.Imaging.RenderTargetBitmap Capture()
    {
        Window.UpdateLayout();
        var width = (int)Window.ActualWidth;
        var height = (int)Window.ActualHeight;
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
            width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
        bitmap.Render(Window);
        return bitmap;
    }

    /// <summary>창 안에서 요소가 차지하는 영역(창 좌표).</summary>
    public Rect BoundsOf(FrameworkElement element)
    {
        return element.TransformToAncestor(Window).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
    }

    public MissionTimeline Timeline => _timeline;

    // ---- 컨트롤 찾기 ----

    private T Find<T>(string name) where T : class
    {
        return (Window.FindName(name) as T) ?? throw new AssertFailedException($"컨트롤 {name} 없음");
    }

    public DskyPanel Panel => Find<DskyPanel>("Dsky");

    public Slider Slider => Find<Slider>("TimelineSlider");

    public ComboBox PhaseBox => Find<ComboBox>("PhaseBox");

    public ComboBox SpeedBox => Find<ComboBox>("SpeedBox");

    public Button PlayButton => Find<Button>("PlayButton");

    // ---- 읽기: DSKY ----

    public string Prog => Seg("ProgDisplay");

    public string Verb => Seg("VerbDisplay");

    public string Noun => Seg("NounDisplay");

    public string R1 => Seg("R1Display");

    public string R2 => Seg("R2Display");

    public string R3 => Seg("R3Display");

    private string Seg(string name)
    {
        return ((SegmentDisplay)Panel.FindName(name)).Text;
    }

    /// <summary>VERB/NOUN 창이 지금 보이는지(깜박임의 꺼진 위상이면 false).</summary>
    public bool VerbNounVisible => ((SegmentDisplay)Panel.FindName("VerbDisplay")).Opacity > 0.5;

    /// <summary>깜박임 위상을 한 단계 넘긴다(패널 타이머를 기다리지 않고 테스트에서 직접 진행).</summary>
    public void AdvanceFlash()
    {
        Panel.AdvanceFlashPhase();
    }

    public bool LampLit(string label)
    {
        var box = label == "COMP ACTY"
            ? (Border)((Border)Panel.FindName("CompActyHost")).Child
            : ((UniformGrid)Panel.FindName("LampGrid")).Children.OfType<Border>()
                .Single(b => b.Child is TextBlock t && t.Text == label);
        return LitLampColors.Contains(((SolidColorBrush)box.Background).Color);
    }

    public IReadOnlyList<string> LitLamps => AllLampLabels.Where(LampLit).ToList();

    // ---- 읽기: 미션 뷰 ----

    public string MetText => Text("MetText");

    public string PhaseName => Text("PhaseNameText");

    public string PhaseDescription => Text("PhaseDescriptionText");

    public string BodyText => Text("BodyText");

    public string AltitudeText => Text("AltitudeText");

    public string SpeedText => Text("SpeedText");

    public string VerticalSpeedText => Text("VerticalSpeedText");

    public string PositionText => Text("PositionText");

    public string PlayLabel => (string)PlayButton.Content;

    public bool IsPlaying => PlayLabel.Contains("일시정지");

    /// <summary>슬라이더가 가리키는 미션 시간(초).</summary>
    public double MetSeconds => Slider.Value;

    private string Text(string name)
    {
        return Find<TextBlock>(name).Text;
    }

    // ---- 조작: 마우스 ----

    /// <summary>키패드 버튼 하나를 마우스로 클릭한다(라벨은 VERB, NOUN, ENTR, KEY REL, 7, + 등).</summary>
    public void Click(string label)
    {
        var button = ((Grid)Panel.FindName("KeypadGrid")).Children.OfType<Button>()
            .Single(b => (string)b.Content == label);
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

    /// <summary>
    /// 키 입력을 문자열로 클릭한다. 표기: 숫자, V=VERB, N=NOUN, E=ENTR, C=CLR, R=RSET, K=KEY REL, P=PRO, +, -.
    /// 공백은 무시한다.
    /// </summary>
    public void Press(string keys)
    {
        foreach (var c in keys.Where(ch => ch != ' '))
        {
            Click(LabelOf(c));
        }
    }

    private static string LabelOf(char c)
    {
        return c switch
        {
            'V' => "VERB",
            'N' => "NOUN",
            'E' => "ENTR",
            'C' => "CLR",
            'R' => "RSET",
            'K' => "KEY REL",
            'P' => "PRO",
            '-' => "−",
            _ => c.ToString(),
        };
    }

    public void ClickPlay()
    {
        PlayButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
    }

    /// <summary>배속 목록에서 선택한다(1, 10, 60, 600, 3600).</summary>
    public void SetSpeed(int multiplier)
    {
        var item = SpeedBox.Items.OfType<ComboBoxItem>().Single(i => (string)i.Tag == multiplier.ToString());
        SpeedBox.SelectedItem = item;
    }

    /// <summary>슬라이더를 움직여 해당 미션 시간으로 이동한다.</summary>
    public void SeekTo(TimeSpan met)
    {
        Slider.Value = met.TotalSeconds;
    }

    public void SeekTo(int hours, int minutes, int seconds)
    {
        SeekTo(new TimeSpan(hours, minutes, seconds));
    }

    /// <summary>"단계 이동" 목록에서 단계를 고른다.</summary>
    public void SelectPhase(int index)
    {
        PhaseBox.SelectedIndex = index;
    }

    public int PhaseIndexOf(string phaseName)
    {
        return _timeline.Keyframes.ToList().FindIndex(k => k.PhaseName == phaseName);
    }

    // ---- 시간 ----

    /// <summary>실시간 경과를 한 번에 흘려 보낸다(타이머 1틱과 같다).</summary>
    public void Step(TimeSpan elapsed)
    {
        Window.Step(elapsed);
    }

    /// <summary>실시간 seconds초를 짧은 간격(0.2초)으로 나눠 흘려 보낸다. 알람 같은 순간 사건을 놓치지 않는다.</summary>
    public void Run(double seconds)
    {
        // 부동소수점 누적 오차로 마지막 스텝이 1틱 모자라는 일이 없도록 정수 틱으로 계산한다.
        var remaining = TimeSpan.FromTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond));
        while (remaining > TimeSpan.Zero)
        {
            var dt = remaining < StepInterval ? remaining : StepInterval;
            Step(dt);
            remaining -= dt;
        }
    }

    /// <summary>조건이 참이 될 때까지(최대 maxSeconds) 실시간을 흘려 보낸다. 참이 되었으면 true.</summary>
    public bool RunUntil(Func<bool> condition, double maxSeconds)
    {
        var limit = TimeSpan.FromTicks((long)Math.Round(maxSeconds * TimeSpan.TicksPerSecond));
        var elapsed = TimeSpan.Zero;
        while (elapsed < limit)
        {
            if (condition())
            {
                return true;
            }

            Step(StepInterval);
            elapsed += StepInterval;
        }

        return condition();
    }

    // ---- 키보드 ----

    /// <summary>PC 키 입력을 창에 보낸다(이벤트 없이 처리 함수 호출). 처리했으면 true.</summary>
    public bool KeyDown(System.Windows.Input.Key key, System.Windows.Input.ModifierKeys modifiers = System.Windows.Input.ModifierKeys.None)
    {
        return Window.HandleKeyDown(key, modifiers);
    }

    // ---- 검증 보조 ----

    /// <summary>
    /// DSKY 화면이 항상 지켜야 하는 형식 규칙을 검사한다: PROG/VERB/NOUN 2글자, R1~R3 6글자,
    /// 레지스터 부호 칸은 공백/+/-, 나머지는 숫자/공백.
    /// </summary>
    public void AssertDisplayWellFormed()
    {
        Assert.AreEqual(2, Prog.Length, $"PROG '{Prog}'");
        Assert.AreEqual(2, Verb.Length, $"VERB '{Verb}'");
        Assert.AreEqual(2, Noun.Length, $"NOUN '{Noun}'");
        foreach (var (name, value) in new[] { ("R1", R1), ("R2", R2), ("R3", R3) })
        {
            Assert.AreEqual(6, value.Length, $"{name} '{value}'");
            Assert.IsTrue(value[0] is ' ' or '+' or '-', $"{name} 부호 칸 '{value}'");
            Assert.IsTrue(value.Skip(1).All(ch => char.IsDigit(ch) || ch == ' '), $"{name} 숫자 칸 '{value}'");
        }
    }

    public void Dispose()
    {
        if (!_closed)
        {
            _closed = true;
            Window.Close();
        }
    }
}
