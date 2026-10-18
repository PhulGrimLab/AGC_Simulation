namespace Agc.Core;

/// <summary>
/// DSKY가 연결된 컴퓨터/우주선. 사령선(CMC: Colossus)과 달착륙선(LGC: Luminary)은 같은 Noun 번호라도
/// 정의가 다르고(예: N62, N63, N68) DSKY 램프도 다르다(ALT/VEL은 달착륙선에만 있다).
/// </summary>
public enum AgcVehicle
{
    /// <summary>사령선(Command Module) — CMC, Colossus 2A (Comanche 055).</summary>
    Csm,

    /// <summary>달착륙선(Lunar Module) — LGC, Luminary 1A (099).</summary>
    Lm,
}

/// <summary>
/// AGC가 읽어 들이는 우주선 상태값(SI 단위). 미션 시뮬레이터가 만들고 DSKY가 소비한다.
/// 고도는 현재 기준 천체(지구/달) 표면 위 높이, 속도는 그 천체 기준 상대 속도이다.
/// RangeToTargetMeters는 목표 지점(예: 착륙 지점)까지의 사거리, TimeToNextPhase는 다음 미션 단계까지 남은 시간이다.
/// 목표가 없거나 다른 천체이면 RangeToTargetMeters는 0이다.
/// Vehicle은 지금 DSKY가 연결된 컴퓨터(사령선 또는 달착륙선)이다.
/// </summary>
public sealed record AgcTelemetry(
    TimeSpan MissionElapsedTime,
    int ProgramNumber,
    double AltitudeMeters,
    double SpeedMetersPerSecond,
    double VerticalSpeedMetersPerSecond,
    double LatitudeDegrees,
    double LongitudeDegrees,
    double RangeToTargetMeters = 0,
    TimeSpan TimeToNextPhase = default,
    AgcVehicle Vehicle = AgcVehicle.Csm);

/// <summary>
/// 현재 우주선 상태를 제공하는 공급자. DSKY는 이 인터페이스로만 미션 쪽과 연결된다.
/// </summary>
public interface ITelemetrySource
{
    /// <summary>가장 최근 상태값.</summary>
    AgcTelemetry Current { get; }
}
