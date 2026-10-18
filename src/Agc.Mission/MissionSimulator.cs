using Agc.Core;

namespace Agc.Mission;

/// <summary>
/// 미션 시계를 진행시키고 현재 텔레메트리를 제공하는 시뮬레이터. DSKY와는 <see cref="ITelemetrySource"/>로 연결된다.
/// - Owner: UI 스레드(MainWindow). Lifetime: 앱 실행 동안.
/// - 스레드: 단일 스레드(UI)에서만 호출한다. 이벤트도 호출한 스레드에서 동기 발생한다.
/// - Advance는 실시간 경과 시간 × 배속만큼 MET를 진행시키고, 그 사이 지난 알람/단계 전환 이벤트를 발생시킨다.
/// - Seek은 임의 MET로 점프하며 알람은 발생시키지 않는다(건너뛴 알람은 재현하지 않음).
/// </summary>
public sealed class MissionSimulator : ITelemetrySource
{
    private readonly MissionTimeline _timeline;
    private readonly ILog _log;
    private MissionSnapshot _snapshot;

    public MissionSimulator(MissionTimeline timeline, ILog log)
    {
        _timeline = timeline;
        _log = log;
        _snapshot = timeline.GetSnapshot(TimeSpan.Zero);
    }

    /// <summary>지나간 알람 발생 시. 인자는 알람 정보.</summary>
    public event Action<MissionAlarm>? AlarmRaised;

    /// <summary>다른 단계(키프레임)로 넘어갔을 때. 인자는 새 스냅샷.</summary>
    public event Action<MissionSnapshot>? PhaseChanged;

    public MissionTimeline Timeline => _timeline;

    public MissionSnapshot Snapshot => _snapshot;

    public AgcTelemetry Current => _snapshot.Telemetry;

    public TimeSpan Met => _snapshot.Met;

    /// <summary>진행 중 여부. 미션 끝에 도달하면 자동으로 false가 된다.</summary>
    public bool IsRunning { get; set; }

    /// <summary>실시간 1초당 흘러가는 미션 초(배속).</summary>
    public double SpeedMultiplier { get; set; } = 1.0;

    /// <summary>실시간 경과분만큼 미션을 진행시킨다. 정지 상태면 아무 일도 하지 않는다.</summary>
    public void Advance(TimeSpan realElapsed)
    {
        if (!IsRunning)
        {
            return;
        }

        var oldMet = _snapshot.Met;
        var newMet = oldMet + TimeSpan.FromTicks((long)(realElapsed.Ticks * SpeedMultiplier));
        if (newMet >= _timeline.Duration)
        {
            newMet = _timeline.Duration;
            IsRunning = false;
        }
        else
        {
            // 아직 미션 끝이 아니므로 계속 진행한다.
        }

        foreach (var alarm in _timeline.Alarms.Where(a => a.Met > oldMet && a.Met <= newMet))
        {
            AlarmRaised?.Invoke(alarm);
        }

        MoveTo(newMet);
    }

    /// <summary>지정한 MET로 이동한다(0~Duration으로 제한). 알람은 발생시키지 않는다.</summary>
    public void Seek(TimeSpan met)
    {
        MoveTo(met);
    }

    private void MoveTo(TimeSpan met)
    {
        var previousIndex = _snapshot.PhaseIndex;
        _snapshot = _timeline.GetSnapshot(met);
        if (_snapshot.PhaseIndex != previousIndex)
        {
            _log.Info($"Phase -> {_snapshot.Phase.PhaseName} (P{_snapshot.Phase.ProgramNumber:D2}) at MET {_snapshot.Met:hh\\:mm\\:ss}");
            PhaseChanged?.Invoke(_snapshot);
        }
        else
        {
            // 같은 단계 안에서의 이동은 로그/이벤트 없이 값만 갱신한다.
        }
    }
}
