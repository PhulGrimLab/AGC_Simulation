using Agc.Tests.Harness;

namespace Agc.Tests.Dsky;

/// <summary>기능: Verb 실행(V35 램프 테스트, V06/V16 Noun 표시, V05 N09 알람) 및 KEY REL.</summary>
[TestClass]
public sealed class DskyCommandTests
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    private FakeTelemetry _telemetry = null!;
    private ListLog _log = null!;
    private DskyController _dsky = null!;

    [TestInitialize]
    public void Setup()
    {
        _telemetry = new FakeTelemetry
        {
            Current = FakeTelemetry.Make(met: new TimeSpan(1, 2, 3), program: 11),
        };
        _log = new ListLog();
        _dsky = new DskyController(_telemetry, _log);
    }

    [TestMethod]
    public void V35_LampTest_LightsEverythingThenEndsAfterFiveSeconds()
    {
        _dsky.Press("V35E");

        var d = _dsky.Display;
        Assert.AreEqual(DskyLamps.All, d.Lamps);
        Assert.AreEqual("88", d.Verb);
        Assert.AreEqual("+88888", d.R2);

        _dsky.Update(TimeSpan.FromSeconds(4));
        Assert.AreEqual(DskyLamps.All, _dsky.Display.Lamps);

        _dsky.Update(TimeSpan.FromSeconds(1.1));
        Assert.AreEqual(DskyLamps.None, _dsky.Display.Lamps);
        Assert.AreEqual("11", _dsky.Display.Prog);
    }

    [TestMethod]
    public void V16N36_Monitor_ShowsClockAndKeepsUpdating()
    {
        _dsky.Press("V16N36E");

        var d = _dsky.Display;
        Assert.AreEqual("+00001", d.R1);
        Assert.AreEqual("+00002", d.R2);
        Assert.AreEqual("+00300", d.R3);
        Assert.IsTrue(d.Lamps.HasFlag(DskyLamps.CompActy));

        _telemetry.Current = FakeTelemetry.Make(met: new TimeSpan(5, 6, 7), program: 11);
        _dsky.Update(OneSecond);

        Assert.AreEqual("+00005", _dsky.Display.R1);
        Assert.AreEqual("+00006", _dsky.Display.R2);
        Assert.AreEqual("+00700", _dsky.Display.R3);
    }

    [TestMethod]
    public void V06N36_DisplayOnce_DoesNotUpdate()
    {
        _dsky.Press("V06N36E");
        _telemetry.Current = FakeTelemetry.Make(met: new TimeSpan(5, 6, 7), program: 11);
        _dsky.Update(OneSecond);

        Assert.AreEqual("+00001", _dsky.Display.R1);
        Assert.IsFalse(_dsky.Display.Lamps.HasFlag(DskyLamps.CompActy));
    }

    [TestMethod]
    public void V16N63_ShowsDescentVelocityRegisters()
    {
        _telemetry.Current = FakeTelemetry.Make(speed: 304.8, verticalSpeed: -30.48, altitude: 1852, program: 63);

        _dsky.Press("V16N63E");

        Assert.AreEqual("+10000", _dsky.Display.R1);
        Assert.AreEqual("-01000", _dsky.Display.R2);
        Assert.AreEqual("+06076", _dsky.Display.R3);
    }

    [TestMethod]
    public void V06N68_ShowsRangeAndTimeToGo()
    {
        _telemetry.Current = FakeTelemetry.Make(range: 18520, timeToNextPhase: TimeSpan.FromSeconds(507), program: 63);

        _dsky.Press("V06N68E");

        Assert.AreEqual("+00100", _dsky.Display.R1);
        Assert.AreEqual("+08 27", _dsky.Display.R2);
    }

    [TestMethod]
    public void UnsupportedNoun62_LightsOprErr()
    {
        _dsky.Press("V06N62E");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    [TestMethod]
    public void UnsupportedVerb_LightsOprErr()
    {
        _dsky.Press("V99E");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    [TestMethod]
    public void UnsupportedNoun_LightsOprErrAndKeepsRegistersBlank()
    {
        _dsky.Press("V06N99E");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
        Assert.AreEqual("      ", _dsky.Display.R1);
    }

    [TestMethod]
    public void MissingNoun_LightsOprErr()
    {
        _dsky.Press("V16E");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    [TestMethod]
    public void V05_WithTelemetryNoun_LightsOprErr()
    {
        _dsky.Press("V05N36E");

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr));
    }

    [TestMethod]
    public void ExecutedCommand_IsLogged()
    {
        _dsky.Press("V16N36E");

        Assert.IsTrue(_log.Infos.Any(i => i.Contains("V16 N36")));
    }

    [TestMethod]
    public void KeyRelease_BlanksDisplayAndStopsMonitoring()
    {
        _dsky.Press("V16N36EK");
        _telemetry.Current = FakeTelemetry.Make(met: new TimeSpan(5, 6, 7), program: 11);
        _dsky.Update(OneSecond);

        var d = _dsky.Display;
        Assert.AreEqual("      ", d.R1);
        Assert.AreEqual("  ", d.Verb);
        Assert.AreEqual("  ", d.Noun);
        Assert.IsFalse(d.Lamps.HasFlag(DskyLamps.CompActy));
    }

    [TestMethod]
    public void RaiseAlarm_LightsProgLampAndRsetClearsIt()
    {
        _dsky.RaiseAlarm(1202);
        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.Prog));
        Assert.IsTrue(_log.Warnings.Any(w => w.Contains("1202")));

        _dsky.Press("R");

        Assert.IsFalse(_dsky.Display.Lamps.HasFlag(DskyLamps.Prog));
    }

    [TestMethod]
    public void V05N09_ShowsAlarmCodesNewestFirst()
    {
        _dsky.RaiseAlarm(1202);
        _dsky.RaiseAlarm(1201);

        _dsky.Press("V05N09E");

        Assert.AreEqual(" 01201", _dsky.Display.R1);
        Assert.AreEqual(" 01202", _dsky.Display.R2);
        Assert.AreEqual("      ", _dsky.Display.R3);
    }

    [TestMethod]
    public void AlarmStore_KeepsOnlyLatestThree()
    {
        foreach (var code in new[] { 1, 2, 3, 4 })
        {
            _dsky.RaiseAlarm(code);
        }

        _dsky.Press("V05N09E");

        Assert.AreEqual(" 00004", _dsky.Display.R1);
        Assert.AreEqual(" 00003", _dsky.Display.R2);
        Assert.AreEqual(" 00002", _dsky.Display.R3);
    }

    [TestMethod]
    public void NewAlarm_WhileAlarmNounShown_RefreshesImmediately()
    {
        _dsky.Press("V05N09E");

        _dsky.RaiseAlarm(1202);

        Assert.AreEqual(" 01202", _dsky.Display.R1);
    }
}
