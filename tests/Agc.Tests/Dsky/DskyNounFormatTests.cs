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
        // N62는 Luminary에서 속도/점화까지 시간/누적 ΔV 이며 아직 지원하지 않는다.
        Assert.IsFalse(DskyNouns.TryFormat(62, FakeTelemetry.Make(), out _));
        Assert.IsFalse(DskyNouns.TryFormat(99, FakeTelemetry.Make(), out _));
    }

    [TestMethod]
    public void IsTelemetryNoun_OnlyForSupportedNouns()
    {
        Assert.IsTrue(new[] { 36, 43, 63, 68 }.All(DskyNouns.IsTelemetryNoun));
        Assert.IsFalse(DskyNouns.IsTelemetryNoun(DskyNouns.AlarmCodes));
        Assert.IsFalse(DskyNouns.IsTelemetryNoun(62));
    }
}
