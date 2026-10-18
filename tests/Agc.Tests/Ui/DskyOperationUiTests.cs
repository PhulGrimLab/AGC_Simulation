using Agc.Tests.Harness;

namespace Agc.Tests.Ui;

/// <summary>
/// 기능: 창에서 마우스로 DSKY를 조작한 결과(UI-D 시나리오). 키패드 버튼 클릭 → 컨트롤러 → 화면(세그먼트/램프/깜박임)까지의 종단 흐름.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class DskyOperationUiTests
{
    private static int Val(string register)
    {
        return int.Parse(register.Trim());
    }

    [STATestMethod]
    [TestCategory("UI-D01")]
    public void InitialScreen_ShowsLaunchProgramAndBlankDisplay()
    {
        using var ui = new UiDriver();

        Assert.AreEqual("11", ui.Prog, "발사 시점의 프로그램은 P11");
        Assert.AreEqual("  ", ui.Verb);
        Assert.AreEqual("  ", ui.Noun);
        Assert.AreEqual("      ", ui.R1);
        Assert.AreEqual("      ", ui.R2);
        Assert.AreEqual("      ", ui.R3);
        Assert.IsEmpty(ui.LitLamps, "처음에는 켜진 램프가 없다");
        Assert.IsTrue(ui.VerbNounVisible);
        ui.AssertDisplayWellFormed();
    }

    [STATestMethod]
    [TestCategory("UI-D02")]
    public void LampTest_V35E_LightsEverythingFlashesThenLeaves88s()
    {
        using var ui = new UiDriver();

        ui.Press("V35E");

        Assert.HasCount(UiDriver.AllLampLabels.Length, ui.LitLamps, "램프 13개 모두 점등");
        Assert.AreEqual("88", ui.Verb);
        Assert.AreEqual("88", ui.Noun);
        Assert.AreEqual("+88888", ui.R1);
        Assert.AreEqual("+88888", ui.R2);
        Assert.AreEqual("+88888", ui.R3);
        Assert.IsTrue(ui.VerbNounVisible);
        ui.AdvanceFlash();
        Assert.IsFalse(ui.VerbNounVisible, "램프 테스트 중 V/N은 깜박인다");
        ui.AdvanceFlash();
        Assert.IsTrue(ui.VerbNounVisible);

        ui.Run(4.8);
        Assert.HasCount(UiDriver.AllLampLabels.Length, ui.LitLamps, "5초가 지나기 전에는 계속 점등");

        ui.Run(0.4);
        Assert.IsEmpty(ui.LitLamps, "5초 후 램프는 꺼진다");
        Assert.AreEqual("88", ui.Verb, "88 표시는 남는다");
        Assert.AreEqual("+88888", ui.R1);
        ui.AdvanceFlash();
        Assert.IsTrue(ui.VerbNounVisible, "깜박임은 멈춘다");
        Assert.AreEqual("11", ui.Prog, "PROG 창은 계속 프로그램을 따른다");
    }

    [STATestMethod]
    [TestCategory("UI-D03")]
    public void Monitor_V16N36E_UpdatesEverySecondWhileMissionRuns()
    {
        using var ui = new UiDriver();
        ui.Press("V16N36E");
        Assert.AreEqual("+00000", ui.R2, "분");
        Assert.IsTrue(ui.LampLit("COMP ACTY"));

        ui.ClickPlay();
        ui.Run(0.6);
        Assert.AreEqual("+00000", ui.R2, "1초가 지나기 전에는 갱신되지 않는다");

        ui.Run(0.4);
        Assert.AreEqual("+00001", ui.R2, "60배속 1초 = 미션 1분");

        ui.Run(2.0);
        Assert.AreEqual("+00003", ui.R2);
    }

    [STATestMethod]
    [TestCategory("UI-D04")]
    public void DisplayOnce_V06N36E_StaysFrozenWhileMissionRuns()
    {
        using var ui = new UiDriver();
        ui.Press("V06N36E");

        ui.ClickPlay();
        ui.Run(3.0);

        Assert.AreEqual("+00000", ui.R2, "V06은 1회 표시이므로 값이 바뀌지 않는다");
        Assert.IsFalse(ui.LampLit("COMP ACTY"));
        StringAssert.Contains(ui.MetText, "000:03:00", "미션 시간은 흘렀다");
    }

    [STATestMethod]
    [TestCategory("UI-D05")]
    public void OperatorError_LightsOprErrAndRsetClearsIt()
    {
        using var ui = new UiDriver();

        ui.Press("5");
        Assert.IsTrue(ui.LampLit("OPR ERR"), "VERB 없이 숫자");

        ui.Press("R");
        Assert.IsFalse(ui.LampLit("OPR ERR"));
    }

    [STATestMethod]
    [TestCategory("UI-D06")]
    public void Clear_ErasesTheFieldBeingTyped()
    {
        using var ui = new UiDriver();

        ui.Press("V1");
        Assert.AreEqual("1 ", ui.Verb);
        ui.Press("C");

        Assert.AreEqual("  ", ui.Verb);
        Assert.IsFalse(ui.LampLit("OPR ERR"));
    }

    [STATestMethod]
    [TestCategory("UI-D07")]
    public void ProgramChange_V37E00E_ChangesProgWindowAndStopsFlashing()
    {
        using var ui = new UiDriver();

        ui.Press("V37E");
        Assert.AreEqual("37", ui.Verb);
        ui.AdvanceFlash();
        Assert.IsFalse(ui.VerbNounVisible, "프로그램 번호를 기다리는 동안 V/N 깜박임");
        ui.AdvanceFlash();

        ui.Press("00E");

        Assert.AreEqual("00", ui.Prog);
        ui.AdvanceFlash();
        Assert.IsTrue(ui.VerbNounVisible, "입력이 끝나면 깜박임이 멈춘다");
    }

    [STATestMethod]
    [TestCategory("UI-D08")]
    public void ClockLoad_V25N36E_SetsClockThenMonitorShowsLoadedValue()
    {
        using var ui = new UiDriver();

        ui.Press("V25N36E");
        Assert.AreEqual("      ", ui.R1, "로드 대기: 레지스터가 비어 있다");
        ui.AdvanceFlash();
        Assert.IsFalse(ui.VerbNounVisible, "로드 대기 중 V/N 깜박임");
        ui.AdvanceFlash();

        ui.Press("+00102E");
        Assert.AreEqual("+00102", ui.R1);
        ui.Press("+00033E");
        ui.Press("+00500E");

        ui.Press("V16N36E");
        Assert.AreEqual("+00102", ui.R1);
        Assert.AreEqual("+00033", ui.R2);
        Assert.AreEqual("+00500", ui.R3);
        Assert.IsFalse(ui.LampLit("OPR ERR"));
    }

    [STATestMethod]
    [TestCategory("UI-D09")]
    public void Typing_ShowsDigitsProgressivelyInVerbAndNounWindows()
    {
        using var ui = new UiDriver();

        ui.Press("V1");
        Assert.AreEqual("1 ", ui.Verb);
        ui.Press("6");
        Assert.AreEqual("16", ui.Verb);
        ui.Press("N6");
        Assert.AreEqual("6 ", ui.Noun);
        ui.Press("3");
        Assert.AreEqual("63", ui.Noun);
    }

    [STATestMethod]
    [TestCategory("UI-D10")]
    public void KeyRelease_BlanksDisplayAndStopsMonitor()
    {
        using var ui = new UiDriver();
        ui.Press("V16N36E");
        Assert.IsTrue(ui.LampLit("COMP ACTY"));

        ui.Press("K");

        Assert.AreEqual("      ", ui.R1);
        Assert.AreEqual("  ", ui.Verb);
        Assert.AreEqual("  ", ui.Noun);
        Assert.IsFalse(ui.LampLit("COMP ACTY"));
    }

    [STATestMethod]
    [TestCategory("UI-D11")]
    public void UnsupportedCommands_LightOprErr()
    {
        using var ui = new UiDriver();

        foreach (var command in new[] { "V99E", "V06N62E", "V06N99E", "V16E", "V05N36E", "V21N36E", "V25N43E", "E" })
        {
            ui.Press("R");
            ui.Press(command);
            Assert.IsTrue(ui.LampLit("OPR ERR"), command);
        }
    }

    [STATestMethod]
    [TestCategory("UI-D12")]
    public void LoadCanBeCancelledWithV34()
    {
        using var ui = new UiDriver();
        ui.Press("V25N36E+001");

        ui.Press("V34E");

        ui.AdvanceFlash();
        Assert.IsTrue(ui.VerbNounVisible, "깜박임이 멈춘다");
        Assert.IsFalse(ui.LampLit("OPR ERR"), "V34는 정상 응답이다");
    }

    [STATestMethod]
    [TestCategory("UI-D13")]
    public void LoadCanBeEndedWithV33AndLeavesClockUnchanged()
    {
        using var ui = new UiDriver();
        ui.Press("V25N36E+00999E");

        ui.Press("V33E");
        ui.Press("V06N36E");

        Assert.IsFalse(ui.LampLit("OPR ERR"));
        Assert.AreEqual("+00000", ui.R1, "일부만 입력했으므로 시계는 바뀌지 않는다");
    }

    [STATestMethod]
    [TestCategory("UI-D14")]
    public void Load_RejectsBadValuesAndRestartsFromTheFirstRegister()
    {
        using var ui = new UiDriver();

        ui.Press("V25N36E+00001E+00060E+00000E");

        Assert.IsTrue(ui.LampLit("OPR ERR"), "분은 0~59");
        Assert.AreEqual("      ", ui.R1, "처음부터 다시 입력받는다");
        ui.Press("R");
        ui.Press("+00001E+00059E+00000E");
        Assert.IsFalse(ui.LampLit("OPR ERR"));
        ui.Press("V06N36E");
        Assert.AreEqual("+00001", ui.R1);
        Assert.AreEqual("+00059", ui.R2);
    }

    [STATestMethod]
    [TestCategory("UI-D15")]
    public void PositionNoun_V06N43E_ShowsTranquilityBaseCoordinates()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 45, 40);

        ui.Press("V06N43E");

        Assert.AreEqual("68", ui.Prog, "착륙 확인 프로그램");
        Assert.AreEqual("+00067", ui.R1, "위도 0.67°");
        Assert.AreEqual("+02347", ui.R2, "경도 23.47°");
        Assert.AreEqual("+00000", ui.R3, "고도 0");
    }

    [STATestMethod]
    [TestCategory("UI-D16")]
    public void DescentVelocityNoun_AtPdi_ShowsOrbitalEntrySpeedAndAltitude()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 33, 5);

        ui.Press("V06N63E");

        Assert.IsGreaterThan(55000, Val(ui.R1), "속도 약 5,577 ft/s ×10");
        Assert.IsLessThan(56500, Val(ui.R1));
        Assert.IsGreaterThan(49000, Val(ui.R3), "고도 약 50,000 ft");
        Assert.IsLessThan(50500, Val(ui.R3));
        Assert.AreEqual('-', ui.R2[0], "하강 중이므로 고도변화율은 음수");
    }

    [STATestMethod]
    [TestCategory("UI-D17")]
    public void RangeNoun_V06N68E_ShowsRangeAndBrakingTimeAtPdi()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 33, 5);

        ui.Press("V06N68E");

        Assert.IsGreaterThan(2400, Val(ui.R1), "사거리 약 253 nm ×10");
        Assert.IsLessThan(2700, Val(ui.R1));
        Assert.AreEqual("+08 27", ui.R2, "제동 단계 잔여 8분 27초");
        Assert.AreEqual("+00000", ui.R3);
    }

    [STATestMethod]
    [TestCategory("UI-D18")]
    public void EveryCommandKeepsTheDisplayWellFormed()
    {
        using var ui = new UiDriver();
        ui.SeekTo(102, 40, 0);

        foreach (var command in new[]
        {
            "V35E", "K", "V16N36E", "V16N43E", "V16N63E", "V06N68E", "V05N09E", "V25N36E", "+00001E+00002E+00003E",
            "V37E00E", "V34E", "V33E", "K", "R", "V", "N", "E", "99", "V3", "C", "+", "-",
        })
        {
            ui.Press(command);
            ui.AssertDisplayWellFormed();
            ui.Run(0.2);
            ui.AssertDisplayWellFormed();
        }
    }
}
