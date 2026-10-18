namespace Agc.Mission;

/// <summary>가이드 단계의 종류: 무엇을 조작/관찰하는 단계인가.</summary>
public enum GuideStepKind
{
    /// <summary>DSKY 키 입력(Verb/Noun 등).</summary>
    Dsky,

    /// <summary>DSKY가 아닌 승무원 조작(스위치, 조종기 등).</summary>
    Crew,

    /// <summary>지상 관제와의 교신/판단.</summary>
    Ground,

    /// <summary>화면에서 관찰할 내용(조작 없음).</summary>
    Observe,
}

/// <summary>설명 내용이 얼마나 근거가 확인되었는가(doc/12의 확인 수준 태그와 같다).</summary>
public enum GuideConfidence
{
    /// <summary>공개 자료에서 직접 확인했다([확인]).</summary>
    Confirmed,

    /// <summary>일부만 확인했다([부분]).</summary>
    Partial,

    /// <summary>실제 키 시퀀스는 확인하지 못했고 일반 절차로 설명한다([일반]).</summary>
    General,

    /// <summary>확인하지 못했다([미확인]).</summary>
    Unconfirmed,
}

/// <summary>
/// 가이드의 한 단계(예: "프로그램 선택"). 무엇을 하고, 왜 하며, 그 결과 무엇이 달라지는지를 담는다.
/// </summary>
/// <param name="Title">짧은 제목(예: "하강 프로그램 선택").</param>
/// <param name="Kind">조작 종류.</param>
/// <param name="Keys">DSKY 키 입력. 표기: 숫자, V=VERB, N=NOUN, E=ENTR, C=CLR, R=RSET, K=KEY REL, P=PRO, +, -.
/// DSKY 입력이 아니면 null. 예: "V37E63E".</param>
/// <param name="Action">구체적인 조작 설명(키의 의미, 입력 순서).</param>
/// <param name="Purpose">이 조작을 하는 이유(목적).</param>
/// <param name="Effect">조작의 결과와 비행/컴퓨터에 미치는 영향.</param>
/// <param name="Runnable">true이면 화면의 [실행] 버튼이 이 시뮬레이터의 DSKY에서 Keys를 그대로 실행할 수 있다
/// (시뮬레이터가 지원하는 명령일 때만 true).</param>
/// <param name="Confidence">근거의 확인 수준.</param>
public sealed record GuideStep(
    string Title,
    GuideStepKind Kind,
    string? Keys,
    string Action,
    string Purpose,
    string Effect,
    bool Runnable,
    GuideConfidence Confidence);

/// <summary>
/// 미션 한 단계(키프레임)의 설명서: 무슨 일이 일어나는지, AGC의 역할, 승무원의 조작 순서와 그 목적·영향.
/// 오른쪽 가이드 패널이 현재 단계에 맞춰 보여 준다.
/// </summary>
/// <param name="PhaseName">키프레임의 PhaseName과 같아야 한다.</param>
/// <param name="Summary">이 단계의 미션 설명(2~4문장).</param>
/// <param name="AgcRole">이 단계에서 AGC가 하는 일(항목별).</param>
/// <param name="Steps">조작 순서(시간순).</param>
/// <param name="Reference">더 자세한 설명 위치(예: "doc/12_Mission_Phase_AGC_Guide.md §10").</param>
public sealed record PhaseGuide(
    string PhaseName,
    string Summary,
    IReadOnlyList<string> AgcRole,
    IReadOnlyList<GuideStep> Steps,
    string Reference);
