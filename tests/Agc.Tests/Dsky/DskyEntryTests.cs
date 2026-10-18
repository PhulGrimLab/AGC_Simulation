using Agc.Tests.Harness;

namespace Agc.Tests.Dsky;

/// <summary>기능: VERB/NOUN 키 입력 상태기계, CLR/RSET, OPR ERR.</summary>
[TestClass]
public sealed class DskyEntryTests
{
    private FakeTelemetry _telemetry = null!;
    private ListLog _log = null!;
    private DskyController _dsky = null!;

    [TestInitialize]
    public void Setup()
    {
        _telemetry = new FakeTelemetry { Current = FakeTelemetry.Make(program: 63) };
        _log = new ListLog();
        _dsky = new DskyController(_telemetry, _log);
    }

    [TestMethod]
    public void InitialDisplay_ShowsProgramFromTelemetryAndBlanksElsewhere()
    {
        var d = _dsky.Display;

        Assert.AreEqual("63", d.Prog);
        Assert.AreEqual("  ", d.Verb);
        Assert.AreEqual("  ", d.Noun);
        Assert.AreEqual("      ", d.R1);
        Assert.AreEqual(DskyLamps.None, d.Lamps);
    }

    [TestMethod]
    public void ProgramWindow_FollowsTelemetry()
    {
        _telemetry.Current = FakeTelemetry.Make(program: 12);

        Assert.AreEqual("12", _dsky.Display.Prog);
    }

    [TestMethod]
    public void VerbEntry_ShowsDigitsProgressively()
    {
        _dsky.Press("V1");
        Assert.AreEqual("1 ", _dsky.Display.Verb);

        _dsky.Press("6");
        Assert.AreEqual("16", _dsky.Display.Verb);
    }

    [TestMethod]
    public void NounEntry_ShowsDigits()
    {
        _dsky.Press("N62");

        Assert.AreEqual("62", _dsky.Display.Noun);
    }

    [TestMethod]
    public void PressingVerbAgain_ClearsVerbField()
    {
        _dsky.Press("V16V");

        Assert.AreEqual("  ", _dsky.Display.Verb);
    }

    [TestMethod]
    public void Clear_DuringEntry_ClearsActiveField()
    {
        _dsky.Press("V1C");

        Assert.AreEqual("  ", _dsky.Display.Verb);
        Assert.AreEqual(DskyLamps.None, _dsky.Display.Lamps);
    }

    [TestMethod]
    public void Digit_WithoutVerbOrNoun_LightsOprErr()
    {
        _dsky.Press("5");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
        Assert.AreEqual(1, _log.Warnings.Count(w => w.Contains("OPR ERR")));
    }

    [TestMethod]
    public void ThirdDigit_AfterFullField_LightsOprErr()
    {
        _dsky.Press("V163");

        Assert.AreEqual("16", _dsky.Display.Verb);
        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    [TestMethod]
    public void Enter_WithoutCompleteVerb_LightsOprErr()
    {
        _dsky.Press("V1E");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    [TestMethod]
    public void SignKey_WithoutDataEntry_LightsOprErr()
    {
        _dsky.Press("+");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    [TestMethod]
    public void Reset_ClearsOprErr()
    {
        _dsky.Press("5R");

        Assert.AreEqual(DskyLamps.None, _dsky.Display.Lamps);
    }

    [TestMethod]
    public void Proceed_IsNoOp()
    {
        _dsky.Press("P");

        Assert.AreEqual(DskyLamps.None, _dsky.Display.Lamps);
    }
}
