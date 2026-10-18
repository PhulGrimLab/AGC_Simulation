using Agc.Tests.Harness;

namespace Agc.Tests.Dsky;

/// <summary>기능: 데이터 로드 Verb(V21~V25)와 AGC 시계(N36) 설정, V33/V34.</summary>
[TestClass]
public sealed class DskyDataLoadTests
{
    private FakeTelemetry _telemetry = null!;
    private ListLog _log = null!;
    private DskyController _dsky = null!;

    [TestInitialize]
    public void Setup()
    {
        _telemetry = new FakeTelemetry { Current = FakeTelemetry.Make(program: 0) };
        _log = new ListLog();
        _dsky = new DskyController(_telemetry, _log);
    }

    private bool OprErr => _dsky.Display.Lamps.HasFlag(DskyLamps.OprErr);

    [TestMethod]
    public void V25N36_StartsLoad_FlashesAndBlanksRegisters()
    {
        _dsky.Press("V25N36E");

        var d = _dsky.Display;
        Assert.IsTrue(d.VerbNounFlashing);
        Assert.AreEqual("25", d.Verb);
        Assert.AreEqual("36", d.Noun);
        Assert.AreEqual("      ", d.R1);
        Assert.AreEqual("      ", d.R2);
        Assert.AreEqual("      ", d.R3);
        Assert.IsFalse(OprErr);
    }

    [TestMethod]
    public void Load_ShowsTypedCharactersProgressively()
    {
        _dsky.Press("V25N36E+00");

        Assert.AreEqual("+00   ", _dsky.Display.R1);
    }

    [TestMethod]
    public void Load_AllThreeRegisters_SetsAgcClock()
    {
        _dsky.Press("V25N36E");
        _dsky.Press("+00102E");
        Assert.IsTrue(_dsky.Display.VerbNounFlashing, "아직 R2, R3 입력이 남아 있다");
        _dsky.Press("+00033E");
        _dsky.Press("+00500E");

        var d = _dsky.Display;
        Assert.IsFalse(d.VerbNounFlashing);
        Assert.AreEqual("+00102", d.R1);
        Assert.AreEqual("+00033", d.R2);
        Assert.AreEqual("+00500", d.R3);
        Assert.IsFalse(OprErr);
        Assert.IsTrue(_log.Infos.Any(i => i.Contains("AGC clock set")));
    }

    [TestMethod]
    public void LoadedClock_KeepsRunningWithMissionTime()
    {
        _dsky.Press("V25N36E+00102E+00033E+00500E");

        _telemetry.Current = FakeTelemetry.Make(met: TimeSpan.FromSeconds(10));
        _dsky.Press("V16N36E");

        Assert.AreEqual("+00102", _dsky.Display.R1);
        Assert.AreEqual("+00033", _dsky.Display.R2);
        Assert.AreEqual("+01500", _dsky.Display.R3, "5.00초 + 경과 10초 = 15.00초");
    }

    [TestMethod]
    public void Load_NegativeSign_IsAcceptedAsDataButRejectedAsClockValue()
    {
        _dsky.Press("V25N36E-00001E+00000E+00000E");

        Assert.IsTrue(OprErr, "시계 시간은 음수일 수 없다");
        Assert.AreEqual("      ", _dsky.Display.R1, "처음부터 다시 입력받는다");
        Assert.IsTrue(_dsky.Display.VerbNounFlashing);
    }

    [TestMethod]
    public void Load_MinutesOutOfRange_LightsOprErrAndRestarts()
    {
        _dsky.Press("V25N36E+00001E+00060E+00000E");

        Assert.IsTrue(OprErr);
        Assert.AreEqual("      ", _dsky.Display.R2);
        Assert.IsTrue(_dsky.Display.VerbNounFlashing);
    }

    [TestMethod]
    public void Load_DigitWithoutSign_LightsOprErr()
    {
        _dsky.Press("V25N36E1");

        Assert.IsTrue(OprErr);
        Assert.AreEqual("      ", _dsky.Display.R1);
    }

    [TestMethod]
    public void Load_SignWhileTyping_LightsOprErr()
    {
        _dsky.Press("V25N36E+0+");

        Assert.IsTrue(OprErr);
    }

    [TestMethod]
    public void Load_SixthDigit_LightsOprErr()
    {
        _dsky.Press("V25N36E+000001");

        Assert.IsTrue(OprErr);
        Assert.AreEqual("+00000", _dsky.Display.R1);
    }

    [TestMethod]
    public void Load_EnterWithIncompleteData_LightsOprErrAndKeepsWaiting()
    {
        _dsky.Press("V25N36E+001E");

        Assert.IsTrue(OprErr);
        Assert.IsTrue(_dsky.Display.VerbNounFlashing);
    }

    [TestMethod]
    public void Load_Clear_ErasesCurrentRegisterEntry()
    {
        _dsky.Press("V25N36E+001C");

        Assert.AreEqual("      ", _dsky.Display.R1);
        Assert.IsFalse(OprErr);
    }

    [TestMethod]
    public void V21N36_ClockNounRequiresLoadingAllThreeRegisters()
    {
        _dsky.Press("V21N36E");

        Assert.IsTrue(OprErr);
        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
    }

    [TestMethod]
    public void V25N43_NonLoadableNoun_LightsOprErr()
    {
        _dsky.Press("V25N43E");

        Assert.IsTrue(OprErr);
    }

    [TestMethod]
    public void V34_TerminatesLoadWithoutChangingClock()
    {
        _telemetry.Current = FakeTelemetry.Make(met: new TimeSpan(1, 2, 3));
        _dsky.Press("V25N36E+00999E");

        _dsky.Press("V34E");

        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
        _dsky.Press("V06N36E");
        Assert.AreEqual("+00001", _dsky.Display.R1, "시계는 바뀌지 않았다");
    }

    [TestMethod]
    public void V33_ProceedWithoutData_EndsLoadAndKeepsClock()
    {
        _dsky.Press("V25N36E");

        _dsky.Press("V33E");

        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
        Assert.IsFalse(OprErr);
    }

    [TestMethod]
    public void VerbKey_DuringLoad_AbortsTheLoad()
    {
        _dsky.Press("V25N36E+001V");

        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
    }

    [TestMethod]
    public void ProKey_DuringLoad_IsIgnored()
    {
        _dsky.Press("V25N36EP");

        Assert.IsTrue(_dsky.Display.VerbNounFlashing, "로드 중 PRO는 무시된다(V33E를 써야 한다)");
        Assert.IsFalse(OprErr);
    }
}
