using Agc.Tests.Harness;

namespace Agc.Tests.Integration;

/// <summary>
/// 기능: 미션 시뮬레이터 ↔ DSKY 연결(AgcMissionLink): 알람 전달과, 프로그램이 단계 시작 때 스스로 띄우는 표시.
/// 발사(P11)에서는 V16 N62(관성속도/고도변화율/고도), 동력 하강(P63)에서는 V16 N63이 자동으로 나타난다.
/// </summary>
[TestClass]
public sealed class AgcMissionLinkTests
{
    private MissionSimulator _sim = null!;
    private DskyController _dsky = null!;

    [TestInitialize]
    public void Setup()
    {
        _sim = new MissionSimulator(Apollo11Mission.Create(), NullLog.Instance);
        _dsky = new DskyController(_sim, NullLog.Instance, _sim);
        _ = new AgcMissionLink(_sim, _dsky);
    }

    [TestMethod]
    public void AtLiftoff_P11ShowsVelocityMonitorOnTheCommandModuleDsky()
    {
        var d = _dsky.Display;

        Assert.AreEqual("11", d.Prog);
        Assert.AreEqual("16", d.Verb);
        Assert.AreEqual("62", d.Noun);
        Assert.AreEqual(AgcVehicle.Csm, d.Vehicle);
        Assert.AreEqual("+00000", d.R1, "속도 0");
        Assert.AreEqual("+01357", d.R2, "상승 중 고도변화율 약 1,357 ft/s (413.6 m/s)");
        Assert.AreEqual("+00000", d.R3, "고도 0");
        Assert.IsTrue(d.Lamps.HasFlag(DskyLamps.CompActy), "V16 모니터 동작");
        Assert.IsFalse(d.VerbNounFlashing);
    }

    [TestMethod]
    public void LaunchMonitor_FollowsAscentWhileMissionRuns()
    {
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 1;

        _sim.Advance(TimeSpan.FromSeconds(60));
        _dsky.Update(TimeSpan.FromSeconds(60));

        Assert.IsGreaterThan(0, int.Parse(_dsky.Display.R3.Trim()), "고도가 올라간다");
        Assert.IsGreaterThan(0, int.Parse(_dsky.Display.R1.Trim()), "속도가 붙는다");
    }

    [TestMethod]
    public void SeekToPowerDescent_ShowsN63MonitorOnTheLunarModuleDsky()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));

        var d = _dsky.Display;
        Assert.AreEqual("63", d.Prog);
        Assert.AreEqual("16", d.Verb);
        Assert.AreEqual("63", d.Noun);
        Assert.AreEqual(AgcVehicle.Lm, d.Vehicle);
        Assert.IsGreaterThan(55000, int.Parse(d.R1.Trim()), "속도 약 5,577 ft/s ×10");
    }

    [TestMethod]
    public void RequestIsAppliedOnlyWhenEnteringThePhase_NotOnSeekWithinIt()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));
        _dsky.PressKey(DskyKey.KeyRelease);
        Assert.AreEqual("      ", _dsky.Display.R1);

        _sim.Seek(new TimeSpan(102, 35, 0));

        Assert.AreEqual("      ", _dsky.Display.R1, "같은 단계 안의 이동은 표시를 다시 띄우지 않는다");
    }

    [TestMethod]
    public void CrossingIgnition_WhilePlaying_ShowsN63WhenCrewIsIdle()
    {
        _sim.Seek(new TimeSpan(102, 32, 0));
        _dsky.PressKey(DskyKey.KeyRelease);
        _sim.SpeedMultiplier = 60;
        _sim.IsRunning = true;

        _sim.Advance(TimeSpan.FromSeconds(1.5));

        Assert.AreEqual("63", _dsky.Display.Prog);
        Assert.AreEqual("63", _dsky.Display.Noun);
    }

    [TestMethod]
    public void CrossingIgnition_WhileCrewIsTyping_LightsKeyRelUntilReleased()
    {
        _sim.Seek(new TimeSpan(102, 32, 0));
        _dsky.PressKey(DskyKey.KeyRelease);
        _dsky.PressKey(DskyKey.Verb);
        _dsky.PressKey(DskyKey.Digit1);
        _sim.SpeedMultiplier = 60;
        _sim.IsRunning = true;

        _sim.Advance(TimeSpan.FromSeconds(1.5));

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.KeyRel), "승무원이 입력 중이므로 KEY REL로 알린다");
        Assert.AreEqual("1 ", _dsky.Display.Verb, "입력 중이던 내용은 그대로");

        _dsky.PressKey(DskyKey.KeyRelease);

        Assert.IsFalse(_dsky.Display.Lamps.HasFlag(DskyLamps.KeyRel));
        Assert.AreEqual("63", _dsky.Display.Noun, "KEY REL 후 대기하던 N63이 나타난다");
    }

    [TestMethod]
    public void Alarms_AreStillForwardedToTheDsky()
    {
        _sim.Seek(new TimeSpan(102, 38, 0));
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;

        _sim.Advance(TimeSpan.FromSeconds(1));

        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.Prog));
    }

    [TestMethod]
    public void ReturningToLowerVehicle_CommandModuleNounsAreUsedAgain()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));
        _sim.Seek(new TimeSpan(128, 3, 0));

        _dsky.PressKey(DskyKey.KeyRelease);
        foreach (var key in new[] { DskyKey.Verb, DskyKey.Digit0, DskyKey.Digit6, DskyKey.Noun, DskyKey.Digit6, DskyKey.Digit3, DskyKey.Enter })
        {
            _dsky.PressKey(key);
        }

        Assert.AreEqual(AgcVehicle.Csm, _dsky.Display.Vehicle, "도킹 후에는 다시 사령선");
        Assert.IsTrue(_dsky.Display.Lamps.HasFlag(DskyLamps.OprErr), "사령선에서 하강 노운 N63은 지원하지 않는다");
    }
}
