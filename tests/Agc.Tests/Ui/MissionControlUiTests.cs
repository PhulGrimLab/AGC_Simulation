using Agc.Tests.Harness;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 오른쪽 미션 뷰와 제어(UI-S 시나리오): 재생/일시정지, 배속, 타임라인 슬라이더, 단계 이동, 텔레메트리 표시, 알람 연동.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class MissionControlUiTests
{
    private static string MetLabel(TimeSpan met)
    {
        return $"MET {(int)met.TotalHours:D3}:{met.Minutes:D2}:{met.Seconds:D2}";
    }

    [STATestMethod]
    [TestCategory("UI-S01")]
    public void InitialState_IsPausedAtLiftoffWithDefaultSpeed()
    {
        using var ui = new UiDriver();

        Assert.IsFalse(ui.IsPlaying);
        Assert.AreEqual("▶ 재생", ui.PlayLabel);
        Assert.AreEqual(0, ui.MetSeconds);
        Assert.AreEqual("MET 000:00:00", ui.MetText);
        Assert.AreEqual(0, ui.PhaseBox.SelectedIndex);
        Assert.HasCount(ui.Timeline.Keyframes.Count, ui.PhaseBox.Items);
        StringAssert.Contains(ui.PhaseName, "발사");
        StringAssert.Contains(ui.PhaseName, "P11");
        Assert.AreEqual(60, int.Parse((string)((System.Windows.Controls.ComboBoxItem)ui.SpeedBox.SelectedItem).Tag), "기본 배속 60×");
    }

    [STATestMethod]
    [TestCategory("UI-S02")]
    public void PlayAndPause_ToggleTheButtonAndTheClock()
    {
        using var ui = new UiDriver();

        ui.ClickPlay();
        Assert.IsTrue(ui.IsPlaying);
        Assert.AreEqual("⏸ 일시정지", ui.PlayLabel);
        ui.Run(1.0);
        Assert.AreEqual("MET 000:01:00", ui.MetText, "60배속 1초 = 1분");

        ui.ClickPlay();
        Assert.IsFalse(ui.IsPlaying);
        Assert.AreEqual("▶ 재생", ui.PlayLabel);
        ui.Run(2.0);
        Assert.AreEqual("MET 000:01:00", ui.MetText, "일시정지 중에는 시간이 흐르지 않는다");
    }

    [STATestMethod]
    [TestCategory("UI-S03")]
    public void EverySpeedOption_AdvancesMissionByThatManySecondsPerRealSecond()
    {
        using var ui = new UiDriver();

        foreach (var speed in new[] { 1, 10, 60, 600, 3600 })
        {
            ui.SeekTo(TimeSpan.Zero);
            ui.SetSpeed(speed);
            ui.ClickPlay();
            ui.Run(1.0);
            ui.ClickPlay();

            Assert.AreEqual(speed, ui.MetSeconds, 1e-6, $"{speed}×");
        }
    }

    [STATestMethod]
    [TestCategory("UI-S04")]
    public void SpeedChange_WhilePlaying_TakesEffectImmediately()
    {
        using var ui = new UiDriver();
        ui.SeekTo(100, 0, 0);
        ui.ClickPlay();
        ui.Run(2.0);
        Assert.AreEqual(100 * 3600 + 120, ui.MetSeconds, 1e-6);

        ui.SetSpeed(600);
        ui.Run(1.0);

        Assert.AreEqual(100 * 3600 + 120 + 600, ui.MetSeconds, 1e-6);
    }

    [STATestMethod]
    [TestCategory("UI-S05")]
    public void Slider_SeekShowsMetPhaseAndDescription()
    {
        using var ui = new UiDriver();

        ui.SeekTo(102, 33, 5);

        Assert.AreEqual("MET 102:33:05", ui.MetText);
        StringAssert.Contains(ui.PhaseName, "P63");
        StringAssert.Contains(ui.PhaseName, "동력 하강 시작");
        StringAssert.Contains(ui.PhaseDescription, "P63");
        Assert.AreEqual("기준: 달", ui.BodyText);
        Assert.AreEqual("63", ui.Prog);
    }

    [STATestMethod]
    [TestCategory("UI-S06")]
    public void Slider_RangeCoversTheWholeMission()
    {
        using var ui = new UiDriver();

        Assert.AreEqual(0, ui.Slider.Minimum);
        Assert.AreEqual(new TimeSpan(195, 18, 35).TotalSeconds, ui.Slider.Maximum);

        ui.SeekTo(TimeSpan.FromDays(100));
        Assert.AreEqual("MET 195:18:35", ui.MetText, "범위를 넘으면 끝으로 제한된다");
        StringAssert.Contains(ui.PhaseName, "착수");
    }

    [STATestMethod]
    [TestCategory("UI-S07")]
    public void PhaseJump_EveryPhase_ShowsItsStartTimeNameProgramAndDescription()
    {
        using var ui = new UiDriver();

        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            var keyframe = ui.Timeline.Keyframes[i];
            ui.SelectPhase(i);

            Assert.AreEqual(MetLabel(keyframe.Met), ui.MetText, keyframe.PhaseName);
            StringAssert.Contains(ui.PhaseName, keyframe.PhaseName);
            StringAssert.Contains(ui.PhaseName, $"P{keyframe.ProgramNumber:D2}");
            Assert.AreEqual($"{keyframe.ProgramNumber:D2}", ui.Prog, $"PROG 창: {keyframe.PhaseName}");
            Assert.AreEqual(keyframe.Description, ui.PhaseDescription);
            Assert.AreEqual(i, ui.PhaseBox.SelectedIndex);
            ui.AssertDisplayWellFormed();
        }
    }

    [STATestMethod]
    [TestCategory("UI-S08")]
    public void MissionEnd_StopsPlaybackAutomatically()
    {
        using var ui = new UiDriver();
        ui.SeekTo(194, 0, 0);
        ui.SetSpeed(3600);
        ui.ClickPlay();

        var stopped = ui.RunUntil(() => !ui.IsPlaying, 30);

        Assert.IsTrue(stopped, "끝에 도달하면 자동으로 일시정지");
        Assert.AreEqual("MET 195:18:35", ui.MetText);
        Assert.AreEqual("▶ 재생", ui.PlayLabel);
        StringAssert.Contains(ui.PhaseName, "착수");
        Assert.AreEqual("00", ui.Prog);
    }

    [STATestMethod]
    [TestCategory("UI-S09")]
    public void Telemetry_AtLiftoffAndAtPdi_ShowsExpectedTexts()
    {
        using var ui = new UiDriver();

        StringAssert.Contains(ui.AltitudeText, "0 m");
        StringAssert.Contains(ui.SpeedText, "0 m/s");
        Assert.AreEqual("기준: 지구", ui.BodyText);
        Assert.AreEqual("위치  28.60°, -80.60°", ui.PositionText);
        StringAssert.StartsWith(ui.VerticalSpeedText, "수직  +", "상승 중이므로 양수");

        ui.SeekTo(102, 33, 5);
        Assert.AreEqual("고도  15 km", ui.AltitudeText);
        Assert.AreEqual("속도  1,700 m/s (6,120 km/h)", ui.SpeedText);
        Assert.AreEqual("기준: 달", ui.BodyText);
        StringAssert.StartsWith(ui.VerticalSpeedText, "수직  -", "하강 중이므로 음수");
    }

    [STATestMethod]
    [TestCategory("UI-S10")]
    public void ReferenceBody_SwitchesToMoonAtLunarSphereEntry()
    {
        using var ui = new UiDriver();

        ui.SeekTo(61, 39, 49);
        Assert.AreEqual("기준: 지구", ui.BodyText);

        ui.SeekTo(61, 39, 50);
        Assert.AreEqual("기준: 달", ui.BodyText);
    }

    [STATestMethod]
    [TestCategory("UI-S11")]
    public void LandingSite_ShowsTranquilityCoordinatesOnTheGround()
    {
        using var ui = new UiDriver();

        ui.SeekTo(102, 45, 40);

        Assert.AreEqual("위치  0.67°, 23.47°", ui.PositionText);
        Assert.AreEqual("고도  0 m", ui.AltitudeText);
        Assert.AreEqual("속도  0 m/s (0 km/h)", ui.SpeedText);
        Assert.AreEqual("68", ui.Prog);
    }

    [STATestMethod]
    [TestCategory("UI-S12")]
    public void Slider_FollowsPlaybackAndSeekFromTheSliderContinuesFromThere()
    {
        using var ui = new UiDriver();
        ui.ClickPlay();
        ui.Run(1.0);
        Assert.AreEqual(60, ui.MetSeconds, 1e-6, "재생 중 슬라이더가 따라간다");

        ui.SeekTo(10, 0, 0);
        ui.Run(1.0);

        Assert.AreEqual(10 * 3600 + 60, ui.MetSeconds, 1e-6, "슬라이더로 옮긴 지점부터 이어서 진행");
        Assert.IsTrue(ui.IsPlaying);
    }

    [STATestMethod]
    [TestCategory("UI-S13")]
    public void PhaseBox_FollowsPlaybackAcrossPhaseBoundaries()
    {
        using var ui = new UiDriver();
        ui.SeekTo(0, 2, 0);
        ui.SetSpeed(60);
        ui.ClickPlay();
        Assert.AreEqual(0, ui.PhaseBox.SelectedIndex);

        ui.RunUntil(() => ui.PhaseBox.SelectedIndex == 1, 10);

        Assert.AreEqual(1, ui.PhaseBox.SelectedIndex, "0:02:42 1단 분리 단계로 넘어가면 목록도 따라간다");
        StringAssert.Contains(ui.PhaseName, "1단 분리");
    }

    [STATestMethod]
    [TestCategory("UI-S14")]
    public void Alarm_WhilePlaying_LightsProgLampButSeekDoesNot()
    {
        using var ui = new UiDriver();

        ui.SeekTo(102, 50, 0);
        Assert.IsFalse(ui.LampLit("PROG"), "Seek는 지나간 알람을 재현하지 않는다");

        ui.SeekTo(102, 38, 0);
        ui.ClickPlay();
        var lit = ui.RunUntil(() => ui.LampLit("PROG"), 5);

        Assert.IsTrue(lit, "102:38:26 첫 1202 알람");
        StringAssert.Contains(ui.MetText, "102:38:");
    }

    [STATestMethod]
    [TestCategory("UI-S15")]
    public void Illustration_ShowsTheSceneOfTheCurrentPhase_AndItsCaptionChanges()
    {
        using var ui = new UiDriver();
        var illustration = (Agc.Wpf.Controls.MissionIllustration)ui.Window.FindName("Illustration");

        var seen = new Dictionary<MissionScene, string?>();
        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            ui.SelectPhase(i);
            Assert.AreEqual(ui.Timeline.Keyframes[i].Scene, illustration.CurrentScene, ui.Timeline.Keyframes[i].PhaseName);
            seen[ui.Timeline.Keyframes[i].Scene] = illustration.CurrentCaption;
        }

        Assert.HasCount(11, seen, "11가지 장면이 모두 나타난다");
        Assert.AreEqual(11, seen.Values.Distinct().Count(), "장면마다 다른 설명 문구");
    }

    [STATestMethod]
    [TestCategory("UI-S16")]
    public void PhaseDescriptions_AreFilledForEveryPhase()
    {
        using var ui = new UiDriver();

        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            ui.SelectPhase(i);
            Assert.IsFalse(string.IsNullOrWhiteSpace(ui.PhaseDescription), ui.Timeline.Keyframes[i].PhaseName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(ui.AltitudeText));
            Assert.IsFalse(string.IsNullOrWhiteSpace(ui.PositionText));
        }
    }
}
