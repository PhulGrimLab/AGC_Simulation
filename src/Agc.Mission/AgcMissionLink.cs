using Agc.Core;

namespace Agc.Mission;

/// <summary>
/// 미션 시뮬레이터와 DSKY를 잇는 연결부.
/// - 알람: 시뮬레이터의 AlarmRaised를 DSKY의 RaiseAlarm으로 전달한다(PROG 램프).
/// - 프로그램 표시: 새 단계에 들어갈 때(재생으로 넘어가든 Seek로 이동하든) 그 단계에 정의된 표시 요청을
///   DSKY의 RequestDisplay로 전달한다. 승무원이 DSKY를 쓰는 중이면 DSKY가 KEY REL 램프로 알린다.
///   생성 시점의 현재 단계(예: 발사)의 요청도 바로 적용한다.
/// - Owner: UI 스레드(MainWindow). Lifetime: 앱 실행 동안(시뮬레이터/DSKY와 같다). 구독 해제는 하지 않는다.
/// - 스레드: 시뮬레이터와 같은 단일(UI) 스레드에서만 사용한다.
/// </summary>
public sealed class AgcMissionLink
{
    private readonly MissionSimulator _simulator;
    private readonly DskyController _dsky;

    public AgcMissionLink(MissionSimulator simulator, DskyController dsky)
    {
        _simulator = simulator;
        _dsky = dsky;
        _simulator.AlarmRaised += alarm => _dsky.RaiseAlarm(alarm.Code);
        _simulator.PhaseChanged += ApplyPhaseRequests;
        ApplyPhaseRequests(_simulator.Snapshot);
    }

    /// <summary>스냅샷의 단계가 막 시작된 것으로 보고, 그 단계 시작 시각에 정의된 표시 요청을 DSKY에 전달한다.</summary>
    private void ApplyPhaseRequests(MissionSnapshot snapshot)
    {
        foreach (var request in _simulator.Timeline.DisplayRequests.Where(r => r.Met == snapshot.Phase.Met))
        {
            _dsky.RequestDisplay(request.Verb, request.Noun, request.Flash);
        }
    }
}
