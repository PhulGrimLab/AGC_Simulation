using Agc.Tests.Harness;

namespace Agc.Tests.Dsky;

/// <summary>기능: V37 프로그램 변경, V35 세부 동작, V16 갱신 주기, 프로그램 표시 요청과 KEY REL 램프.</summary>
[TestClass]
public sealed class DskyProgramAndDisplayRequestTests
{
    private static readonly TimeSpan HalfSecond = TimeSpan.FromSeconds(0.5);

    private FakeTelemetry _telemetry = null!;
    private FakeProgramControl _programs = null!;
    private ListLog _log = null!;
    private DskyController _dsky = null!;

    [TestInitialize]
    public void Setup()
    {
        _telemetry = new FakeTelemetry { Current = FakeTelemetry.Make(met: new TimeSpan(1, 2, 3), program: 11) };
        _programs = new FakeProgramControl();
        _log = new ListLog();
        _dsky = new DskyController(_telemetry, _log, _programs);
    }

    private bool OprErr => _dsky.Display.Lamps.HasFlag(DskyLamps.OprErr);

    // ---- V37 ----

    [TestMethod]
    public void V37_AwaitsProgramNumber_FlashingAndEchoesDigitsInNounWindow()
    {
        _dsky.Press("V37E");
        Assert.IsTrue(_dsky.Display.VerbNounFlashing);

        _dsky.Press("6");

        Assert.AreEqual("37", _dsky.Display.Verb);
        Assert.AreEqual("6 ", _dsky.Display.Noun);
    }

    [TestMethod]
    public void V37_ProgramNumberEntr_RequestsProgramAndStopsFlashing()
    {
        _dsky.Press("V37E63E");

        CollectionAssert.AreEqual(new[] { 63 }, _programs.Requested);
        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
        Assert.IsFalse(OprErr);
        Assert.IsTrue(_log.Infos.Any(i => i.Contains("V37 P63")));
    }

    [TestMethod]
    public void V37_RejectedProgram_LightsOprErr()
    {
        _programs.Accept = false;

        _dsky.Press("V37E99E");

        Assert.IsTrue(OprErr);
        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
    }

    [TestMethod]
    public void V37_WithoutProgramControl_LightsOprErr()
    {
        var dsky = new DskyController(_telemetry, _log);

        dsky.Press("V37E");

        Assert.IsTrue(dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
        Assert.IsFalse(dsky.Display.VerbNounFlashing);
    }

    [TestMethod]
    public void V37_EntrWithOneDigit_LightsOprErrAndKeepsWaiting()
    {
        _dsky.Press("V37E6E");

        Assert.IsTrue(OprErr);
        Assert.IsTrue(_dsky.Display.VerbNounFlashing);
        Assert.IsEmpty(_programs.Requested);
    }

    [TestMethod]
    public void V37_NounKey_AbortsProgramEntry()
    {
        _dsky.Press("V37EN");

        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
    }

    // ---- V35 / V16 ----

    [TestMethod]
    public void V35_FlashesVerbNounDuringTest_AndLeaves88sAfterwards()
    {
        _dsky.Press("V35E");
        Assert.IsTrue(_dsky.Display.VerbNounFlashing);

        _dsky.Update(DskyController.LampTestDuration + TimeSpan.FromMilliseconds(1));

        var d = _dsky.Display;
        Assert.IsFalse(d.VerbNounFlashing, "깜박임은 멈춘다");
        Assert.AreEqual(DskyLamps.None, d.Lamps);
        Assert.AreEqual("88", d.Verb);
        Assert.AreEqual("88", d.Noun);
        Assert.AreEqual("+88888", d.R1, "숫자는 남는다");
        Assert.AreEqual("11", d.Prog, "PROG 창은 계속 텔레메트리를 따른다");
    }

    [TestMethod]
    public void V35Residue_IsClearedByKeyRelease()
    {
        _dsky.Press("V35E");
        _dsky.Update(DskyController.LampTestDuration + TimeSpan.FromMilliseconds(1));

        _dsky.Press("K");

        Assert.AreEqual("      ", _dsky.Display.R1);
        Assert.AreEqual("  ", _dsky.Display.Verb);
    }

    [TestMethod]
    public void V16Monitor_RefreshesOncePerSecond()
    {
        _dsky.Press("V16N36E");
        Assert.AreEqual("+00001", _dsky.Display.R1);

        _telemetry.Current = FakeTelemetry.Make(met: new TimeSpan(5, 6, 7), program: 11);
        _dsky.Update(HalfSecond);
        Assert.AreEqual("+00001", _dsky.Display.R1, "0.5초 후에는 아직 갱신되지 않는다");

        _dsky.Update(HalfSecond);
        Assert.AreEqual("+00005", _dsky.Display.R1, "1초가 지나면 갱신된다");
    }

    [TestMethod]
    public void NormalDisplay_DoesNotFlash()
    {
        _dsky.Press("V16N36E");

        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
    }

    // ---- 프로그램 표시 요청 / KEY REL ----

    [TestMethod]
    public void RequestDisplay_WhenIdle_ShowsImmediatelyAndFlashes()
    {
        _dsky.RequestDisplay(6, 36, flash: true);

        var d = _dsky.Display;
        Assert.AreEqual("06", d.Verb);
        Assert.AreEqual("36", d.Noun);
        Assert.AreEqual("+00001", d.R1);
        Assert.IsTrue(d.VerbNounFlashing);
        Assert.IsFalse(d.Lamps.HasFlag(DskyLamps.KeyRel));
    }

    [TestMethod]
    public void Pro_AcknowledgesFlashingRequest_KeepsDisplay()
    {
        _dsky.RequestDisplay(6, 36, flash: true);

        _dsky.Press("P");

        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
        Assert.AreEqual("+00001", _dsky.Display.R1);
    }

    [TestMethod]
    public void RequestDisplay_WhileCrewTyping_LightsKeyRelAndWaits()
    {
        _dsky.Press("V1");

        _dsky.RequestDisplay(6, 36, flash: false);

        var d = _dsky.Display;
        Assert.IsTrue(d.Lamps.HasFlag(DskyLamps.KeyRel));
        Assert.AreEqual("1 ", d.Verb, "승무원의 입력은 그대로");
        Assert.AreEqual("      ", d.R1);
    }

    [TestMethod]
    public void RequestDisplay_WhileCrewMonitoring_LightsKeyRelAndWaits()
    {
        _dsky.Press("V16N43E");

        _dsky.RequestDisplay(6, 36, flash: false);

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.KeyRel));
        Assert.AreEqual("43", _dsky.Display.Noun);
    }

    [TestMethod]
    public void KeyRelease_ShowsPendingRequestAndTurnsLampOff()
    {
        _dsky.Press("V16N43E");
        _dsky.RequestDisplay(6, 36, flash: true);

        _dsky.Press("K");

        var d = _dsky.Display;
        Assert.IsFalse(d.Lamps.HasFlag(DskyLamps.KeyRel));
        Assert.AreEqual("06", d.Verb);
        Assert.AreEqual("36", d.Noun);
        Assert.IsTrue(d.VerbNounFlashing);
        Assert.IsFalse(d.Lamps.HasFlag(DskyLamps.CompActy), "V06은 모니터가 아니다");
    }

    [TestMethod]
    public void RequestDisplay_UnsupportedNoun_IsIgnoredAndLogged()
    {
        _dsky.RequestDisplay(6, 99, flash: true);

        Assert.AreEqual("  ", _dsky.Display.Verb);
        Assert.IsTrue(_log.Warnings.Any(w => w.Contains("Ignored display request")));
    }

    [TestMethod]
    public void V34_TerminatesFlashingRequest()
    {
        _dsky.RequestDisplay(6, 36, flash: true);

        _dsky.Press("V34E");

        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
    }
}
