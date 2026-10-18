using Agc.Tests.Harness;

namespace Agc.Tests.Integration;

/// <summary>
/// 기능: 동력 하강 점화 시퀀스와 알람 재시작의 미션↔DSKY 연결.
/// 점화 약 1분 전 V16 N62 카운트다운 → 5초 전 V99 점멸(허가 요청) → 점화(P63) 후 V16 N63.
/// 1201/1202 알람은 Executive 재시작으로 승무원이 띄운 V16 N68 모니터를 없앤다.
/// </summary>
[TestClass]
public sealed class AgcMissionLinkIgnitionTests
{
    private static readonly TimeSpan Ignition = new(102, 33, 5);

    private MissionSimulator _sim = null!;
    private DskyController _dsky = null!;

    [TestInitialize]
    public void Setup()
    {
        _sim = new MissionSimulator(Apollo11Mission.Create(), NullLog.Instance);
        _dsky = new DskyController(_sim, NullLog.Instance, _sim);
        _ = new AgcMissionLink(_sim, _dsky);
    }

    private (string Verb, string Noun, bool Flashing) Shown()
    {
        var d = _dsky.Display;
        return (d.Verb, d.Noun, d.VerbNounFlashing);
    }

    [TestMethod]
    public void Data_HasTheCountdownAndTheFlashingEnablePrompt()
    {
        var requests = _sim.Timeline.DisplayRequests;

        Assert.IsTrue(requests.Any(r => r.Verb == 16 && r.Noun == 62 && r.Met < Ignition && r.Met > Ignition - TimeSpan.FromMinutes(3)), "점화 전 카운트다운");
        var enable = requests.Single(r => r.Verb == 99);
        Assert.IsTrue(enable.Flash);
        Assert.AreEqual(Ignition - TimeSpan.FromSeconds(5), enable.Met, "점화 5초 전");
        Assert.IsTrue(requests.Any(r => r.Met == Ignition && r.Verb == 16 && r.Noun == 63), "점화 후 N63");
    }

    [TestMethod]
    public void Playback_ThroughIgnition_ShowsCountdownThenFlashingEnableThenN63()
    {
        _sim.Seek(Ignition - TimeSpan.FromSeconds(100));
        _dsky.PressKey(DskyKey.KeyRelease);
        _sim.SpeedMultiplier = 1;
        _sim.IsRunning = true;
        var sequence = new List<(string Verb, string Noun, bool Flashing)>();

        for (var i = 0; i < 110; i++)
        {
            _sim.Advance(TimeSpan.FromSeconds(1));
            var shown = Shown();
            if (sequence.Count == 0 || sequence[^1] != shown)
            {
                sequence.Add(shown);
            }
        }

        CollectionAssert.AreEqual(
            new[] { ("  ", "  ", false), ("16", "62", false), ("99", "62", true), ("16", "63", false) },
            sequence);
    }

    [TestMethod]
    public void Countdown_ShowsTheTimeToIgnitionInTheMiddleRegister()
    {
        _sim.Seek(Ignition - TimeSpan.FromSeconds(40));
        _dsky.PressKey(DskyKey.KeyRelease);
        _dsky.Press("V16N62E");

        Assert.AreEqual("+00 40", _dsky.Display.R2);

        _sim.Seek(Ignition - TimeSpan.FromSeconds(20));
        _dsky.Update(TimeSpan.FromSeconds(1));
        Assert.AreEqual("+00 20", _dsky.Display.R2);
    }

    [TestMethod]
    public void PressingPro_AtTheEnablePrompt_AcknowledgesIt()
    {
        _sim.Seek(Ignition - TimeSpan.FromSeconds(100));
        _dsky.PressKey(DskyKey.KeyRelease);
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 1;
        _sim.Advance(TimeSpan.FromSeconds(96));
        Assert.IsTrue(_dsky.Display.VerbNounFlashing, "T-4초: 허가 요청이 점멸 중");

        _dsky.PressKey(DskyKey.Proceed);

        Assert.IsFalse(_dsky.Display.VerbNounFlashing);
        _sim.Advance(TimeSpan.FromSeconds(5));
        Assert.AreEqual("63", _dsky.Display.Noun, "점화 후 N63");
        Assert.AreEqual("63", _dsky.Display.Prog);
    }

    [TestMethod]
    public void SeekInsideTheSamePhase_DoesNotChangeTheDisplay()
    {
        _sim.Seek(new TimeSpan(100, 12, 0));
        _dsky.PressKey(DskyKey.KeyRelease);

        _sim.Seek(Ignition - TimeSpan.FromSeconds(3));

        Assert.AreEqual(("  ", "  ", false), Shown(), "같은 단계 안의 이동은 표시를 재현하지 않는다");
    }

    [TestMethod]
    public void SeekIntoThePhaseFromAnotherPhase_ShowsTheLatestRequestAtThatTime()
    {
        _sim.Seek(TimeSpan.Zero);
        _dsky.PressKey(DskyKey.KeyRelease);

        _sim.Seek(Ignition - TimeSpan.FromSeconds(3));

        Assert.AreEqual(("99", "62", true), Shown(), "다른 단계에서 이동해 오면 그 시각의 최신 요청이 나타난다");
    }

    [TestMethod]
    public void SeekEarlyInThePhase_ShowsNothingBecauseNoRequestHasHappenedYet()
    {
        _sim.Seek(TimeSpan.Zero);
        _dsky.PressKey(DskyKey.KeyRelease);

        _sim.Seek(new TimeSpan(100, 30, 0));

        Assert.AreEqual(("  ", "  ", false), Shown());
    }

    // ---- 알람과 재시작 ----

    [TestMethod]
    public void Alarm1202_DuringBraking_DropsTheCrewN68MonitorAndReturnsToN63()
    {
        _sim.Seek(Ignition);
        _dsky.Press("V16N68E");
        Assert.AreEqual("68", _dsky.Display.Noun);
        _sim.SpeedMultiplier = 60;
        _sim.IsRunning = true;

        _sim.Advance(TimeSpan.FromSeconds(0.5));
        Assert.AreEqual("68", _dsky.Display.Noun, "아직 알람 전(102:33:35)");
        _sim.Seek(new TimeSpan(102, 38, 0));
        _dsky.Press("V16N68E");
        _sim.Advance(TimeSpan.FromSeconds(1));

        var d = _dsky.Display;
        Assert.IsTrue(d.Lamps.HasFlag(DskyLamps.Prog), "PROG 램프");
        Assert.AreEqual("63", d.Noun, "재시작 후 프로그램의 N63으로 돌아온다");
        Assert.AreEqual("16", d.Verb);
    }

    [TestMethod]
    public void SecondAlarm_DropsTheMonitorKeyedAgain()
    {
        _sim.Seek(new TimeSpan(102, 38, 0));
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;
        _sim.Advance(TimeSpan.FromSeconds(1));
        _dsky.Press("V16N68E");
        Assert.AreEqual("68", _dsky.Display.Noun);

        _sim.Advance(TimeSpan.FromSeconds(1.5));

        Assert.AreEqual("63", _dsky.Display.Noun, "두 번째 1202(102:39:02)도 같은 방식으로 모니터를 없앤다");
    }

    [TestMethod]
    public void Alarm1201_AlsoRestarts()
    {
        _sim.Seek(new TimeSpan(102, 42, 10));
        _dsky.Press("V16N68E");
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;

        _sim.Advance(TimeSpan.FromSeconds(0.5));

        Assert.AreNotEqual("68", _dsky.Display.Noun, "1201(102:42:17)도 BAILOUT 재시작");
    }

    [TestMethod]
    public void StaticCrewDisplay_SurvivesTheAlarmRestart()
    {
        _sim.Seek(new TimeSpan(102, 38, 0));
        _dsky.Press("V06N43E");
        _sim.IsRunning = true;
        _sim.SpeedMultiplier = 60;

        _sim.Advance(TimeSpan.FromSeconds(1));

        Assert.AreEqual("43", _dsky.Display.Noun, "V06 1회 표시는 모니터가 아니므로 유지된다");
    }
}
