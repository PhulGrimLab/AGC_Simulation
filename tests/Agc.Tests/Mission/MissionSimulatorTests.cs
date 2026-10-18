using Agc.Tests.Harness;

namespace Agc.Tests.Mission;

/// <summary>기능: 미션 시계 진행, 배속, Seek, 알람/단계 이벤트(MissionSimulator).</summary>
[TestClass]
public sealed class MissionSimulatorTests
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    private MissionSimulator _sim = null!;

    [TestInitialize]
    public void Setup()
    {
        _sim = new MissionSimulator(Apollo11Mission.Create(), NullLog.Instance);
    }

    [TestMethod]
    public void Advance_WhenPaused_DoesNothing()
    {
        _sim.Advance(OneSecond);

        Assert.AreEqual(TimeSpan.Zero, _sim.Met);
    }

    [TestMethod]
    public void Advance_AppliesSpeedMultiplier()
    {
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;

        _sim.Advance(OneSecond);

        Assert.AreEqual(TimeSpan.FromSeconds(60), _sim.Met);
    }

    [TestMethod]
    public void Advance_PastEnd_ClampsAndStops()
    {
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 1e9;

        _sim.Advance(OneSecond);

        Assert.AreEqual(_sim.Timeline.Duration, _sim.Met);
        Assert.IsFalse(_sim.IsRunning);
    }

    [TestMethod]
    public void Seek_ClampsToTimelineRange()
    {
        _sim.Seek(TimeSpan.FromDays(100));
        Assert.AreEqual(_sim.Timeline.Duration, _sim.Met);

        _sim.Seek(TimeSpan.FromHours(-1));
        Assert.AreEqual(TimeSpan.Zero, _sim.Met);
    }

    [TestMethod]
    public void Current_TracksSnapshotTelemetry()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));

        Assert.AreEqual(63, _sim.Current.ProgramNumber);
        Assert.AreEqual(_sim.Met, _sim.Current.MissionElapsedTime);
    }

    [TestMethod]
    public void Advance_CrossingAlarm_RaisesItExactlyOnce()
    {
        var raised = new List<int>();
        _sim.AlarmRaised += a => raised.Add(a.Code);
        _sim.Seek(new TimeSpan(102, 38, 0));
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;

        _sim.Advance(OneSecond);
        _sim.Advance(TimeSpan.FromMilliseconds(10));

        CollectionAssert.AreEqual(new[] { 1202 }, raised);
    }

    [TestMethod]
    public void AlarmHandler_SeesTheMissionTimeAfterTheStep()
    {
        TimeSpan? metSeenByHandler = null;
        _sim.AlarmRaised += _ => metSeenByHandler = _sim.Met;
        _sim.Seek(new TimeSpan(102, 38, 0));
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;

        _sim.Advance(OneSecond);

        Assert.AreEqual(new TimeSpan(102, 39, 0), metSeenByHandler, "알람 핸들러는 이미 갱신된 MET를 보아야 한다");
    }

    [TestMethod]
    public void Seek_PastAlarms_DoesNotRaiseThem()
    {
        var raised = 0;
        _sim.AlarmRaised += _ => raised++;

        _sim.Seek(new TimeSpan(110, 0, 0));

        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void Advance_AcrossSeveralAlarms_RaisesAllInOrder()
    {
        var raised = new List<int>();
        _sim.AlarmRaised += a => raised.Add(a.Code);
        _sim.Seek(new TimeSpan(102, 37, 0));
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 600;

        _sim.Advance(TimeSpan.FromSeconds(10));

        CollectionAssert.AreEqual(new[] { 1202, 1202, 1201, 1202, 1202 }, raised);
    }

    [TestMethod]
    public void PhaseChanged_RaisedOnlyWhenCrossingKeyframe()
    {
        var phases = new List<string>();
        _sim.PhaseChanged += s => phases.Add(s.Phase.PhaseName);
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 1;

        _sim.Advance(TimeSpan.FromSeconds(100));
        Assert.IsEmpty(phases);

        _sim.Advance(TimeSpan.FromSeconds(100));
        Assert.HasCount(1, phases);
        Assert.AreEqual("1단 분리", phases[0]);
    }

    [TestMethod]
    public void TryRequestProgram_KnownProgram_OverridesCurrentProgramNumber()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));

        Assert.IsTrue(_sim.TryRequestProgram(66));

        Assert.AreEqual(66, _sim.Current.ProgramNumber);
        Assert.AreEqual(63, _sim.Snapshot.Phase.ProgramNumber, "단계 자체의 프로그램은 그대로");
    }

    [TestMethod]
    public void TryRequestProgram_P00IsAlwaysAllowed_UnknownProgramRejected()
    {
        Assert.IsTrue(_sim.TryRequestProgram(0));
        Assert.IsFalse(_sim.TryRequestProgram(99));
    }

    [TestMethod]
    public void RequestedProgram_IsReplacedWhenMissionEntersNextPhase()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));
        _sim.TryRequestProgram(0);

        _sim.Seek(new TimeSpan(102, 41, 32));

        Assert.AreEqual(64, _sim.Current.ProgramNumber);
    }

    [TestMethod]
    public void RequestedProgram_StaysWhileInsideSamePhase()
    {
        _sim.Seek(new TimeSpan(102, 33, 5));
        _sim.TryRequestProgram(0);
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;

        _sim.Advance(TimeSpan.FromSeconds(5));

        Assert.AreEqual(0, _sim.Current.ProgramNumber);
    }

    [TestMethod]
    public void PhaseChange_IsLogged()
    {
        var log = new ListLog();
        var sim = new MissionSimulator(Apollo11Mission.Create(), log);

        sim.Seek(new TimeSpan(102, 33, 5));

        Assert.IsTrue(log.Infos.Any(i => i.Contains("P63")));
    }
}
