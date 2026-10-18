using Agc.Tests.Harness;

namespace Agc.Tests.Dsky;

/// <summary>기능: Noun별 레지스터 표시 형식과 단위 변환(DskyNouns). 정의는 Luminary 099 Noun 표 기준.</summary>
[TestClass]
public sealed class DskyNounFormatTests
{
    [TestMethod]
    public void Format_DecimalAlwaysHasSign()
    {
        Assert.AreEqual("+00067", DskyNouns.Format(67));
        Assert.AreEqual("-02347", DskyNouns.Format(-2347));
        Assert.AreEqual("+00000", DskyNouns.Format(0));
    }

    [TestMethod]
    public void Format_Overflow_ClampsTo99999()
    {
        Assert.AreEqual("+99999", DskyNouns.Format(1e9));
        Assert.AreEqual("-99999", DskyNouns.Format(-1e9));
    }

    [TestMethod]
    public void Format_RoundsHalfAwayFromZero()
    {
        Assert.AreEqual("+00003", DskyNouns.Format(2.5));
        Assert.AreEqual("-00003", DskyNouns.Format(-2.5));
    }

    [TestMethod]
    public void FormatOctal_HasBlankSignSlot()
    {
        Assert.AreEqual(" 01202", DskyNouns.FormatOctal(1202));
    }

    [TestMethod]
    public void FormatMinSec_UsesMinutesBlankSeconds()
    {
        Assert.AreEqual("+08 27", DskyNouns.FormatMinSec(TimeSpan.FromSeconds(507)));
        Assert.AreEqual("+00 00", DskyNouns.FormatMinSec(TimeSpan.FromSeconds(-5)));
        Assert.AreEqual("+99 00", DskyNouns.FormatMinSec(TimeSpan.FromMinutes(100)));
    }

    [TestMethod]
    public void Noun36_ClockShowsHoursMinutesCentiseconds()
    {
        var t = FakeTelemetry.Make(met: new TimeSpan(0, 102, 33, 5, 120));

        Assert.IsTrue(DskyNouns.TryFormat(DskyNouns.ClockTime, t, out var r));

        CollectionAssert.AreEqual(new[] { "+00102", "+00033", "+00512" }, r);
    }

    [TestMethod]
    public void Noun43_ShowsLatLongInHundredthDegreesAndAltitudeInTenthNm()
    {
        var t = FakeTelemetry.Make(lat: 0.67, lon: -23.47, altitude: 18520);

        Assert.IsTrue(DskyNouns.TryFormat(DskyNouns.LatLongAltitude, t, out var r));

        CollectionAssert.AreEqual(new[] { "+00067", "-02347", "+00100" }, r);
    }

    [TestMethod]
    public void Noun63_ShowsVelocityAltRateInTenthFpsAndAltitudeInFeet()
    {
        var t = FakeTelemetry.Make(speed: 304.8, verticalSpeed: -30.48, altitude: 1852);

        Assert.IsTrue(DskyNouns.TryFormat(DskyNouns.LandingVelocity, t, out var r));

        CollectionAssert.AreEqual(new[] { "+10000", "-01000", "+06076" }, r);
    }

    [TestMethod]
    public void Noun68_ShowsRangeInTenthNmTimeToGoAndZeroAltitudeDelta()
    {
        var t = FakeTelemetry.Make(range: 18520, timeToNextPhase: TimeSpan.FromSeconds(507));

        Assert.IsTrue(DskyNouns.TryFormat(DskyNouns.LandingRange, t, out var r));

        CollectionAssert.AreEqual(new[] { "+00100", "+08 27", "+00000" }, r);
    }

    [TestMethod]
    public void TryFormat_UnknownNoun_ReturnsFalse()
    {
        Assert.IsFalse(DskyNouns.TryFormat(99, FakeTelemetry.Make(), out _));
        Assert.IsFalse(DskyNouns.TryFormat(64, FakeTelemetry.Make(), out _), "N64는 아직 지원하지 않는다");
    }

    [TestMethod]
    public void IsTelemetryNoun_LunarModuleSupportsClockPositionVelocityDescentAndRange()
    {
        Assert.IsTrue(new[] { 36, 43, 62, 63, 68 }.All(n => DskyNouns.IsTelemetryNoun(n, AgcVehicle.Lm)));
        Assert.IsFalse(DskyNouns.IsTelemetryNoun(DskyNouns.AlarmCodes, AgcVehicle.Lm));
    }

    [TestMethod]
    public void IsTelemetryNoun_CommandModuleSupportsClockPositionAndVelocityOnly()
    {
        Assert.IsTrue(new[] { 36, 43, 62 }.All(n => DskyNouns.IsTelemetryNoun(n, AgcVehicle.Csm)));
        Assert.IsFalse(DskyNouns.IsTelemetryNoun(63, AgcVehicle.Csm), "CM의 N63은 재진입 사거리/예측속도/TTE(아직 미지원)");
        Assert.IsFalse(DskyNouns.IsTelemetryNoun(68, AgcVehicle.Csm), "CM의 N68은 재진입 뱅크각/속도/Rdot(아직 미지원)");
    }

    [TestMethod]
    public void Noun62_CommandModule_ShowsWholeFpsVelocityAltRateAndAltitudeInTenthNm()
    {
        // Comanche 055: N62 = 관성속도 크기(VEL2 XXXXX. ft/s), 고도변화율(VEL2), 패드 반경 위 고도(POS4 XXXX.X nm)
        var t = FakeTelemetry.Make(speed: 304.8, verticalSpeed: -30.48, altitude: 18520, vehicle: AgcVehicle.Csm);

        Assert.IsTrue(DskyNouns.TryFormat(DskyNouns.Velocity, t, out var r));

        CollectionAssert.AreEqual(new[] { "+01000", "-00100", "+00100" }, r);
    }

    [TestMethod]
    public void Noun62_LunarModule_ShowsVelocityTimeToIgnitionAndDeltaV()
    {
        // Luminary 099: N62 = 속도(VEL3 XXXX.X ft/s), 점화까지 시간(MIN/SEC), 누적 ΔV(VEL3, 모델링 안 함 → 0)
        var t = FakeTelemetry.Make(speed: 304.8, timeToNextPhase: TimeSpan.FromSeconds(507), vehicle: AgcVehicle.Lm);

        Assert.IsTrue(DskyNouns.TryFormat(DskyNouns.Velocity, t, out var r));

        CollectionAssert.AreEqual(new[] { "+10000", "+08 27", "+00000" }, r);
    }

    [TestMethod]
    public void LandingNouns_OnCommandModule_AreNotFormatted()
    {
        var cm = FakeTelemetry.Make(vehicle: AgcVehicle.Csm);

        Assert.IsFalse(DskyNouns.TryFormat(DskyNouns.LandingVelocity, cm, out _));
        Assert.IsFalse(DskyNouns.TryFormat(DskyNouns.LandingRange, cm, out _));
    }

    [TestMethod]
    public void ClockAndPositionNouns_AreTheSameOnBothVehicles()
    {
        var met = new TimeSpan(0, 5, 6, 7, 120);
        var cm = FakeTelemetry.Make(met: met, lat: 1.5, lon: -2.5, altitude: 1852, vehicle: AgcVehicle.Csm);
        var lm = FakeTelemetry.Make(met: met, lat: 1.5, lon: -2.5, altitude: 1852, vehicle: AgcVehicle.Lm);

        foreach (var noun in new[] { DskyNouns.ClockTime, DskyNouns.LatLongAltitude })
        {
            Assert.IsTrue(DskyNouns.TryFormat(noun, cm, out var a));
            Assert.IsTrue(DskyNouns.TryFormat(noun, lm, out var b));
            CollectionAssert.AreEqual(a, b, $"N{noun}");
        }
    }
}
