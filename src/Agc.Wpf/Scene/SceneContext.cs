using Agc.Core;
using Agc.Mission;

namespace Agc.Wpf.Scene;

/// <summary>
/// 한 순간의 장면을 그리는 데 필요한 값들. 미션 스냅샷에서 계산하며 렌더러는 이 값만 본다(UI와 미션 모델의 분리).
/// 불변 값 객체이다.
/// </summary>
internal sealed record SceneContext(
    MissionScene Scene,
    CelestialBody Body,
    AgcVehicle Vehicle,
    int Program,
    bool EngineOn,
    TimeSpan Met,
    double AltitudeMeters,
    double SpeedMetersPerSecond,
    double VerticalSpeedMetersPerSecond,
    double PhaseProgress,
    double JourneyFraction,
    string PhaseName)
{
    private const double EarthMoonDistanceMeters = 384_400_000.0;
    private const double JourneyMin = 0.02;
    private const double JourneyMax = 0.98;

    // 엔진이 실제로 불을 뿜는 시간(P40 점화는 단계 전체가 아니라 앞부분만): LOI-1 약 357.5초, TEI 약 148초
    private static readonly TimeSpan MajorBurnWindow = TimeSpan.FromSeconds(360);

    // 로켓/엔진이 불을 뿜는 프로그램: TLI(15), 상승(12), 동력 하강(63/64/66)
    private static readonly int[] ThrustingPrograms = [12, 15, 63, 64, 66];
    private const int ServicePropulsionProgram = 40;

    /// <summary>미션 스냅샷과 타임라인으로 장면 문맥을 만든다.</summary>
    public static SceneContext From(MissionSnapshot snapshot, MissionTimeline timeline)
    {
        var phase = snapshot.Phase;
        var next = snapshot.PhaseIndex + 1 < timeline.Keyframes.Count ? timeline.Keyframes[snapshot.PhaseIndex + 1].Met : phase.Met;
        var span = (next - phase.Met).TotalSeconds;
        var progress = span > 0 ? Math.Clamp((snapshot.Met - phase.Met).TotalSeconds / span, 0.0, 1.0) : 1.0;
        var elapsed = snapshot.Met - phase.Met;

        var engineOn = phase.Scene == MissionScene.Launch
            || ThrustingPrograms.Contains(phase.ProgramNumber)
            || (phase.ProgramNumber == ServicePropulsionProgram && elapsed < MajorBurnWindow);

        var altitude = snapshot.Telemetry.AltitudeMeters;
        var journey = snapshot.Body == CelestialBody.Earth
            ? altitude / EarthMoonDistanceMeters
            : 1.0 - altitude / EarthMoonDistanceMeters;

        return new SceneContext(
            phase.Scene,
            snapshot.Body,
            snapshot.Telemetry.Vehicle,
            phase.ProgramNumber,
            engineOn,
            snapshot.Met,
            altitude,
            snapshot.Telemetry.SpeedMetersPerSecond,
            snapshot.Telemetry.VerticalSpeedMetersPerSecond,
            progress,
            Math.Clamp(journey, JourneyMin, JourneyMax),
            phase.PhaseName);
    }
}
