namespace Agc.Tests.Mission;

/// <summary>기능: Apollo 11 타임라인 데이터의 무결성과 주요 이벤트 시각.</summary>
[TestClass]
public sealed class Apollo11MissionDataTests
{
    private static readonly MissionTimeline Timeline = Apollo11Mission.Create();

    [TestMethod]
    public void Create_Succeeds_AndStartsWithLaunchProgram11()
    {
        var first = Timeline.Keyframes[0];

        Assert.AreEqual(TimeSpan.Zero, first.Met);
        Assert.AreEqual(11, first.ProgramNumber);
        Assert.AreEqual(0, first.AltitudeMeters);
    }

    [TestMethod]
    public void EndsAtSplashdown_195h18m35s_OnEarthAtZeroAltitude()
    {
        var last = Timeline.Keyframes[^1];

        Assert.AreEqual(new TimeSpan(195, 18, 35), last.Met);
        Assert.AreEqual(CelestialBody.Earth, last.Body);
        Assert.AreEqual(0, last.AltitudeMeters);
        Assert.AreEqual(0, last.SpeedMetersPerSecond);
    }

    [TestMethod]
    public void AllKeyframes_HaveNonNegativeAltitudeAndSpeed_AndDescriptions()
    {
        foreach (var k in Timeline.Keyframes)
        {
            Assert.IsGreaterThanOrEqualTo(0.0, k.AltitudeMeters, k.PhaseName);
            Assert.IsGreaterThanOrEqualTo(0.0, k.SpeedMetersPerSecond, k.PhaseName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(k.PhaseName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(k.Description), k.PhaseName);
        }
    }

    [TestMethod]
    public void PoweredDescentInitiation_StartsProgram63()
    {
        var snapshot = Timeline.GetSnapshot(new TimeSpan(102, 33, 5));

        Assert.AreEqual(63, snapshot.Telemetry.ProgramNumber);
        Assert.AreEqual(CelestialBody.Moon, snapshot.Body);
    }

    [TestMethod]
    public void Descent_AltitudeDecreasesMonotonicallyBetweenPdiAndTouchdown()
    {
        var previous = double.MaxValue;
        for (var s = 0; s <= 12 * 60 + 35; s += 15)
        {
            var altitude = Timeline.GetSnapshot(new TimeSpan(102, 33, 5) + TimeSpan.FromSeconds(s)).Telemetry.AltitudeMeters;

            Assert.IsLessThanOrEqualTo(previous, altitude, $"offset {s}s");
            previous = altitude;
        }
    }

    [TestMethod]
    public void Touchdown_At102h45m40s_IsProgram68WithZeroAltitudeAndSpeed()
    {
        var snapshot = Timeline.GetSnapshot(new TimeSpan(102, 45, 40));

        Assert.AreEqual(68, snapshot.Telemetry.ProgramNumber);
        Assert.AreEqual(0, snapshot.Telemetry.AltitudeMeters, 1e-9);
        Assert.AreEqual(0, snapshot.Telemetry.SpeedMetersPerSecond, 1e-9);
    }

    [TestMethod]
    public void RangeToLandingSite_AtPdiIsAboutTwoHundredFiftyNauticalMiles_AndZeroAtTouchdown()
    {
        var pdiNm = Timeline.GetSnapshot(new TimeSpan(102, 33, 5)).Telemetry.RangeToTargetMeters / 1852.0;
        var touchdown = Timeline.GetSnapshot(new TimeSpan(102, 45, 40)).Telemetry.RangeToTargetMeters;

        Assert.IsGreaterThan(240.0, pdiNm);
        Assert.IsLessThan(270.0, pdiNm);
        Assert.AreEqual(0, touchdown, 1.0);
    }

    [TestMethod]
    public void BrakingPhaseTimeToGo_AtPdiIsEightMinutes27Seconds()
    {
        var t = Timeline.GetSnapshot(new TimeSpan(102, 33, 5)).Telemetry;

        Assert.AreEqual(new TimeSpan(0, 8, 27), t.TimeToNextPhase);
    }

    [TestMethod]
    public void Vehicle_IsCommandModuleExceptBetweenUndockingAndDocking()
    {
        var expected = new (TimeSpan Met, AgcVehicle Vehicle)[]
        {
            (TimeSpan.Zero, AgcVehicle.Csm),
            (new TimeSpan(75, 49, 50), AgcVehicle.Csm),
            (new TimeSpan(100, 11, 59), AgcVehicle.Csm),
            (new TimeSpan(100, 12, 0), AgcVehicle.Lm),
            (new TimeSpan(102, 33, 5), AgcVehicle.Lm),
            (new TimeSpan(102, 45, 40), AgcVehicle.Lm),
            (new TimeSpan(109, 24, 15), AgcVehicle.Lm),
            (new TimeSpan(124, 29, 15), AgcVehicle.Lm),
            (new TimeSpan(128, 2, 59), AgcVehicle.Lm),
            (new TimeSpan(128, 3, 0), AgcVehicle.Csm),
            (new TimeSpan(135, 23, 42), AgcVehicle.Csm),
            (new TimeSpan(195, 18, 35), AgcVehicle.Csm),
        };

        foreach (var (met, vehicle) in expected)
        {
            Assert.AreEqual(vehicle, Timeline.GetSnapshot(met).Telemetry.Vehicle, $"MET {met}");
        }
    }

    [TestMethod]
    public void DisplayRequests_AreDefinedForLaunchMonitorAndPoweredDescent()
    {
        var requests = Timeline.DisplayRequests;

        Assert.IsTrue(requests.Any(r => r.Met == TimeSpan.Zero && r.Verb == 16 && r.Noun == 62), "P11: V16 N62");
        Assert.IsTrue(requests.Any(r => r.Met == new TimeSpan(102, 33, 5) && r.Verb == 16 && r.Noun == 63), "P63: V16 N63");
        foreach (var request in requests)
        {
            Assert.IsTrue(Timeline.Keyframes.Any(k => k.Met == request.Met), $"요청은 단계 시작 시각에 있어야 한다: {request.Met}");
        }
    }

    [TestMethod]
    public void DescentNounRequest_IsOnlyUsedOnTheLunarModule()
    {
        foreach (var request in Timeline.DisplayRequests.Where(r => r.Noun is 63 or 68))
        {
            Assert.AreEqual(AgcVehicle.Lm, Timeline.GetSnapshot(request.Met).Telemetry.Vehicle, $"N{request.Noun}");
        }
    }

    [TestMethod]
    public void DescentAlarms_FallDuringDescentPrograms()
    {
        // 공개 기록: 1202 4회 + 1201 1회. 1201은 P64 진입 후(102:41:32 이후)에 발생했다.
        Assert.HasCount(5, Timeline.Alarms);
        Assert.AreEqual(4, Timeline.Alarms.Count(a => a.Code == 1202));
        Assert.AreEqual(1, Timeline.Alarms.Count(a => a.Code == 1201));
        Assert.IsGreaterThan(new TimeSpan(102, 41, 32), Timeline.Alarms.Single(a => a.Code == 1201).Met);
        foreach (var alarm in Timeline.Alarms)
        {
            var program = Timeline.GetSnapshot(alarm.Met).Telemetry.ProgramNumber;

            Assert.IsTrue(program is 63 or 64 or 66, $"alarm {alarm.Code} in P{program}");
            Assert.IsTrue(alarm.Code is 1201 or 1202);
        }
    }
}
