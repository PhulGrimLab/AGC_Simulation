using Agc.Tests.Harness;
using Agc.Wpf.Controls;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 사령선(CMC)/달착륙선(LGC) DSKY 전환과 프로그램의 자동 표시(UI-V 시나리오), 패널의 ALT/VEL 칸(UI-P15/P16).
/// - 사령선 DSKY에는 ALT/VEL 램프가 없고, 착륙선 DSKY에는 있다.
/// - Apollo 11에서 LM 분리(100:12:00)~도킹(128:03:00) 동안은 착륙선 컴퓨터로 본다.
/// - 단계 시작 때 프로그램이 표시를 띄운다: 발사 P11 → V16 N62, 동력 하강 P63 → V16 N63.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class VehicleUiTests
{
    private static string Caption(AgcVehicle vehicle) => vehicle == AgcVehicle.Lm ? "착륙선" : "사령선";

    [STATestMethod]
    [TestCategory("UI-P15")]
    public void Panel_CommandModuleDisplay_HidesAltAndVelLampCells()
    {
        var panel = new DskyPanel();

        panel.Display = new DskyDisplay("11", "  ", "  ", "      ", "      ", "      ", DskyLamps.None, Vehicle: AgcVehicle.Csm);

        var grid = (System.Windows.Controls.Primitives.UniformGrid)panel.FindName("LampGrid");
        foreach (var label in new[] { "ALT", "VEL" })
        {
            var box = grid.Children.OfType<System.Windows.Controls.Border>().Single(b => (b.Child as System.Windows.Controls.TextBlock)?.Text == label);
            Assert.AreEqual(System.Windows.Visibility.Hidden, box.Visibility, label);
        }

        var prog = grid.Children.OfType<System.Windows.Controls.Border>().Single(b => (b.Child as System.Windows.Controls.TextBlock)?.Text == "PROG");
        Assert.AreEqual(System.Windows.Visibility.Visible, prog.Visibility, "다른 램프는 그대로 보인다");
    }

    [STATestMethod]
    [TestCategory("UI-P16")]
    public void Panel_LunarModuleDisplay_ShowsAltAndVelAndSwitchesBack()
    {
        var panel = new DskyPanel();
        var grid = (System.Windows.Controls.Primitives.UniformGrid)panel.FindName("LampGrid");
        System.Windows.Visibility Of(string label) => grid.Children.OfType<System.Windows.Controls.Border>()
            .Single(b => (b.Child as System.Windows.Controls.TextBlock)?.Text == label).Visibility;

        panel.Display = new DskyDisplay("63", "  ", "  ", "      ", "      ", "      ", DskyLamps.Alt, Vehicle: AgcVehicle.Lm);
        Assert.AreEqual(System.Windows.Visibility.Visible, Of("ALT"));
        Assert.AreEqual(System.Windows.Visibility.Visible, Of("VEL"));

        panel.Display = new DskyDisplay("00", "  ", "  ", "      ", "      ", "      ", DskyLamps.None, Vehicle: AgcVehicle.Csm);
        Assert.AreEqual(System.Windows.Visibility.Hidden, Of("ALT"), "다시 사령선이 되면 숨겨진다");
    }

    [STATestMethod]
    [TestCategory("UI-V01")]
    public void Caption_AndAltVelLamps_FollowTheVehicleAtUndockingAndDocking()
    {
        using var ui = new UiDriver();

        ui.SeekTo(100, 11, 59);
        StringAssert.Contains(ui.VehicleText, "사령선");
        StringAssert.Contains(ui.VehicleText, "Colossus 2A");
        Assert.IsFalse(ui.LampInstalled("ALT"));
        Assert.IsFalse(ui.LampInstalled("VEL"));

        ui.SeekTo(100, 12, 0);
        StringAssert.Contains(ui.VehicleText, "착륙선");
        StringAssert.Contains(ui.VehicleText, "Luminary 1A");
        Assert.IsTrue(ui.LampInstalled("ALT"));
        Assert.IsTrue(ui.LampInstalled("VEL"));

        ui.SeekTo(128, 2, 59);
        Assert.IsTrue(ui.LampInstalled("ALT"), "도킹 직전까지 착륙선");
        ui.SeekTo(128, 3, 0);
        StringAssert.Contains(ui.VehicleText, "사령선");
        Assert.IsFalse(ui.LampInstalled("VEL"));
    }

    [STATestMethod]
    [TestCategory("UI-V02")]
    public void EveryPhase_ShowsTheRightComputerCaptionAndLamps()
    {
        using var ui = new UiDriver();

        for (var i = 0; i < ui.Timeline.Keyframes.Count; i++)
        {
            var keyframe = ui.Timeline.Keyframes[i];
            ui.SelectPhase(i);

            StringAssert.Contains(ui.VehicleText, Caption(keyframe.Vehicle), keyframe.PhaseName);
            Assert.AreEqual(keyframe.Vehicle == AgcVehicle.Lm, ui.LampInstalled("ALT"), $"ALT {keyframe.PhaseName}");
            Assert.AreEqual(keyframe.Vehicle == AgcVehicle.Lm, ui.LampInstalled("VEL"), $"VEL {keyframe.PhaseName}");
        }
    }

    [STATestMethod]
    [TestCategory("UI-V03")]
    public void LampTest_LightsAltAndVelOnlyOnTheLunarModule()
    {
        using var ui = new UiDriver();

        ui.SeekTo(102, 33, 5);
        ui.Press("V35E");

        Assert.HasCount(UiDriver.AllLampLabels.Length, ui.LitLamps, "착륙선: 13개 모두 점등");
        Assert.IsTrue(ui.LampLit("ALT"));
        Assert.IsTrue(ui.LampLit("VEL"));
    }

    [STATestMethod]
    [TestCategory("UI-V04")]
    public void PowerDescent_ProgramShowsN63MonitorOnItsOwn_AndPlaybackKeepsItUpdated()
    {
        using var ui = new UiDriver();

        ui.SeekTo(102, 33, 5);

        Assert.AreEqual("63", ui.Prog);
        Assert.AreEqual("16", ui.Verb);
        Assert.AreEqual("63", ui.Noun, "점화 후 프로그램이 N63을 띄운다");
        Assert.IsTrue(ui.LampLit("COMP ACTY"));
        var before = int.Parse(ui.R3.Trim());
        ui.ClickPlay();
        ui.Run(3.0);
        Assert.IsLessThan(before, int.Parse(ui.R3.Trim()), "재생하면 계산 고도가 줄어든다");
    }

    [STATestMethod]
    [TestCategory("UI-V05")]
    public void ProgramDisplay_ReplacesPreviousProgramDisplay_ButNotTheCrewsOwnCommand()
    {
        using var ui = new UiDriver();
        // 발사의 V16 N62(프로그램 표시)에서 PDI로 이동하면 승무원 입력 없이 N63으로 교체된다.
        ui.SeekTo(102, 33, 5);
        Assert.AreEqual("63", ui.Noun);
        Assert.IsFalse(ui.LampLit("KEY REL"));

        // 승무원이 직접 명령한 표시는 보호된다: 다음 단계의 요청은 KEY REL로 알린다.
        ui.SeekTo(0, 0, 0);
        ui.Press("V06N36E");
        ui.SeekTo(102, 33, 5);
        Assert.IsTrue(ui.LampLit("KEY REL"), "승무원이 쓰는 표시가 있으면 KEY REL로 알린다");
        Assert.AreEqual("36", ui.Noun, "승무원의 표시는 그대로");

        ui.Press("K");
        Assert.IsFalse(ui.LampLit("KEY REL"));
        Assert.AreEqual("63", ui.Noun, "KEY REL 후 프로그램의 N63이 나타난다");
    }

    [STATestMethod]
    [TestCategory("UI-V06")]
    public void N62_CommandModuleShowsVelocityAltRateAltitude_LunarModuleShowsIgnitionCountdown()
    {
        using var ui = new UiDriver();

        // 사령선(상승 중): 약 1분 뒤 속도·고도가 올라 있다.
        ui.ClickPlay();
        ui.Run(1.0);
        ui.ClickPlay();
        ui.Run(1.0);
        Assert.AreEqual("62", ui.Noun);
        Assert.IsGreaterThan(0, int.Parse(ui.R1.Trim()), "관성속도");
        Assert.IsGreaterThan(0, int.Parse(ui.R3.Trim()), "고도");

        // 착륙선(점화 3분 5초 전): R2가 점화까지 남은 시간
        ui.SeekTo(102, 30, 0);
        ui.Press("V16N62E");
        Assert.AreEqual("+03 05", ui.R2, "PDI(102:33:05)까지 3분 5초");
        ui.ClickPlay();
        ui.Run(1.0);
        Assert.AreEqual("+02 05", ui.R2, "60배속 1초 = 1분 감소");
    }

    [STATestMethod]
    [TestCategory("UI-V07")]
    public void DescentNouns_AreRejectedOnTheCommandModuleAndAcceptedOnTheLunarModule()
    {
        using var ui = new UiDriver();

        ui.SeekTo(100, 11, 59);
        ui.Press("V06N63E");
        Assert.IsTrue(ui.LampLit("OPR ERR"), "사령선의 N63은 재진입 노운(미지원)");
        ui.Press("R");

        ui.SeekTo(100, 12, 0);
        ui.Press("V06N63E");
        Assert.IsFalse(ui.LampLit("OPR ERR"), "착륙선의 N63은 하강 노운");
        ui.Press("V06N68E");
        Assert.IsFalse(ui.LampLit("OPR ERR"));

        ui.SeekTo(128, 3, 0);
        ui.Press("V06N68E");
        Assert.IsTrue(ui.LampLit("OPR ERR"), "도킹 후 다시 사령선");
    }

    [STATestMethod]
    [TestCategory("UI-V08")]
    public void WholeDescentAndAscent_PlaybackSwitchesComputersExactlyTwice()
    {
        using var ui = new UiDriver();
        ui.SeekTo(100, 10, 0);
        ui.SetSpeed(60);
        ui.ClickPlay();
        var captions = new List<string> { ui.VehicleText };
        var guard = 0;
        while (ui.MetSeconds < new TimeSpan(128, 10, 0).TotalSeconds && guard++ < 20000)
        {
            ui.SetSpeed(600);
            ui.Run(0.2);
            if (ui.VehicleText != captions[^1])
            {
                captions.Add(ui.VehicleText);
            }
        }

        Assert.HasCount(3, captions, "사령선 → 착륙선 → 사령선");
        StringAssert.Contains(captions[0], "사령선");
        StringAssert.Contains(captions[1], "착륙선");
        StringAssert.Contains(captions[2], "사령선");
    }
}
