using Agc.Core;

namespace Agc.Mission;

/// <summary>
/// 미션 시뮬레이터와 DSKY를 잇는 연결부.
/// - 알람: 시뮬레이터의 AlarmRaised를 DSKY의 RaiseAlarm으로 전달한다(PROG 램프).
///   1201/1202(Executive 용량 초과)는 BAILOUT 재시작이므로 승무원의 모니터가 사라진다(DSKY의 ExecutiveRestart).
/// - 프로그램 표시: 재생 중 표시 요청 시각을 지나가면 그 요청을 DSKY의 RequestDisplay로 전달한다.
///   다른 단계로 넘어가거나(재생이든 Seek든) 생성될 때는 그 단계에서 지금까지 나온 가장 최신 요청 하나를 보여 준다.
///   같은 단계 안의 Seek는 표시를 바꾸지 않는다. 승무원이 DSKY를 쓰는 중이면 DSKY가 KEY REL 램프로 알린다.
/// - Owner: UI 스레드(MainWindow). Lifetime: 앱 실행 동안(시뮬레이터/DSKY와 같다). 구독 해제는 하지 않는다.
/// - 스레드: 시뮬레이터와 같은 단일(UI) 스레드에서만 사용한다.
/// </summary>
public sealed class AgcMissionLink
{
    // 재시작(BAILOUT)을 일으키는 알람: 1201 = VAC 영역 부족, 1202 = 코어 세트 부족.
    private const int AlarmNoVacAreas = 1201;
    private const int AlarmNoCoreSets = 1202;

    private readonly MissionSimulator _simulator;
    private readonly DskyController _dsky;
    // 단계 전환 때 이미 반영한 시각. 같은 Advance에서 같은 요청이 두 번 전달되지 않게 한다.
    private TimeSpan _appliedThrough = TimeSpan.MinValue;

    public AgcMissionLink(MissionSimulator simulator, DskyController dsky)
    {
        _simulator = simulator;
        _dsky = dsky;
        _simulator.AlarmRaised += OnAlarm;
        _simulator.PhaseChanged += ApplyLatestPhaseRequest;
        _simulator.Advanced += ApplyCrossedRequests;
        ApplyLatestPhaseRequest(_simulator.Snapshot);
    }

    private void OnAlarm(MissionAlarm alarm)
    {
        _dsky.RaiseAlarm(alarm.Code);
        if (alarm.Code is AlarmNoVacAreas or AlarmNoCoreSets)
        {
            _dsky.ExecutiveRestart();
        }
        else
        {
            // 그 밖의 알람은 램프만 켜고 표시는 건드리지 않는다.
        }
    }

    /// <summary>새 단계에 들어선 시점: 그 단계 시작부터 지금까지 나온 최신 표시 요청 하나를 전달한다(없으면 아무것도 하지 않는다).</summary>
    private void ApplyLatestPhaseRequest(MissionSnapshot snapshot)
    {
        _appliedThrough = snapshot.Met;
        var latest = _simulator.Timeline.DisplayRequests
            .Where(r => r.Met >= snapshot.Phase.Met && r.Met <= snapshot.Met)
            .OrderBy(r => r.Met)
            .LastOrDefault();
        if (latest is not null)
        {
            _dsky.RequestDisplay(latest.Verb, latest.Noun, latest.Flash);
        }
        else
        {
            // 이 단계에서는 아직 프로그램이 띄운 표시가 없다.
        }
    }

    /// <summary>재생으로 시각을 지나간 표시 요청을 시간 순서대로 전달한다.</summary>
    private void ApplyCrossedRequests(TimeSpan from, TimeSpan to)
    {
        var start = from > _appliedThrough ? from : _appliedThrough;
        foreach (var request in _simulator.Timeline.DisplayRequests
                     .Where(r => r.Met > start && r.Met <= to)
                     .OrderBy(r => r.Met))
        {
            _dsky.RequestDisplay(request.Verb, request.Noun, request.Flash);
        }

        _appliedThrough = to;
    }
}
