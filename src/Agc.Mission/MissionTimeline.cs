using Agc.Core;

namespace Agc.Mission;

/// <summary>기준 천체. 고도/속도는 이 천체 기준이다.</summary>
public enum CelestialBody
{
    Earth,
    Moon,
}

/// <summary>
/// 미션 타임라인의 한 지점(키프레임). 이 시각부터 다음 키프레임 전까지가 하나의 단계(phase)이다.
/// </summary>
public sealed record MissionKeyframe(
    TimeSpan Met,
    string PhaseName,
    string Description,
    int ProgramNumber,
    CelestialBody Body,
    double AltitudeMeters,
    double SpeedMetersPerSecond,
    double LatitudeDegrees,
    double LongitudeDegrees);

/// <summary>사거리 계산의 목표 지점(예: 착륙 지점). 지정한 천체 표면 위의 위·경도이다.</summary>
public sealed record MissionTarget(CelestialBody Body, double LatitudeDegrees, double LongitudeDegrees);

/// <summary>특정 MET에 발생하는 AGC 프로그램 알람(예: 1202).</summary>
public sealed record MissionAlarm(TimeSpan Met, int Code);

/// <summary>특정 MET의 미션 상태: 속한 단계와 보간된 텔레메트리.</summary>
public sealed record MissionSnapshot(
    TimeSpan Met,
    MissionKeyframe Phase,
    int PhaseIndex,
    CelestialBody Body,
    AgcTelemetry Telemetry);

/// <summary>
/// 키프레임과 알람 목록으로 임의 MET의 미션 상태를 계산하는 불변 타임라인.
/// 같은 천체 기준인 인접 키프레임 사이는 선형 보간하고, 천체가 바뀌는 구간은 앞 키프레임 값을 유지한다.
/// 고도 변화율은 구간의 평균 기울기로 근사한다.
/// </summary>
public sealed class MissionTimeline
{
    private const double MoonRadiusMeters = 1_737_400.0;
    private const double EarthRadiusMeters = 6_371_000.0;
    private const double DegreesToRadians = Math.PI / 180.0;

    private readonly MissionTarget? _target;

    /// <param name="target">사거리(RangeToTargetMeters) 계산 대상. null이면 사거리는 항상 0이다.</param>
    public MissionTimeline(
        IReadOnlyList<MissionKeyframe> keyframes, IReadOnlyList<MissionAlarm> alarms, MissionTarget? target = null)
    {
        _target = target;
        if (keyframes.Count == 0)
        {
            throw new ArgumentException("키프레임이 최소 1개 필요합니다.", nameof(keyframes));
        }

        for (var i = 1; i < keyframes.Count; i++)
        {
            if (keyframes[i].Met <= keyframes[i - 1].Met)
            {
                throw new ArgumentException($"키프레임 MET는 엄격히 증가해야 합니다 (index {i}).", nameof(keyframes));
            }
        }

        Keyframes = keyframes;
        Alarms = alarms.OrderBy(a => a.Met).ToList();
    }

    public IReadOnlyList<MissionKeyframe> Keyframes { get; }

    public IReadOnlyList<MissionAlarm> Alarms { get; }

    /// <summary>미션 총 길이(마지막 키프레임의 MET).</summary>
    public TimeSpan Duration => Keyframes[^1].Met;

    /// <summary>주어진 MET(범위 밖이면 0~Duration으로 제한)의 미션 상태를 계산한다.</summary>
    public MissionSnapshot GetSnapshot(TimeSpan met)
    {
        met = met < TimeSpan.Zero ? TimeSpan.Zero : (met > Duration ? Duration : met);
        var index = FindPhaseIndex(met);
        var current = Keyframes[index];
        var next = index + 1 < Keyframes.Count ? Keyframes[index + 1] : null;

        var altitude = current.AltitudeMeters;
        var speed = current.SpeedMetersPerSecond;
        var latitude = current.LatitudeDegrees;
        var longitude = current.LongitudeDegrees;
        var verticalSpeed = 0.0;

        if (next is not null && next.Body == current.Body)
        {
            var span = (next.Met - current.Met).TotalSeconds;
            var fraction = (met - current.Met).TotalSeconds / span;
            altitude = Lerp(current.AltitudeMeters, next.AltitudeMeters, fraction);
            speed = Lerp(current.SpeedMetersPerSecond, next.SpeedMetersPerSecond, fraction);
            latitude = Lerp(current.LatitudeDegrees, next.LatitudeDegrees, fraction);
            longitude = Lerp(current.LongitudeDegrees, next.LongitudeDegrees, fraction);
            verticalSpeed = (next.AltitudeMeters - current.AltitudeMeters) / span;
        }
        else
        {
            // 마지막 구간이거나 기준 천체가 바뀌는 구간: 값을 보간할 수 없으므로 앞 키프레임 값을 유지한다.
        }

        var timeToNextPhase = next is null ? TimeSpan.Zero : next.Met - met;
        var range = RangeToTarget(current.Body, altitude, latitude, longitude);
        var telemetry = new AgcTelemetry(
            met, current.ProgramNumber, altitude, speed, verticalSpeed, latitude, longitude, range, timeToNextPhase);
        return new MissionSnapshot(met, current, index, current.Body, telemetry);
    }

    /// <summary>MET 이하인 마지막 키프레임의 인덱스(첫 키프레임보다 이르면 0).</summary>
    private int FindPhaseIndex(TimeSpan met)
    {
        var index = 0;
        for (var i = 0; i < Keyframes.Count; i++)
        {
            if (Keyframes[i].Met <= met)
            {
                index = i;
            }
            else
            {
                break;
            }
        }

        return index;
    }

    /// <summary>
    /// 현재 위치에서 목표 지점까지의 사거리(m): 천체 표면의 대원 거리와 고도를 직각으로 합성한 근사값.
    /// 목표가 없거나 목표와 다른 천체 기준이면 0.
    /// </summary>
    private double RangeToTarget(CelestialBody body, double altitude, double latitude, double longitude)
    {
        if (_target is null || _target.Body != body)
        {
            return 0;
        }

        var radius = body == CelestialBody.Moon ? MoonRadiusMeters : EarthRadiusMeters;
        var lat1 = latitude * DegreesToRadians;
        var lat2 = _target.LatitudeDegrees * DegreesToRadians;
        var dLon = (longitude - _target.LongitudeDegrees) * DegreesToRadians;
        var cosAngle = Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        var ground = radius * Math.Acos(Math.Clamp(cosAngle, -1.0, 1.0));
        return Math.Sqrt(ground * ground + altitude * altitude);
    }

    private static double Lerp(double a, double b, double fraction)
    {
        return a + (b - a) * fraction;
    }
}
