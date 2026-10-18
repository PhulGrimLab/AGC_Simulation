using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Agc.Core;
using Agc.Mission;

namespace Agc.Wpf;

/// <summary>
/// 메인 창: 왼쪽 DSKY, 오른쪽 미션 뷰를 조립하고 시뮬레이션 루프를 돌린다.
/// - Owner: App. Lifetime: 앱 실행 동안.
/// - 스레드: UI 스레드 전용. DispatcherTimer가 주기적으로 시뮬레이터/DSKY를 진행시킨다.
/// - 연동: 시뮬레이터의 알람 이벤트를 DSKY.RaiseAlarm으로 전달하고, DSKY는 시뮬레이터를 텔레메트리 공급자로 읽는다.
/// - 로그: 비동기 파일 로거(실행 파일 옆 logs 폴더). 로거 생성에 실패하면 로그 없이 계속 동작한다.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(50);
    private const string LogFolderName = "logs";
    private const string LogFilePrefix = "agc";
    private const double MetersPerKilometer = 1000.0;
    private const double KilometersPerHourFactor = 3.6;

    private readonly ILog _log;
    private readonly MissionSimulator _sim;
    private readonly DskyController _dsky;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _lastTick;
    private bool _updatingSlider;
    private bool _updatingPhaseBox;

    public MainWindow()
    {
        InitializeComponent();

        _log = CreateLog();
        var timeline = Apollo11Mission.Create();
        _sim = new MissionSimulator(timeline, _log);
        _dsky = new DskyController(_sim, _log, _sim);
        _sim.AlarmRaised += alarm => _dsky.RaiseAlarm(alarm.Code);
        _sim.SpeedMultiplier = ReadSpeedMultiplier();

        TimelineSlider.Maximum = timeline.Duration.TotalSeconds;
        foreach (var keyframe in timeline.Keyframes)
        {
            PhaseBox.Items.Add($"{FormatMet(keyframe.Met)}  {keyframe.PhaseName}");
        }

        Dsky.KeyPressed += _dsky.PressKey;
        Dsky.KeyPressed += _ => RefreshView();

        _timer = new DispatcherTimer { Interval = TickInterval };
        _timer.Tick += OnTick;
        Loaded += (_, _) =>
        {
            _log.Info("AGC simulation started (Apollo 11)");
            _stopwatch.Start();
            _timer.Start();
            RefreshView();
        };
        Closed += OnClosed;
    }

    private static ILog CreateLog()
    {
        try
        {
            return new AsyncFileLogger(Path.Combine(AppContext.BaseDirectory, LogFolderName), LogFilePrefix);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 로그 폴더를 만들 수 없어도 앱은 로그 없이 정상 동작해야 한다.
            return NullLog.Instance;
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = _stopwatch.Elapsed;
        var elapsed = now - _lastTick;
        _lastTick = now;

        Step(elapsed);
    }

    /// <summary>
    /// 실제 경과 시간만큼 미션과 DSKY를 한 단계 진행하고 화면을 갱신한다.
    /// 타이머가 주기적으로 호출하며, 테스트에서는 시간을 직접 흘려 보내는 데 쓴다.
    /// </summary>
    internal void Step(TimeSpan elapsed)
    {
        _sim.Advance(elapsed);
        _dsky.Update(elapsed);
        RefreshView();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _log.Info("AGC simulation stopped");
        (_log as IDisposable)?.Dispose();
    }

    /// <summary>DSKY 화면과 미션 정보 표시를 현재 상태로 갱신한다.</summary>
    private void RefreshView()
    {
        var snapshot = _sim.Snapshot;
        var t = snapshot.Telemetry;

        Dsky.Display = _dsky.Display;

        PhaseNameText.Text = $"P{snapshot.Phase.ProgramNumber:D2} · {snapshot.Phase.PhaseName}";
        PhaseDescriptionText.Text = snapshot.Phase.Description;
        MetText.Text = $"MET {FormatMet(snapshot.Met)}";
        BodyText.Text = snapshot.Body == CelestialBody.Earth ? "기준: 지구" : "기준: 달";
        AltitudeText.Text = $"고도  {FormatDistance(t.AltitudeMeters)}";
        SpeedText.Text = string.Create(CultureInfo.InvariantCulture,
            $"속도  {t.SpeedMetersPerSecond:N0} m/s ({t.SpeedMetersPerSecond * KilometersPerHourFactor:N0} km/h)");
        VerticalSpeedText.Text = string.Create(CultureInfo.InvariantCulture,
            $"수직  {t.VerticalSpeedMetersPerSecond:+0.0;-0.0;0.0} m/s");
        PositionText.Text = string.Create(CultureInfo.InvariantCulture,
            $"위치  {t.LatitudeDegrees:0.00}°, {t.LongitudeDegrees:0.00}°");

        _updatingSlider = true;
        TimelineSlider.Value = snapshot.Met.TotalSeconds;
        _updatingSlider = false;

        // 단계 목록도 현재 단계를 가리키게 한다. 그렇지 않으면 같은 항목을 다시 골라도 선택 변경이 없어 이동하지 않는다.
        if (PhaseBox.SelectedIndex != snapshot.PhaseIndex)
        {
            _updatingPhaseBox = true;
            PhaseBox.SelectedIndex = snapshot.PhaseIndex;
            _updatingPhaseBox = false;
        }
        else
        {
            // 이미 현재 단계를 가리키고 있으면 바꿀 것이 없다.
        }

        PlayButton.Content = _sim.IsRunning ? "⏸ 일시정지" : "▶ 재생";

        Illustration.Update(snapshot, _sim.Timeline);
    }

    private static string FormatMet(TimeSpan met)
    {
        return $"{(int)met.TotalHours:D3}:{met.Minutes:D2}:{met.Seconds:D2}";
    }

    private static string FormatDistance(double meters)
    {
        return meters >= MetersPerKilometer
            ? string.Create(CultureInfo.InvariantCulture, $"{meters / MetersPerKilometer:N0} km")
            : string.Create(CultureInfo.InvariantCulture, $"{meters:N0} m");
    }

    private double ReadSpeedMultiplier()
    {
        var item = (ComboBoxItem)SpeedBox.SelectedItem;
        return double.Parse((string)item.Tag, CultureInfo.InvariantCulture);
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_sim.IsRunning && _sim.Met >= _sim.Timeline.Duration)
        {
            // 미션 끝에서 재생을 누르면 바로 다시 멈추므로 처음부터 다시 시작한다.
            _sim.Seek(TimeSpan.Zero);
        }
        else
        {
            // 중간 지점이거나 이미 재생 중이면 그대로 재생/일시정지를 전환한다.
        }

        _sim.IsRunning = !_sim.IsRunning;
        RefreshView();
    }

    private void SpeedBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // InitializeComponent 중에도 호출되므로 시뮬레이터 생성 전에는 무시한다.
        if (_sim is not null)
        {
            _sim.SpeedMultiplier = ReadSpeedMultiplier();
        }
    }

    private void PhaseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 현재 단계 표시를 맞추려고 코드가 선택을 바꾸는 중의 변경은 사용자 조작이 아니다.
        if (_sim is not null && !_updatingPhaseBox && PhaseBox.SelectedIndex >= 0)
        {
            _sim.Seek(_sim.Timeline.Keyframes[PhaseBox.SelectedIndex].Met);
            RefreshView();
        }
    }

    private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // 타이머가 값을 갱신하는 중의 변경은 사용자 조작이 아니므로 무시한다.
        if (_sim is not null && !_updatingSlider)
        {
            _sim.Seek(TimeSpan.FromSeconds(e.NewValue));
            RefreshView();
        }
    }

    /// <summary>PC 키보드를 DSKY 키로 매핑한다. 숫자, +/-, V/N, Enter, C/Backspace, R, K, P.</summary>
    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (HandleKeyDown(e.Key, Keyboard.Modifiers))
        {
            e.Handled = true;
        }
        else
        {
            // DSKY와 관련 없는 키는 기본 동작(슬라이더 조작 등)에 맡긴다.
        }
    }

    /// <summary>
    /// PC 키 입력을 DSKY 키로 변환해 눌러 준다. DSKY 키로 처리했으면 true(이벤트를 처리됨으로 표시해야 함).
    /// 테스트에서 키 이벤트 없이 직접 호출할 수 있도록 분리했다.
    /// </summary>
    internal bool HandleKeyDown(Key key, ModifierKeys modifiers)
    {
        if (DskyKeyboardMap.TryMap(key, modifiers) is not { } dskyKey)
        {
            return false;
        }

        _dsky.PressKey(dskyKey);
        RefreshView();
        return true;
    }
}
