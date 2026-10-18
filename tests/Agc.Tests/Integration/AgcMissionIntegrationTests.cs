using Agc.Tests.Harness;

namespace Agc.Tests.Integration;

/// <summary>기능: 미션 시뮬레이터 ↔ DSKY 연동(PROG 창, Noun 모니터, 알람 전달).</summary>
[TestClass]
public sealed class AgcMissionIntegrationTests
{
    private MissionSimulator _sim = null!;
    private DskyController _dsky = null!;

    [TestInitialize]
    public void Setup()
    {
        _sim = new MissionSimulator(Apollo11Mission.Create(), NullLog.Instance);
        _dsky = new DskyController(_sim, NullLog.Instance);
        _sim.AlarmRaised += alarm => _dsky.RaiseAlarm(alarm.Code);
    }

    [TestMethod]
    public void ProgWindow_FollowsMissionPhase()
    {
        _sim.Seek(TimeSpan.Zero);
        Assert.AreEqual("11", _dsky.Display.Prog);

        _sim.Seek(new TimeSpan(102, 33, 5));
        Assert.AreEqual("63", _dsky.Display.Prog);

        _sim.Seek(new TimeSpan(102, 45, 40));
        Assert.AreEqual("68", _dsky.Display.Prog);
    }

    [TestMethod]
    public void MonitorNoun36_ShowsMissionElapsedTime()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));

        _dsky.Press("V16N36E");

        Assert.AreEqual("+00102", _dsky.Display.R1);
        Assert.AreEqual("+00033", _dsky.Display.R2);
        Assert.AreEqual("+00500", _dsky.Display.R3);
    }

    [TestMethod]
    public void MonitorNoun63_TracksDescentWhileSimulationRuns()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));
        _dsky.Press("V16N63E");
        var before = int.Parse(_dsky.Display.R3);

        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;
        _sim.Advance(TimeSpan.FromSeconds(5));
        _dsky.Update(TimeSpan.FromSeconds(5));
        var after = int.Parse(_dsky.Display.R3);

        Assert.IsLessThan(before, after, "하강 중이므로 계산 고도(ft) 레지스터가 줄어야 한다");
        Assert.AreEqual('-', _dsky.Display.R2[0], "고도변화율은 음수");
    }

    [TestMethod]
    public void MonitorNoun68_ShowsRangeToTranquilityAndBrakingTimeAtPdi()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));

        _dsky.Press("V06N68E");

        var rangeTenthNm = int.Parse(_dsky.Display.R1);
        Assert.IsGreaterThan(2400, rangeTenthNm);
        Assert.IsLessThan(2700, rangeTenthNm);
        Assert.AreEqual("+08 27", _dsky.Display.R2);
    }

    [TestMethod]
    public void DescentAlarm_LightsProgLampAndIsReadableViaV05N09()
    {
        _sim.Seek(new TimeSpan(102, 38, 0));
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;

        _sim.Advance(TimeSpan.FromSeconds(1));
        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.Prog));

        _dsky.Press("V05N09E");
        Assert.AreEqual(" 01202", _dsky.Display.R1);

        _dsky.Press("R");
        Assert.IsFalse(_dsky.Display.Lamps.HasFlag(DskyLamps.Prog));
    }
}
