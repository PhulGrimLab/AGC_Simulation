using Agc.Tests.Harness;

namespace Agc.Tests.Dsky;

/// <summary>
/// 기능: Executive 재시작(BAILOUT)이 DSKY 표시에 미치는 영향과 V99(점화 허가 요청) 표시.
/// 근거(Eyles, Apollo 11 하강): 1201/1202 재시작이 재시작 보호가 없는 V16 N68 모니터를 없앴고 표시가 프로그램의 N63으로 돌아갔다.
/// 점화 5초 전 표시가 점멸하며 허가를 요청했고 Aldrin이 PROCEED를 눌렀다(V99 = PLEASE ENABLE ENGINE).
/// </summary>
[TestClass]
public sealed class DskyExecutiveRestartTests
{
    private FakeTelemetry _telemetry = null!;
    private ListLog _log = null!;
    private DskyController _dsky = null!;

    [TestInitialize]
    public void Setup()
    {
        _telemetry = new FakeTelemetry
        {
            Current = FakeTelemetry.Make(program: 63, speed: 1700, verticalSpeed: -20, altitude: 15000, range: 400000),
        };
        _log = new ListLog();
        _dsky = new DskyController(_telemetry, _log);
    }

    [TestMethod]
    public void Restart_DropsCrewMonitor_AndReturnsToTheProgramDisplay()
    {
        _dsky.RequestDisplay(16, 63, flash: false);
        _dsky.Press("V16N68E");
        Assert.AreEqual("68", _dsky.Display.Noun);

        _dsky.ExecutiveRestart();

        var d = _dsky.Display;
        Assert.AreEqual("16", d.Verb);
        Assert.AreEqual("63", d.Noun, "표시가 프로그램의 N63으로 돌아간다");
        Assert.IsTrue(d.Lamps.HasFlag(DskyLamps.CompActy), "프로그램 모니터는 계속 돈다");
        Assert.IsTrue(_log.Warnings.Any(w => w.Contains("restart", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Restart_WithoutAProgramDisplay_BlanksTheCrewMonitor()
    {
        _dsky.Press("V16N68E");

        _dsky.ExecutiveRestart();

        var d = _dsky.Display;
        Assert.AreEqual("      ", d.R1);
        Assert.AreEqual("  ", d.Verb);
        Assert.AreEqual("  ", d.Noun);
        Assert.IsFalse(d.Lamps.HasFlag(DskyLamps.CompActy));
    }

    [TestMethod]
    public void Restart_AfterwardsTheCrewCanKeyTheMonitorAgain()
    {
        _dsky.RequestDisplay(16, 63, flash: false);
        _dsky.Press("V16N68E");
        _dsky.ExecutiveRestart();

        _dsky.Press("V16N68E");

        Assert.AreEqual("68", _dsky.Display.Noun, "Aldrin이 다시 V16 N68을 눌렀다");
        Assert.IsFalse(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    [TestMethod]
    public void Restart_KeepsStaticCrewDisplaysAndAlarmCodes()
    {
        _dsky.RaiseAlarm(1202);
        _dsky.Press("V05N09E");

        _dsky.ExecutiveRestart();

        Assert.AreEqual("09", _dsky.Display.Noun, "V05 알람 코드 표시는 모니터가 아니다");
        Assert.AreEqual(" 01202", _dsky.Display.R1);
        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.Prog), "PROG 램프는 그대로");
    }

    [TestMethod]
    public void Restart_DoesNotDisturbAnEntryInProgress()
    {
        _dsky.RequestDisplay(16, 63, flash: false);
        _dsky.Press("V16N68E");
        _dsky.Press("V1");

        _dsky.ExecutiveRestart();

        Assert.AreEqual("1 ", _dsky.Display.Verb, "입력 중인 키는 그대로");
    }

    [TestMethod]
    public void Restart_WhenNothingIsShown_IsHarmless()
    {
        _dsky.ExecutiveRestart();

        Assert.AreEqual("  ", _dsky.Display.Verb);
        Assert.IsFalse(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    // ---- V99 점화 허가 요청 ----

    [TestMethod]
    public void EnableEngineRequest_ShowsFlashingV99_AndProAcknowledges()
    {
        _dsky.RequestDisplay(16, 62, flash: false);

        _dsky.RequestDisplay(99, 62, flash: true);

        var d = _dsky.Display;
        Assert.AreEqual("99", d.Verb);
        Assert.AreEqual("62", d.Noun, "카운트다운(N62)이 계속 보인다");
        Assert.IsTrue(d.VerbNounFlashing, "점멸하며 승무원의 허가를 기다린다");

        _dsky.Press("P");

        Assert.IsFalse(_dsky.Display.VerbNounFlashing, "PRO로 허가하면 점멸이 멈춘다");
        Assert.AreEqual("99", _dsky.Display.Verb);
    }

    [TestMethod]
    public void EnableEngineRequest_WithoutFlash_IsIgnoredAndLogged()
    {
        _dsky.RequestDisplay(99, 62, flash: false);

        Assert.AreEqual("  ", _dsky.Display.Verb);
        Assert.IsTrue(_log.Warnings.Any(w => w.Contains("Ignored display request")));
    }

    [TestMethod]
    public void EnableEngineRequest_ReplacesTheCountdownProgramDisplayWithoutKeyRel()
    {
        _dsky.RequestDisplay(16, 62, flash: false);

        _dsky.RequestDisplay(99, 62, flash: true);

        Assert.IsFalse(_dsky.Display.Lamps.HasFlag(DskyLamps.KeyRel));
    }

    [TestMethod]
    public void CrewCannotKeyV99Directly()
    {
        _dsky.Press("V99E");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr), "V99는 프로그램이 요청하는 표시이지 승무원이 입력하는 명령이 아니다");
    }
}
