using System.Windows;
using System.Windows.Controls;
using Agc.Wpf;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 메인 창의 미션 컨트롤(재생/타임라인/단계 이동) 동작.
/// 창을 화면에 띄우지 않고 생성만 해서 컨트롤 이벤트를 직접 일으켜 검증한다(타이머는 시작되지 않는다).
/// WPF XAML 로드 경합을 피하려고 직렬로 실행한다.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MainWindowUiTests
{
    private static readonly MissionTimeline Timeline = Apollo11Mission.Create();

    private static T Find<T>(MainWindow window, string name) where T : class
    {
        return (window.FindName(name) as T) ?? throw new AssertFailedException($"{name} 없음");
    }

    private static int PhaseIndexOf(string phaseName)
    {
        return Timeline.Keyframes.ToList().FindIndex(k => k.PhaseName == phaseName);
    }

    [STATestMethod]
    public void Play_AfterMissionEnd_RestartsFromTheBeginning()
    {
        var window = new MainWindow();
        try
        {
            var slider = Find<Slider>(window, "TimelineSlider");
            var play = Find<Button>(window, "PlayButton");
            var met = Find<TextBlock>(window, "MetText");
            slider.Value = slider.Maximum;
            StringAssert.Contains(met.Text, "195:18:35");

            play.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));

            StringAssert.Contains(met.Text, "000:00:00", "끝에서 재생하면 처음부터 다시 시작한다");
            StringAssert.Contains((string)play.Content, "일시정지", "재생 상태가 유지된다");
        }
        finally
        {
            window.Close();
        }
    }

    [STATestMethod]
    public void PhaseBox_FollowsTheMissionPosition()
    {
        var window = new MainWindow();
        try
        {
            var slider = Find<Slider>(window, "TimelineSlider");
            var phaseBox = Find<ComboBox>(window, "PhaseBox");
            var pdi = PhaseIndexOf("동력 하강 시작(PDI)");

            slider.Value = new TimeSpan(102, 34, 0).TotalSeconds;

            Assert.AreEqual(pdi, phaseBox.SelectedIndex, "슬라이더로 옮기면 단계 목록도 현재 단계를 가리킨다");
        }
        finally
        {
            window.Close();
        }
    }

    [STATestMethod]
    public void PhaseBox_SelectingTheSamePhaseAgainStillJumps()
    {
        var window = new MainWindow();
        try
        {
            var slider = Find<Slider>(window, "TimelineSlider");
            var phaseBox = Find<ComboBox>(window, "PhaseBox");
            var met = Find<TextBlock>(window, "MetText");
            var pdi = PhaseIndexOf("동력 하강 시작(PDI)");

            phaseBox.SelectedIndex = pdi;
            StringAssert.Contains(met.Text, "102:33:05");

            slider.Value = 0;
            phaseBox.SelectedIndex = pdi;

            StringAssert.Contains(met.Text, "102:33:05", "다른 곳으로 옮긴 뒤 같은 단계를 다시 골라도 이동해야 한다");
        }
        finally
        {
            window.Close();
        }
    }
}
