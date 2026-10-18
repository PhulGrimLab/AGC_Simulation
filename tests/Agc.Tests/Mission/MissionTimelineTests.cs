namespace Agc.Tests.Mission;

/// <summary>기능: 키프레임 보간 타임라인(MissionTimeline).</summary>
[TestClass]
public sealed class MissionTimelineTests
{
    private static MissionKeyframe Frame(int seconds, CelestialBody body, double altitude, double speed, int program = 0)
    {
        return new MissionKeyframe(
            TimeSpan.FromSeconds(seconds), $"phase{seconds}", "desc", program, body, altitude, speed, 0, 0);
    }

    private static MissionTimeline Build(params MissionKeyframe[] frames)
    {
        return new MissionTimeline(frames, []);
    }

    [TestMethod]
    public void Constructor_EmptyKeyframes_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Build());
    }

    [TestMethod]
    public void Constructor_NonIncreasingMet_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => Build(Frame(10, CelestialBody.Earth, 0, 0), Frame(10, CelestialBody.Earth, 1, 1)));
    }

    [TestMethod]
    public void Duration_IsLastKeyframeMet()
    {
        var timeline = Build(Frame(0, CelestialBody.Earth, 0, 0), Frame(100, CelestialBody.Earth, 1000, 100));

        Assert.AreEqual(TimeSpan.FromSeconds(100), timeline.Duration);
    }

    [TestMethod]
    public void Snapshot_OutOfRangeMet_IsClamped()
    {
        var timeline = Build(Frame(0, CelestialBody.Earth, 0, 0), Frame(100, CelestialBody.Earth, 1000, 100));

        Assert.AreEqual(TimeSpan.Zero, timeline.GetSnapshot(TimeSpan.FromSeconds(-5)).Met);
        Assert.AreEqual(TimeSpan.FromSeconds(100), timeline.GetSnapshot(TimeSpan.FromSeconds(500)).Met);
    }

    [TestMethod]
    public void Snapshot_SameBody_InterpolatesLinearly()
    {
        var timeline = Build(Frame(0, CelestialBody.Earth, 0, 0), Frame(100, CelestialBody.Earth, 1000, 100));

        var t = timeline.GetSnapshot(TimeSpan.FromSeconds(50)).Telemetry;

        Assert.AreEqual(500, t.AltitudeMeters, 1e-9);
        Assert.AreEqual(50, t.SpeedMetersPerSecond, 1e-9);
        Assert.AreEqual(10, t.VerticalSpeedMetersPerSecond, 1e-9);
    }

    [TestMethod]
    public void Snapshot_BodyChange_HoldsPreviousValues()
    {
        var timeline = Build(Frame(0, CelestialBody.Earth, 100, 7), Frame(100, CelestialBody.Moon, 5, 1));

        var snapshot = timeline.GetSnapshot(TimeSpan.FromSeconds(50));

        Assert.AreEqual(CelestialBody.Earth, snapshot.Body);
        Assert.AreEqual(100, snapshot.Telemetry.AltitudeMeters, 1e-9);
        Assert.AreEqual(0, snapshot.Telemetry.VerticalSpeedMetersPerSecond, 1e-9);
    }

    [TestMethod]
    public void Snapshot_AtLastKeyframe_UsesItsValuesAndPhaseIndex()
    {
        var timeline = Build(Frame(0, CelestialBody.Earth, 10, 1, program: 11), Frame(100, CelestialBody.Earth, 0, 0, program: 0));

        var snapshot = timeline.GetSnapshot(TimeSpan.FromSeconds(100));

        Assert.AreEqual(1, snapshot.PhaseIndex);
        Assert.AreEqual(0, snapshot.Telemetry.AltitudeMeters, 1e-9);
        Assert.AreEqual(0, snapshot.Telemetry.ProgramNumber);
    }

    [TestMethod]
    public void Snapshot_ProgramNumberFollowsPhase()
    {
        var timeline = Build(
            Frame(0, CelestialBody.Earth, 0, 0, program: 11),
            Frame(60, CelestialBody.Earth, 0, 0, program: 15));

        Assert.AreEqual(11, timeline.GetSnapshot(TimeSpan.FromSeconds(59)).Telemetry.ProgramNumber);
        Assert.AreEqual(15, timeline.GetSnapshot(TimeSpan.FromSeconds(60)).Telemetry.ProgramNumber);
    }

    [TestMethod]
    public void Snapshot_TimeToNextPhase_CountsDownToNextKeyframe()
    {
        var timeline = Build(Frame(0, CelestialBody.Earth, 0, 0), Frame(100, CelestialBody.Earth, 1000, 100));

        Assert.AreEqual(TimeSpan.FromSeconds(70), timeline.GetSnapshot(TimeSpan.FromSeconds(30)).Telemetry.TimeToNextPhase);
        Assert.AreEqual(TimeSpan.Zero, timeline.GetSnapshot(TimeSpan.FromSeconds(100)).Telemetry.TimeToNextPhase);
    }

    [TestMethod]
    public void Snapshot_WithoutTarget_RangeIsZero()
    {
        var timeline = Build(Frame(0, CelestialBody.Moon, 1000, 0));

        Assert.AreEqual(0, timeline.GetSnapshot(TimeSpan.Zero).Telemetry.RangeToTargetMeters);
    }

    [TestMethod]
    public void Snapshot_Range_UsesGreatCircleDistance()
    {
        // 목표와 같은 위도에서 경도 1도 차이 = 달 반지름 x 1도(라디안), 고도 0.
        var frame = new MissionKeyframe(TimeSpan.Zero, "p", "d", 0, CelestialBody.Moon, 0, 0, 0, 1);
        var timeline = new MissionTimeline([frame], [], new MissionTarget(CelestialBody.Moon, 0, 0));

        var range = timeline.GetSnapshot(TimeSpan.Zero).Telemetry.RangeToTargetMeters;

        Assert.AreEqual(1_737_400.0 * Math.PI / 180.0, range, 1.0);
    }

    [TestMethod]
    public void Snapshot_Range_IsZeroWhenBodyDiffersFromTarget()
    {
        var timeline = new MissionTimeline(
            [Frame(0, CelestialBody.Earth, 100, 0)], [], new MissionTarget(CelestialBody.Moon, 0, 0));

        Assert.AreEqual(0, timeline.GetSnapshot(TimeSpan.Zero).Telemetry.RangeToTargetMeters);
    }

    [TestMethod]
    public void Alarms_AreSortedByMet()
    {
        var timeline = new MissionTimeline(
            [Frame(0, CelestialBody.Earth, 0, 0)],
            [new MissionAlarm(TimeSpan.FromSeconds(30), 2), new MissionAlarm(TimeSpan.FromSeconds(10), 1)]);

        CollectionAssert.AreEqual(new[] { 1, 2 }, timeline.Alarms.Select(a => a.Code).ToArray());
    }
}
