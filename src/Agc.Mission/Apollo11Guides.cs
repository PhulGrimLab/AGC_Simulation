namespace Agc.Mission;

/// <summary>
/// Apollo 11 각 단계(키프레임)의 설명서 데이터. 단계마다 미션 설명, AGC의 역할, 승무원 조작 순서(목적과 영향 포함)를 담는다.
/// 내용은 전부 doc/12_Mission_Phase_AGC_Guide.md와 그 근거 자료에서 가져왔고, 각 단계의 확인 수준은 doc/12의 태그를 그대로 따른다.
/// Runnable=true인 DSKY 단계는 이 시뮬레이터가 그 단계에서 그대로 실행할 수 있는 키 입력뿐이다(PhaseGuideTests가 검증한다).
/// </summary>
public static class Apollo11Guides
{
    private const string Doc = "doc/12_Mission_Phase_AGC_Guide.md §";

    private static readonly IReadOnlyList<PhaseGuide> Guides = BuildGuides();

    /// <summary>모든 단계의 설명서.</summary>
    public static IReadOnlyList<PhaseGuide> All => Guides;

    /// <summary>키프레임 단계 이름으로 설명서를 찾는다. 없으면 null.</summary>
    public static PhaseGuide? For(string phaseName)
    {
        return Guides.FirstOrDefault(g => g.PhaseName == phaseName);
    }

    private static IReadOnlyList<PhaseGuide> BuildGuides()
    {
        var guides = new List<PhaseGuide>();
        guides.AddRange(LaunchAndEarthOrbit());
        guides.AddRange(TranslunarAndLunarOrbit());
        guides.AddRange(PoweredDescent());
        guides.AddRange(SurfaceAndAscent());
        guides.AddRange(ReturnAndEntry());
        return guides;
    }

    private static PhaseGuide Guide(string name, string summary, string[] agcRole, GuideStep[] steps, string chapter)
    {
        return new PhaseGuide(name, summary, agcRole, steps, Doc + chapter);
    }

    private static GuideStep Dsky(
        string title, string keys, string action, string purpose, string effect, bool runnable, GuideConfidence c)
    {
        return new GuideStep(title, GuideStepKind.Dsky, keys, action, purpose, effect, runnable, c);
    }

    private static GuideStep Crew(string title, string action, string purpose, string effect, GuideConfidence c)
    {
        return new GuideStep(title, GuideStepKind.Crew, null, action, purpose, effect, false, c);
    }

    private static GuideStep Ground(string title, string action, string purpose, string effect, GuideConfidence c)
    {
        return new GuideStep(title, GuideStepKind.Ground, null, action, purpose, effect, false, c);
    }

    private static GuideStep Observe(string title, string action, string purpose, string effect, GuideConfidence c)
    {
        return new GuideStep(title, GuideStepKind.Observe, null, action, purpose, effect, false, c);
    }

    // ------------------------------------------------------------------ 발사 ~ 지구 궤도 ~ TLI

    private static IEnumerable<PhaseGuide> LaunchAndEarthOrbit()
    {
        const GuideConfidence confirmed = GuideConfidence.Confirmed;
        const GuideConfidence partial = GuideConfidence.Partial;
        const GuideConfidence general = GuideConfidence.General;

        yield return Guide(
            "발사",
            "1969-07-16 새턴 V가 이륙한다(MET 000:00:00). 리프트오프 신호로 사령선 컴퓨터(CMC)의 P11 " +
            "(지구 궤도 진입 감시)이 자동으로 시작되고 시계가 0으로 맞춰진다. 새턴을 조종하는 것은 " +
            "새턴 IU이고 승무원은 DSKY와 FDAI로 상승을 감시한다.",
            [
                "리프트오프 신호를 받으면 P02가 끝나고 P11이 자동으로 시작되어 CMC 시계를 0으로 맞추고 평균 중력(AVERAGEG) 항법을 시작한다.",
                "새턴을 조종하지 않는다. 계산한 기준 자세와 실제 IMU 자세의 차를 FDAI 오차 바늘로 보여 준다.",
                "V06 N62(관성 속도, 고도변화율, 고도)를 2초마다 갱신해 승무원이 발사 큐 카드와 비교하게 한다.",
            ],
            [
                Dsky(
                    "발사 속도/고도 표시", "V16N62E",
                    "V16 N62 ENTR: 사령선의 N62를 모니터한다. R1=관성 속도(VI, fps), R2=고도변화율(HDOT, fps), " +
                    "R3=고도(n.mi.). 실제 P11은 이 표시를 스스로 띄워 2초마다 갱신한다.",
                    "승무원이 상승 중 실제 속도와 고도가 발사 단계 큐 카드와 맞는지 비교하기 위해서다.",
                    "화면에 N62가 나온다. 큐 카드와 어긋나면 중단(abort) 판단의 근거가 된다.",
                    true, confirmed),
                Observe(
                    "FDAI 오차 바늘",
                    "리프트오프 자세를 유지하다가 약 +10초에 피치/롤 프로그램이 시작되고 약 +133초에 고정되는 기준 자세를, " +
                    "FDAI 오차 바늘이 실제 자세와의 차이로 보여 준다.",
                    "새턴 유도가 계획 자세를 잘 따르는지 승무원이 독립적으로 확인하기 위해서다.",
                    "바늘이 크게 벗어나면 IU 유도 이상의 단서가 된다. CMC가 직접 자세를 바꾸지는 않는다.",
                    confirmed),
                Crew(
                    "LV GUID 스위치",
                    "정상 비행에서는 LV GUID 스위치를 IU 위치에 둔다. CMC 위치는 승무원이 새턴 인수(V46E)를 할 때만 쓴다.",
                    "유도의 주체를 새턴 IU로 두고 CMC는 감시만 하게 하려는 것이다.",
                    "IU가 부스터를 유도한다. 인수는 백업 기능이며 Apollo 11에서 쓰였는지는 자료에 없다.",
                    general),
                Dsky(
                    "백업 P11 시작", "V75E",
                    "V75 ENTR: 리프트오프 신호가 이상해 P11이 자동으로 시작되지 않을 때 승무원이 P11을 수동으로 시작한다. " +
                    "시뮬레이터는 V75를 지원하지 않는다.",
                    "리프트오프 신호가 없어도 P11 감시와 시계 기준을 확보하기 위해서다.",
                    "P11이 시작되어 시계가 0으로 맞춰지고 상승 감시가 이어진다. 이 키는 소스에 존재하며 절차는 일반 자료 기준이다.",
                    false, partial),
            ],
            "2");

        yield return Guide(
            "1단 분리",
            "MET 약 000:02:42에 S-IC 1단 연소가 끝나 분리되고 S-II 2단이 점화된다. 시뮬레이터의 이 시각과 수치(약 67 km, 2,400 m/s)는 " +
            "교육용 근사값이다.",
            [
                "CMC는 P11을 계속 실행하며 IMU 항법과 N62 표시를 갱신한다. 분리와 점화는 새턴 IU가 수행한다.",
                "FDAI 오차 바늘로 기준 자세와 실제 자세의 차이를 계속 보여 준다.",
            ],
            [
                Observe(
                    "N62로 진행 상태 확인",
                    "V06 N62가 2초마다 갱신되는 속도와 고도가 단 분리 전후로 연속적인지 본다.",
                    "단 분리 때 속도가 크게 달라지지 않는지(추력 공백 후 재가속) 큐 카드와 대조하기 위해서다.",
                    "정상이면 이어서 S-II 연소 중 속도와 고도가 계속 증가한다.",
                    GuideConfidence.Confirmed),
                Dsky(
                    "AGC 시계 확인", "V16N36E",
                    "V16 N36 ENTR: AGC 시계(MET)를 시·분·초로 표시한다. 시뮬레이터에서는 이 단계의 MET가 약 0:02:42로 나온다.",
                    "컴퓨터 시계가 미션 경과 시간과 일치하는지 확인하기 위해서다(P11이 리프트오프에 0으로 맞췄다).",
                    "화면에 시계가 나오고 그 값으로 이후 단계의 시각 기준(TIG 등)을 맞춘다.",
                    true, GuideConfidence.Confirmed),
            ],
            "2");

        yield return Guide(
            "2단 연소 종료",
            "MET 약 000:09:12에 S-II 2단 연소가 끝나고 S-IVB 3단이 이어받아 지구 저궤도 진입(000:11:49)까지 가속한다. " +
            "자료 AFJ의 중단 모드 일정으로는 IV 모드가 000:09:00이다.",
            [
                "P11이 계속 실행되어 평균 중력 항법으로 속도와 고도를 갱신한다.",
                "새턴 유도는 IU가 한다. CMC가 중단 판단을 하는 기능이 있었는지는 확인하지 못했다.",
            ],
            [
                Observe(
                    "삽입 목표 접근",
                    "V06 N62의 속도(R1)가 궤도 속도에 가까워지고 고도변화율(R2)이 0에 가까워지는지 본다.",
                    "S-IVB가 목표 궤도에 가까워지는 것을 큐 카드와 비교해 확인하기 위해서다.",
                    "속도와 고도가 목표에 맞으면 S-IVB가 정상 종료하고 궤도에 진입한다.",
                    GuideConfidence.Confirmed),
                Dsky(
                    "새턴 인수(백업)", "V46E",
                    "V46 ENTR: LV GUID 스위치를 CMC로 두고 회전 조종기(RHC)로 승무원이 부스터를 조종하는 백업 모드를 켠다. " +
                    "시뮬레이터는 V46을 지원하지 않는다.",
                    "IU 유도가 실패했을 때 승무원이 직접 상승을 이어가기 위한 백업 수단이다.",
                    "DAP가 인수 모드로 바뀐다. Apollo 11 비행에서 사용된 기록은 자료에 없다(일반 절차).",
                    false, general),
            ],
            "2");

        yield return Guide(
            "지구 저궤도 진입",
            "MET 000:11:49에 약 185 km 원 궤도에 진입한다(주차궤도 약 100 n.mi.). 이후 약 1.5바퀴를 돌며 시스템 점검, " +
            "별을 이용한 IMU 재정렬(P52), 지상의 상태벡터 업링크, TLI 준비를 한다.",
            [
                "S-IVB 종료 후 P00으로 돌아와 평균 중력 항법으로 궤도 상태를 유지하고 지상 업링크를 받는다.",
                "P52로 IMU의 별 기준 자세를 다시 맞추고 별 각도 차(N05)와 자이로 토크각(N93)을 보여 준다.",
            ],
            [
                Dsky(
                    "P00(대기)으로", "V37E00E",
                    "V37 ENTR 00 ENTR: 프로그램 00(CMC 대기)을 선택한다. 일반 절차(E2448)이며 Apollo 11의 실제 키 입력은 기록에서 확인하지 못했다.",
                    "S-IVB 종료로 P11 감시가 끝났으므로 컴퓨터를 대기 상태로 두고 다음 프로그램(P52 등)과 지상 업링크를 받을 준비를 하기 위해서다.",
                    "PROG 창이 00이 된다.",
                    true, general),
                Dsky(
                    "IMU 별 정렬 선택", "V37E52E",
                    "V37 ENTR 52 ENTR: P52(IMU 재정렬)를 선택한다. Apollo 11은 000:40~54에 처음 수행했다(옵션 3, 별 30 Menkent와 37 Nunki). " +
                    "시뮬레이터는 P52를 지원하지 않아 OPR ERR이 난다.",
                    "발사 때의 짐벌 기준 오차를 지우고 IMU 자세를 별 기준으로 다시 맞춰 TLI 전에 정확한 방향을 확보하기 위해서다.",
                    "컴퓨터가 정렬 옵션(N06), 별 코드(N70/N71)를 차례로 묻는다. 별 두 개를 마크하면 N05 별 각도 차 0.01도가 확인되고 " +
                    "PRO로 자이로 토크(N93)가 실행된다.",
                    false, general),
                Observe(
                    "N05 별 각도 차와 N93 토크",
                    "Armstrong이 000:47:24에 별 각도 차를 '0.01'로 읽고, 000:47:48~48:14에 N93의 Z 토크 plus 0.152를 확인한 뒤 'Proceed, torque.'로 토크했다.",
                    "별 측정 오차(N05)가 충분히 작은지, 필요한 자이로 토크(N93)가 타당한지 승인하기 위해서다.",
                    "PRO를 누르면 CMC가 자이로 토크로 IMU를 정렬한다. 이어 세 번째 별(34 Atria)로 점검하고 V37E로 종료한다.",
                    confirmed),
                Ground(
                    "상태벡터 업링크",
                    "001:43:57 McCandless: 'If you can give us P00 and Accept, we have a state vector update for you.' " +
                    "승무원은 P00에서 UPLINK를 ACCEPT 위치로 둔다. 001:47:06에 업링크가 끝나 컴퓨터가 돌아온다.",
                    "지상의 정밀 추적 결과로 컴퓨터 안의 위치/속도(상태벡터)를 갱신하기 위해서다.",
                    "UPLINK ACTY 램프가 켜지고 상태벡터가 새 값으로 바뀌어 이후 TLI와 항법 계산의 기준이 정확해진다.",
                    confirmed),
            ],
            "3");

        yield return Guide(
            "TLI 점화",
            "MET 002:44:16~18(MR 2:44:16, AFJ 녹취 2:44:18)에 S-IVB의 J-2 엔진이 재점화되어 약 5분 47초 동안 달 전이 궤도로 가속한다. " +
            "연소는 새턴 IU가 유도하며 승무원은 FDAI, EMS, N62로 감시한다.",
            [
                "CMC는 TLI를 감시한다. CMC 표시는 자세와 속도가 기준 궤적과 정확히 일치했다(MR).",
                "시뮬레이터는 이 단계를 P15로 표시하지만, Apollo 11 CMC가 P15를 썼는지는 확인하지 못했다(Comanche 055 소스에 P15 파일이 없다).",
            ],
            [
                Dsky(
                    "DAP 데이터 로드와 랑데부 표시", "V48E",
                    "V48 ENTR: DAP 데이터(R03)를 로드한다. Aldrin은 002:12:15에 'Verb 48 in, Verb 83, ORDEAL set, Sequence Pyro Arm'이라고 보고했다. " +
                    "시뮬레이터는 V48/V83을 지원하지 않는다.",
                    "연소와 이후 분리·전이에서 쓸 자세 제어(DAP) 설정을 컴퓨터에 넣고 시스템 상태를 확인하기 위해서다.",
                    "DAP가 새 설정으로 초기화된다. V83은 랑데부 파라미터(R31)를 표시한다.",
                    false, confirmed),
                Crew(
                    "유도원 확인과 파이로 무장",
                    "002:12:53 'Guidance, IU?' 'IU, yes.'로 유도원이 IU임을 확인하고, ORDEAL 설정과 SEQUENCE PYRO ARM을 한다.",
                    "TLI 연소를 새턴 IU가 유도하며 분리·점화용 파이로가 작동 가능한 상태임을 확인하기 위해서다.",
                    "점화 후 IU가 연소를 유도한다. CMC는 감시만 한다.",
                    confirmed),
                Dsky(
                    "TLI 프로그램 선택", "V37E15E",
                    "V37 ENTR 15 ENTR: P15(TLI 시작/종료)를 선택한다. Colossus 3 시대 절차(E2448)이며 Apollo 11 CMC에서 실제로 쓰였는지는 미확인이다.",
                    "TLI 연소 감시를 위한 프로그램을 선택해 연소 중 정보를 표시하기 위해서다.",
                    "PROG 창이 15로 바뀐다. 시뮬레이터에서는 이 선택이 수락된다.",
                    true, general),
                Dsky(
                    "N62 모니터", "V16N62E",
                    "V16 N62 ENTR: 속도(VI), 고도변화율, 고도를 계속 표시한다. 종료 때 McCandless가 'we copy the numbers in Noun 62'로 지상이 값을 받아 적었다.",
                    "연소 중 속도 증가를 감시하고 종료 속도를 지상과 대조하기 위해서다.",
                    "종료 시 VI 35,579 ft/s(EMS 차 +3.3 ft/s)처럼 값이 기록되어 정상 연소임을 확인한다.",
                    true, confirmed),
            ],
            "4");

        yield return Guide(
            "TLI 종료",
            "MET 약 002:50:03~06에 S-IVB가 종료한다. 속도는 약 10.8 km/s(35,570 ft/s), 고도 177 n.mi.이며 우주선은 달 전이 궤도에 들어선다. " +
            "이후 CSM은 S-IVB에서 분리되어 LM과 도킹한다.",
            [
                "CMC는 N62로 종료 속도와 고도를 표시해 승무원과 지상이 확인하게 한다.",
                "분리와 도킹 중에는 CSM RCS 디지털 자동조종(DAP)이 자세를 제어한다(V48로 초기화).",
            ],
            [
                Observe(
                    "종료 값 읽기",
                    "002:50:06 'We have cut-off' 35,570 ft/s, 177 n.mi., 002:51:21 Aldrin: 'VI of 35579 and the EMS was plus 3.3.'",
                    "TLI가 목표 속도에 도달했는지 CMC와 EMS 두 가지 값으로 교차 확인하기 위해서다.",
                    "EMS의 전방 속도 오차 3.3 ft/s 이내로 정상 판정되어 지상이 달 전이 궤적을 확정한다.",
                    confirmed),
                Ground(
                    "N62 값 보고",
                    "002:50:35 McCandless: 'we copy the numbers in Noun 62.'",
                    "지상이 종료 상태를 자체 추적과 비교해 다음 궤도 수정이 필요한지 판단하기 위해서다.",
                    "값이 목표와 맞아 이후 MCC 계획의 기준이 된다.",
                    confirmed),
                Dsky(
                    "N62 값 확인", "V16N62E",
                    "V16 N62 ENTR: 종료 직후 속도와 고도를 표시한다. 시뮬레이터의 이 단계 속도는 약 10,830 m/s다.",
                    "종료 속도를 승무원이 직접 읽어 지상에 보고하기 위해서다.",
                    "N62 화면으로 읽은 값이 지상과 대조된다.",
                    true, confirmed),
                Dsky(
                    "P00(대기)으로", "V37E00E",
                    "V37 ENTR 00 ENTR: 연소 감시를 끝내고 프로그램 00으로 돌아간다(일반 절차).",
                    "TLI 감시 프로그램이 끝났으므로 컴퓨터를 대기 상태로 돌려 다음 작업(분리/도킹 DAP)을 받기 위해서다.",
                    "PROG 창이 00이 된다.",
                    true, general),
            ],
            "4");
    }

    // ------------------------------------------------------------------ 달 전이 ~ 달 궤도 ~ LM 분리

    private static IEnumerable<PhaseGuide> TranslunarAndLunarOrbit()
    {
        const GuideConfidence confirmed = GuideConfidence.Confirmed;
        const GuideConfidence partial = GuideConfidence.Partial;
        const GuideConfidence general = GuideConfidence.General;

        yield return Guide(
            "달로 향하는 비행",
            "약 3일 동안 도킹한 CSM+LM이 달을 향해 관성 비행한다. 육분의로 항법 조준(P23), IMU 정렬(P52), 수동 열 제어(PTC) 롤을 하고 " +
            "중간 수정은 MCC-2 한 번만 수행했다(MET 026:44:57.92, SPS 약 2.9초, 20.9 ft/s).",
            [
                "상태벡터를 적분하고 지상 업링크(P27)를 수락한다.",
                "P23(지구-달 항법), P52(IMU 정렬)로 위치와 자세 오차를 줄인다.",
                "MCC-2에서는 P30(ΔV 입력)과 P40(SPS 연소 유도)이 연소를 수행했다.",
            ],
            [
                Dsky(
                    "항법 조준 선택", "V37E23E",
                    "V37 ENTR 23 ENTR: P23(지구-달 항법)을 선택한다. 육분의로 별과 지구 수평선 사이를 측정해 MARK한다. " +
                    "Apollo 11은 006:09:31 등에 사용했다(절차는 Colossus 3 일반 절차). 시뮬레이터는 P23을 지원하지 않는다.",
                    "우주선 위치를 지구 쪽 측정으로 보정해 지상 추적 없이도 궤적 정확도를 확인하기 위해서다.",
                    "측정이 쌓이면 N49(ΔR, ΔV) 갱신량이 나오고 승무원이 PRO로 받아들이거나 거부한다.",
                    false, general),
                Dsky(
                    "IMU 정렬 선택", "V37E52E",
                    "V37 ENTR 52 ENTR: P52를 선택한다. Apollo 11은 024:25:08 등에 정렬했다. 시뮬레이터는 P52를 지원하지 않는다.",
                    "IMU 자세 기준이 시간이 지나며 어긋나므로 별로 다시 맞춰 정확한 연소 자세를 확보하기 위해서다.",
                    "별 각도 차와 자이로 토크가 표시되고 PRO로 토크하면 IMU가 정렬된다.",
                    false, general),
                Ground(
                    "업링크와 MCC 판단",
                    "지상이 'go P00 and Accept'로 업링크를 보낸다. MCC-1은 취소, MCC-2만 수행(ΔV 20.9 ft/s), MCC-4는 069:18에 불필요로 확정.",
                    "상태벡터를 최신으로 유지하고 궤적이 이미 충분히 좋으면 연소를 생략해 연료를 아끼기 위해서다.",
                    "정확한 상태로 이후 LOI 준비 값(LOI-1 상태벡터, 착륙지점 REFSMMAT)이 준비된다.",
                    confirmed),
                Crew(
                    "수동 열 제어(PTC)",
                    "008:24:44 지상 'do a P52, option 1 preferred, and establish PTC.' 010:58:19 'PTC is started now.' " +
                    "우주선이 X축 주위로 약 3회/시간 천천히 회전한다. Apollo 11 CMC에는 PTC 전용 프로그램이 없었다.",
                    "한쪽 면만 햇빛에 노출되어 과열/냉각되는 것을 막아 우주선 온도를 고르게 하기 위해서다.",
                    "자세가 천천히 계속 돌며, 연소나 정렬 전에는 PTC를 멈춰 자세를 잡는다.",
                    partial),
                Dsky(
                    "MCC-2 ΔV 입력(P30)", "V37E30E",
                    "V37 ENTR 30 ENTR: P30(외부 ΔV)을 선택해 N33(TIG), N81(ΔV)을 로드한다. 승무원이 이 연소에서 실제로 누른 키는 확인하지 못했다. " +
                    "시뮬레이터는 P30을 지원하지 않는다.",
                    "지상이 계산한 연소 시각과 ΔV를 컴퓨터에 넣어 연소 계획을 준비하기 위해서다.",
                    "컴퓨터가 원지점/근지점/ΔV 크기를 계산해 승무원이 점검하게 한다. 이어 P40에서 점화한다.",
                    false, general),
            ],
            "6");

        yield return Guide(
            "달 접근",
            "MET 약 61시간. 지구에서 멀어지며 속도가 줄어든다(시뮬레이터 약 1,100 m/s). 달 궤도 진입 연소(LOI-1)를 준비해 " +
            "지상이 LOI-1 상태벡터와 착륙지점 REFSMMAT를 업링크하고 P52로 정렬한다.",
            [
                "상태벡터를 적분하며 지상의 LOI-1 상태벡터, 목표 값 업링크(P27)를 받는다.",
                "P52 옵션 1(착륙지점 REFSMMAT) 정렬을 수행한다.",
            ],
            [
                Ground(
                    "MCC-4 취소",
                    "AFJ 11day4-approach: MCC-4는 067:28에 비행 계획에서 삭제, 069:18:26 Evans 'Midcourse number 4 is not required.'",
                    "궤적이 충분히 정확해 연소가 불필요함을 확인해 연료와 위험을 줄이기 위해서다.",
                    "LOI-1에 필요한 궤적 보정 없이 그대로 달 접근을 이어간다.",
                    confirmed),
                Ground(
                    "LOI-1 데이터 업링크",
                    "약 073:20에 지상이 착륙 지점 REFSMMAT, LOI-1 상태벡터, 목표 로드를 P27로 올린다.",
                    "LOI-1은 달 뒤편에서 지상과 통신 없이 수행되므로 필요한 값을 미리 넣어 두기 위해서다.",
                    "컴퓨터가 LOI-1 연소에 필요한 상태와 목표를 갖게 된다.",
                    partial),
                Dsky(
                    "착륙지점 정렬", "V37E52E",
                    "V37 ENTR 52 ENTR: P52 옵션 1(착륙지점 REFSMMAT)로 정렬한다. Apollo 11은 073:42경 수행했다. 시뮬레이터는 P52를 지원하지 않는다.",
                    "이후 LM 착륙과 LOI 연소에서 같은 기준 좌표를 쓰도록 IMU를 맞추기 위해서다.",
                    "별 두 개로 IMU가 정렬되고 REFSMMAT가 확정된다.",
                    false, general),
                Dsky(
                    "시계 확인", "V16N36E",
                    "V16 N36 ENTR: AGC 시계(MET)를 표시한다.",
                    "LOI-1 점화 시각(TIG 075:49:50)까지 남은 시간을 컴퓨터 시계로 확인하기 위해서다.",
                    "화면에 시·분·초가 나온다.",
                    true, confirmed),
            ],
            "6");

        yield return Guide(
            "달 중력권 진입",
            "MET 약 61:39:50에 달의 중력이 지구보다 커지는 영역(영향권)에 들어선다. 컴퓨터는 기준 천체를 지구에서 달로 바꾸고 " +
            "상태벡터를 달 기준으로 갱신한다. 시뮬레이터 속도는 약 910 m/s다.",
            [
                "항법 상태벡터의 기준 천체가 지구에서 달로 바뀌어 계산이 달 중심 좌표로 이어진다.",
                "달 궤도 진입 연소에 필요한 정보를 계속 갱신하고 지상 업링크를 받는다.",
            ],
            [
                Observe(
                    "기준 천체 변화",
                    "시뮬레이터에서 지구에서 달 중심으로 속도 기준이 바뀌는 것을 본다. 이 구간의 승무원 DSKY 조작은 자료에 없다.",
                    "지구 중력 기준 계산에서 달 기준 계산으로 넘어가는 시점을 이해하기 위해서다.",
                    "이후 속도와 고도가 달 기준으로 표시된다.",
                    partial),
                Dsky(
                    "시계 확인", "V16N36E",
                    "V16 N36 ENTR: AGC 시계(MET)를 표시한다.",
                    "LOI-1(MET 75:49:50)까지 남은 시간을 컴퓨터 시계로 확인하기 위해서다.",
                    "화면에 시·분·초가 나온다.",
                    true, confirmed),
            ],
            "6");

        yield return Guide(
            "달 궤도 진입 연소(LOI-1)",
            "MET 075:49:50에 SPS(20,500 lbf)가 357.5초(약 6분 2초) 점화해 ΔV 2,917.3 fps로 달 궤도(169.9 × 60.9 mi)에 포획된다. " +
            "연소는 달 뒤편에서 통신 두절(LOS 075:41:23) 중에 수행했다.",
            [
                "P30으로 TIG와 ΔV를 입력받아 원지점/근지점을 점검하고, P40이 자세 기동(R60), 짐벌 구동 시험, 울리지, 점화 허가, 연소 조종을 한다.",
                "교차곱 조종으로 필요 속도(VG)를 0으로 줄이다 자동 컷오프한다.",
                "TIG-35초 부근에서 평균 중력 계산을 시작하고, 연소 후 잔류 속도를 표시한다.",
            ],
            [
                Dsky(
                    "TIG/ΔV 입력(P30)", "V37E30E",
                    "V37 ENTR 30 ENTR: P30을 선택해 N33(TIG), N81(ΔV)을 로드한다(Comanche 055 소스의 흐름). " +
                    "Apollo 11 승무원이 이 연소에서 실제로 누른 키는 확인하지 못했다. 시뮬레이터는 P30을 지원하지 않는다.",
                    "패드로 받은 연소 시각과 ΔV를 컴퓨터에 넣어 연소 계획을 확정하기 위해서다.",
                    "컴퓨터가 HA/HP, ΔV 크기를 계산해 예상 궤도(예상 HA 169.2/HP 61.0)와 비교하게 한다.",
                    false, general),
                Dsky(
                    "SPS 연소 프로그램 선택", "V37E40E",
                    "V37 ENTR 40 ENTR: P40(SPS 연소)을 선택한다. Collins는 LOI-2(079:50:35)에서 'Verb 37, Enter. 40, Enter.'라고 했다.",
                    "점화 자세 기동, SPS 짐벌 점검, 점화 카운트다운, 연소 조종을 컴퓨터가 수행하도록 하기 위해서다.",
                    "PROG 창이 40이 된다. 컴퓨터가 R60으로 연소 자세로 기동하고 N40을 표시한다.",
                    true, confirmed),
                Observe(
                    "TIG-35초 공백과 N40",
                    "075:49:15경 DSKY가 약 5초 공백이 된 뒤 N40(점화까지 시간, VG, 누적 ΔV)이 복귀한다.",
                    "공백은 컴퓨터가 평균 중력 계산을 시작하는 신호이므로 정상 진행 여부를 확인하기 위해서다.",
                    "공백이 없거나 N40이 안 돌아오면 연소 준비에 이상이 있음을 알 수 있다.",
                    confirmed),
                Dsky(
                    "엔진 점화 허가", "P",
                    "TIG-5초에 V/N 창에 99가 점멸하면 PRO 키를 누른다. Collins '075:49:46 I'll proceed on the 99.' " +
                    "시뮬레이터에는 V99 점멸 응답이 없어 이 키는 실행하지 않는다.",
                    "승무원이 점화를 마지막으로 허가하는 안전 단계다.",
                    "허가를 받으면 점화(075:49:51 'Burning')하고, 허가하지 않으면 점화하지 않는다.",
                    false, confirmed),
                Dsky(
                    "연소 후 궤도 확인", "V82E",
                    "V82 ENTR: 연소 후 궤도 원지점/근지점을 표시한다(AFJ 075:59:43). N85로 잔류 속도(-1, -1, +1)도 읽는다. " +
                    "시뮬레이터는 V82를 지원하지 않는다.",
                    "연소 결과 궤도가 목표(169.9 × 60.9 mi)에 맞는지 확인하고 잔류를 줄일지 판단하기 위해서다.",
                    "결과가 맞으면 V66(LM 슬롯에 상태벡터 복사), V83, V48을 이어 점검한다.",
                    false, confirmed),
            ],
            "7");

        yield return Guide(
            "LM 분리",
            "MET 약 100:12(MR 표 100:12:00, AFJ 100:12:03/100:18:01로 시각이 다르다)에 이글(LM)이 컬럼비아(CSM)에서 분리된다. " +
            "이 시점부터 DSKY는 착륙선 컴퓨터(LGC)를 가리킨다. LM IMU는 분리 전에 CSM 플랫폼을 기준으로 정렬한다.",
            [
                "CSM CMC는 분리 후 CSM을 이끌어 40 ft 점검 거리를 유지하고 3-임펄스 분리 시퀀스(실제 연소 9초, ΔV 2.7 ft/s)를 수행한다.",
                "LGC(Luminary 099)는 LM의 유도·항법·제어를 맡아 DOI(하강궤도진입)와 PDI(동력 하강)를 준비한다.",
            ],
            [
                Observe(
                    "LM IMU 정렬 기준",
                    "분리 직전 LM IMU는 CSM 플랫폼을 기준으로 정렬된다(MR 9.6.2). 도킹 인터페이스의 오정렬 2.05도가 보정된다.",
                    "LM 정렬은 CSM 정렬을 기준으로 삼아 두 우주선의 좌표를 맞추기 위해서다.",
                    "정렬이 틀리면 이후 DOI·PDI 자세 기준이 어긋난다.",
                    confirmed),
                Dsky(
                    "LM IMU 정렬", "V37E52E",
                    "V37 ENTR 52 ENTR: LGC에서 P52(IMU 정렬)를 선택한다. 시뮬레이터는 P52를 지원하지 않는다. " +
                    "Apollo 11은 101:47경에도 P52를 수행했다.",
                    "LM의 IMU를 별로 정렬해 PDI 자세 기준을 확보하기 위해서다.",
                    "정렬이 끝나면 LM IMU가 착륙용 좌표계를 갖는다.",
                    false, partial),
                Dsky(
                    "시계 확인", "V16N36E",
                    "V16 N36 ENTR: LGC 시계(MET)를 표시한다. 분리 이후 DSKY는 착륙선 컴퓨터다.",
                    "분리 이후 PDI(MET 102:33:05)까지 시각을 확인하기 위해서다.",
                    "화면에 시·분·초가 나온다.",
                    true, confirmed),
                Ground(
                    "분리 후 점검",
                    "MR 4.9: 분리 시 LM에 준 속도 0.4 ft/s, CSM은 40 ft 점검 거리를 유지한다. 이후 DOI(TIG 101:36:14.07)로 근월점을 낮춘다.",
                    "분리 직후 LM 상태를 확인하고 하강 궤도를 만들기 위해서다.",
                    "DOI가 끝나면 PDI(동력 하강)로 이어진다.",
                    partial),
            ],
            "8");
    }

    // ------------------------------------------------------------------ 동력 하강 (가장 상세)

    private static IEnumerable<PhaseGuide> PoweredDescent()
    {
        const GuideConfidence confirmed = GuideConfidence.Confirmed;
        const GuideConfidence partial = GuideConfidence.Partial;

        yield return Guide(
            "동력 하강 시작(PDI)",
            "MET 102:33:05(자료마다 1~2초 차)에 Eagle의 하강 엔진(DPS)이 10% 스로틀로 점화되어 약 5,560 ft/s(약 50,000 ft)의 " +
            "궤도 속도를 깎는 제동 단계(P63)가 시작된다. 약 26초 뒤 스로틀이 약 94%(9,870 lb)로 올라간다. " +
            "점화 직전 랑데부 레이더(RR) 모드 스위치가 LGC 위치에 있어 레이더가 컴퓨터 시간을 소모했고, 그 결과 102:38:26부터 " +
            "1202/1201 알람이 반복되었다.",
            [
                "LGC가 제동 단계 유도 방정식으로 추력 방향과 크기를 계산하고 DAP가 RCS와 스로틀을 자동으로 제어한다.",
                "평균 G(SERVICER) 작업이 2초마다 IMU 가속도를 적분해 상태벡터를 갱신한다. 착륙레이더(LR) 데이터는 " +
                "승무원과 지상의 승인(V57)이 있어야 상태벡터에 반영된다.",
                "N63(속도/고도변화율/계산 고도), N68(사거리/제동 잔여시간/DELTAH)을 표시해 승무원이 점검하게 한다.",
                "과부하 시 BAILOUT으로 소프트 재시작을 하고 항법/유도/제어를 유지한다(알람 1202/1201).",
            ],
            [
                Crew(
                    "랑데부 레이더 전원/모드",
                    "PDI 직전 RR 모드 스위치를 LGC 위치로 두고 RR 전원을 켠다('RR switched on just before P63 was selected'). " +
                    "102:06:24 'Let's get the Rendezvous Radar circuit breaker in.'",
                    "하강 중 LGC가 레이더를 쓸 수 있게 하고 중단(abort) 시 랑데부에 대비하기 위해서다.",
                    "RR CDU가 LGC 카운터를 거의 끊임없이 증감시켜(최대 6,400 pulse/s) 컴퓨터 시간의 약 15%를 소모한다. " +
                    "이것이 1202/1201 알람의 직접 원인이 되었다.",
                    confirmed),
                Dsky(
                    "제동 단계 프로그램 선택", "V37E63E",
                    "V37 ENTR 63 ENTR: 프로그램 P63을 선택한다(Eyles: 'Verb 37 Noun 63'). 시뮬레이터에서는 PDI 단계가 이미 P63이라 그대로 수락된다.",
                    "달 착륙 제동 단계(수평 속도를 줄이는 것이 유일한 목적) 유도를 시작하도록 컴퓨터에 알리기 위해서다.",
                    "컴퓨터가 N62 점화 카운트다운을 표시하고 점화 후 N63으로 바뀐다. TIG-35초에 평균 G를 시작하고 -7.5초에 울리지를 한다.",
                    true, partial),
                Dsky(
                    "엔진 점화 허가", "P",
                    "점화 5초 전(T-5초) 표시가 점멸하며 허가를 요청하고 Aldrin이 PRO(PROCEED)를 눌렀다. " +
                    "ALSJ 102:33:05~08 'Proceed. One, Zero.' 점멸 표시의 V/N은 P63에서는 자료로 확인되지 않아(V99는 P40 소스에서 확인) " +
                    "시뮬레이터는 102:33:00에 V99를 점멸시키고 PRO로 점멸이 멈춘다(이 키는 점멸 중에 " +
                    "직접 눌러야 하므로 [실행] 버튼은 없다). 카운트다운 N62 표시 시작(102:32:05)은 근사값이다.",
                    "점화를 승무원이 마지막으로 허가하는 안전 단계다.",
                    "허가 후 하강 엔진이 10% 스로틀로 점화되고 약 26초 뒤 약 94%로 올라간다.",
                    false, partial),
                Dsky(
                    "N63 속도/고도 모니터", "V16N63E",
                    "V16 N63 ENTR: LGC에서 R1=속도 |V|(ft/s), R2=고도변화율(ft/s), R3=계산 고도(ft)를 표시한다. " +
                    "점화 직후 Eyles 기록은 5559.7 ft/s, -2.2 ft/s, 49,971 ft였다.",
                    "하강 중 속도 감소와 고도 변화가 계획과 맞는지 승무원이 계속 확인하기 위해서다.",
                    "화면에 N63이 나온다. 시뮬레이터 값은 키프레임 보간값이라 실제와 약간 다르다.",
                    true, confirmed),
                Crew(
                    "자세 조종 감도(rate switch)",
                    "102:36:55 Armstrong이 rate switch를 5에서 25 deg/s로 바꾼다.",
                    "접근 단계에서 더 빠른 자세 조종이 가능하도록 감도를 높이기 위해서다.",
                    "수동 조종 입력에 대한 자세 회전 속도 한도가 커진다.",
                    confirmed),
                Dsky(
                    "DELTAH(레이더/계산 고도 차) 감시", "V16N68E",
                    "V16 N68 ENTR: R1=착륙점까지 사거리(nm), R2=제동 잔여시간(분/초), R3=LR 고도-계산 고도(DELTAH, ft). " +
                    "Aldrin이 102:38 부근에 이 화면을 띄워 'Delta-H is minus 2,900'이라고 읽었다. 시뮬레이터는 R3(DELTAH)을 항상 0으로 둔다.",
                    "레이더 고도와 컴퓨터 계산 고도의 차를 보고, 지상 승인(V57) 뒤 레이더 데이터를 항법이 받아들이는지 확인하기 위해서다.",
                    "차이가 줄어 약 900 ft가 된다. 알람(재시작)이 일어나면 이 모니터 표시는 사라진다(재시작 보호가 없는 기능).",
                    true, confirmed),
            ],
            "10");

        yield return Guide(
            "접근 단계(P64)",
            "MET 102:41:32 하이 게이트(고도 약 7,400 ft, 하강율 125 ft/s)에서 컴퓨터가 P63에서 P64(Eyles: 가시 단계)로 자동 전환한다. " +
            "Armstrong은 창밖 착륙점을 보고 LPD 각도를 읽어 지점을 조정한다. 이 구간에서 1201/1202 알람이 계속된다" +
            "(102:38:26, 102:39:02 1202; 102:42:17 1201; 이어서 +24초, +16초 1202).",
            [
                "P64가 자동 시작되어 N64(재지정 잔여시간/LPD 각도, 고도변화율, 계산 고도)를 표시한다.",
                "Executive가 core set(작업 공간)이나 VAC 영역이 부족하면 알람 1202/1201을 올리고 BAILOUT으로 소프트 재시작한다.",
                "재시작 보호가 된 항법·유도·제어는 즉시 복구되고 보호되지 않은 표시(V16N68 모니터)는 사라진다.",
            ],
            [
                Observe(
                    "하이 게이트와 PROG 램프",
                    "102:41:32 하이 게이트에서 P64로 자동 전환되고 102:41:35 Armstrong이 'P64.'라고 확인했다. " +
                    "알람이 나면 DSKY의 PROG 램프가 켜지고 Armstrong이 'Program Alarm. It's a 1202.'(102:38:26~30)라고 보고했다.",
                    "프로그램 전환 시점과 알람 발생을 즉시 알아채 대응하기 위해서다.",
                    "PROG 램프가 켜지면 알람 코드를 읽어 판단해야 한다. 알람 코드는 V05 N09로 읽는다.",
                    confirmed),
                Dsky(
                    "알람 코드 읽기", "V05N09E",
                    "V05 N09 ENTR: 최근 알람 코드를 표시한다. 시뮬레이터에서는 알람 이후 R1이 8진 코드(01202/01201)로 나오고, " +
                    "최근 순서로 최대 3개가 R1/R2/R3에 나온다.",
                    "어떤 알람인지(1202=core set 부족, 1201=VAC 영역 부족) 정확히 확인해 계속 진행해도 되는지 지상과 판단하기 위해서다.",
                    "화면에 코드가 나온다. 1202/1201은 컴퓨터가 과부하로 일부 작업을 버리고 재시작했다는 뜻이다.",
                    true, confirmed),
                Ground(
                    "휴스턴 'Go' 판정",
                    "Armstrong이 코드를 보고하면 지상 유도 담당(Bales)이 위협이 아니라고 판단하고 Duke가 'We're Go.'(약 102:42:25)라고 한다. " +
                    "MIT가 재시작 능력을 시험해 두었기 때문에 가능했다.",
                    "알람이 있어도 항법·유도가 유지되는지 지상이 판단해 착륙을 중단(abort)할지 계속할지 결정하기 위해서다.",
                    "계속 진행(Go)이면 승무원은 착륙에 집중한다. 알람 간격 기준 같은 구체적 규칙은 자료에 없다.",
                    partial),
                Dsky(
                    "PROG 램프 끄기", "R",
                    "RSET(R) 키를 눌러 PROG 램프를 끈다. 알람이 다시 나면 램프가 다시 켜진다.",
                    "알람 램프를 지워 새 알람이 발생하면 곧바로 알아볼 수 있게 하기 위해서다.",
                    "램프가 꺼진다. 컴퓨터 동작은 바뀌지 않는다.",
                    true, confirmed),
                Observe(
                    "왜 재시작했는가(Executive/BAILOUT)",
                    "RR 레이더의 가짜 카운터 증감으로 컴퓨터 시간의 약 13~15%가 소모되어 정규 작업이 끝나기 전에 새 작업이 계속 예약되었다.",
                    "알람이 하드웨어 설정(RR 스위치)에서 비롯된 과부하임을 이해하기 위해서다.",
                    "BAILOUT이 소프트 재시작을 일으켜 가장 최근 SERVICER 작업만 다시 만들고 나머지는 버렸다. 착륙은 계속된다.",
                    confirmed),
                Observe(
                    "LPD로 착륙점 조정",
                    "102:42:32 'Give me an LPD.' Aldrin이 N64 R1의 LPD 각도를 읽어 주고 Armstrong이 창문 눈금으로 착륙점을 지정한다.",
                    "목표 지점이 바위 지대일 때 더 안전한 지점을 고르기 위해서다.",
                    "착륙점이 바뀌어 접근 경로가 조정된다.",
                    confirmed),
            ],
            "10");

        yield return Guide(
            "수동 조종 착륙(P66)",
            "MET 102:43:08(고도 650 ft)에 Armstrong이 자세 모드를 AUTO에서 ATT HOLD로 바꾸고, 102:43:20(고도 430 ft)에 " +
            "P66으로 들어가 ROD 스위치로 하강율을 수동 제어한다. 연료가 줄어든 가운데(102:44:45 'Five percent') " +
            "102:45:40에 접촉(Contact Light)한다.",
            [
                "P66은 하강율(ROD) 모드로 컴퓨터가 수직 속도 유지를 맡고 승무원이 하강율을 조절한다.",
                "수평 속도는 Armstrong이 수동으로 0으로 만들고 DAP가 자세를 유지한다.",
                "서보서/알람 감시는 계속되며 마지막 1202(770 ft, 27 ft/s)가 이 구간 직전에 있었다.",
            ],
            [
                Crew(
                    "AUTO에서 ATT HOLD",
                    "102:43:08 고도 650 ft에서 Armstrong이 자세 모드 스위치를 AUTO에서 ATT HOLD로 바꾼다.",
                    "자동 유도가 정한 착륙점 대신 승무원이 수동으로 자세를 잡아 바위 지대를 피하기 위해서다.",
                    "DAP가 자세를 유지하고 Armstrong의 조종기 입력이 직접 자세를 바꾼다.",
                    confirmed),
                Crew(
                    "ROD 스위치로 하강율 조절",
                    "102:43:20 고도 430 ft에서 P66에 진입한다. DSKY가 아니라 스프링 복귀형 ROD 스위치로 하강율을 조절한다(ALSJ 102:43:22).",
                    "승무원이 수평 이동에 집중하는 동안 하강율 조절만 스위치로 간단히 하기 위해서다.",
                    "ROD 스위치 조작에 따라 목표 하강율이 바뀌고 컴퓨터가 이를 따르도록 스로틀을 조절한다.",
                    confirmed),
                Dsky(
                    "남은 속도/고도 확인", "V16N63E",
                    "V16 N63 ENTR: 속도 |V|, 고도변화율, 계산 고도를 표시한다. 시뮬레이터 P66 구간은 고도 131 m, 20 m/s 부근이다.",
                    "착륙 직전 고도와 하강율을 확인하기 위해서다. 실제 P66 중 승무원이 어떤 노운을 봤는지는 이 문서의 자료로는 확정하지 못했다.",
                    "화면에 N63이 나온다.",
                    true, partial),
                Observe(
                    "연료 경고",
                    "102:44:31 연료 Quantity 램프가 켜지고 102:44:45 'Five percent'라는 호출이 나온다.",
                    "남은 연료로 착륙 가능한지 판단하기 위해서다.",
                    "연료가 모자라면 중단해야 하므로 승무원은 빨리 착지점을 정해야 한다.",
                    confirmed),
                Observe(
                    "접촉",
                    "102:45:40 Contact Light가 켜지고 102:45:43 'Shutdown', 102:45:44 'Okay. Engine Stop.'",
                    "접촉등은 탐침이 지면에 닿았다는 신호이므로 엔진을 끄기 위해서다.",
                    "엔진이 정지하고 착륙 확인 프로그램 P68이 이어진다.",
                    confirmed),
            ],
            "10");

        yield return Guide(
            "착륙 — 고요의 바다",
            "MET 102:45:40에 Eagle이 고요의 바다(0.67°N, 23.47°E)에 착륙한다. 'Houston, Tranquility Base here. The Eagle has landed.' " +
            "AGC는 P68(착륙 확인)을 수행해 위치를 확정한다. 약 21.6시간의 표면 체류가 시작된다.",
            [
                "P68(LANDING CONFIRMATION)이 착륙 지점의 위치(위도/경도/고도)를 컴퓨터에 확정한다. 이때 DAP가 한 번 순환하고 접촉 시 DAP는 ATT HOLD이다.",
                "착륙 후 PGNS 상태를 기준으로 AGS를 정렬하고 상태를 점검한다.",
            ],
            [
                Crew(
                    "엔진 정지와 스위치 정리",
                    "102:45:44 'Engine Stop', 102:45:46 'Out of Detent. Auto.', 102:45:47 'Mode Control, both Auto. Descent Engine Command Override, Off. Engine Arm, Off. 413 is in.'",
                    "엔진을 안전하게 끄고 자세 모드를 자동으로 돌려 착륙 상태를 정리하기 위해서다.",
                    "엔진이 정지하고 모든 스위치가 안전한 상태가 된다.",
                    confirmed),
                Crew(
                    "AGS 입력(DEDA)",
                    "DEDA에서 'Clear, 413, Enter, +10000, Enter'를 입력한다(ALSJ). DSKY가 아니라 AGS의 DEDA 키패드 조작이다. 413의 정확한 의미는 부분 확인이다.",
                    "착륙 후 AGS(백업 유도 컴퓨터)를 착륙 상태로 설정하기 위해서다.",
                    "AGS가 착륙 상태 값을 갖는다. 이후 AGS를 PGNS에 정렬한다.",
                    partial),
                Dsky(
                    "착륙점 위치 확인", "V06N43E",
                    "V06 N43 ENTR: PGNS가 계산한 착륙 지점의 위도(R1), 경도(R2), 고도(R3)를 표시한다. " +
                    "Aldrin은 102:48:10에 'Noun 43'을 불러 지상이 받아 적었다.",
                    "착륙 지점의 위치를 알면 지상이 이후 이륙과 랑데부 계산의 기준으로 쓸 수 있다.",
                    "화면에 위도 0.67, 경도 23.47(×100 형식)이 나온다.",
                    true, confirmed),
                Ground(
                    "AGS를 PGNS에 정렬",
                    "102:50:59 '(How would you) like AGS to PGNS align?' 103:15:09 Duke 'You are Go for the AGS to PGNS align, and then the lunar align.'",
                    "두 유도 컴퓨터의 기준을 맞춰 한쪽이 고장 나도 이륙에 쓸 수 있게 하기 위해서다.",
                    "AGS가 PGNS와 같은 기준으로 정렬되고 이어서 달 표면 P57 정렬을 한다.",
                    confirmed),
            ],
            "10");
    }

    // ------------------------------------------------------------------ 달 표면 ~ 상승 ~ 도킹

    private static IEnumerable<PhaseGuide> SurfaceAndAscent()
    {
        const GuideConfidence confirmed = GuideConfidence.Confirmed;
        const GuideConfidence partial = GuideConfidence.Partial;
        const GuideConfidence general = GuideConfidence.General;

        yield return Guide(
            "달 표면 활동(EVA)",
            "MET 109:24:15경 닐 암스트롱이 달 표면에 첫 발을 내딛는다. 착륙부터 이륙까지 약 21.6시간 동안 LM은 달 표면에 있었고, " +
            "LGC는 IMU를 중력/별로 정렬(P57)하고 전원을 절감했다가 이륙 준비 때 다시 켠다.",
            [
                "LGC는 P57로 달 표면에서 IMU를 중력과 별을 이용해 다시 정렬한다.",
                "지상 업링크(UPDATA LINK DATA)로 시계와 상태를 갱신한다.",
                "전원 절감(powerdown) 동안 컴퓨터를 꺼 두었다가 이륙 준비 때 다시 켠다.",
            ],
            [
                Dsky(
                    "달 표면 IMU 정렬(P57)", "V37E57E",
                    "V37 ENTR 57 ENTR: P57(LUNAR SURFACE IMU ALIGNMENT)을 선택한다. 이 문서는 일반 문법만 확인했고 승무원이 실제로 누른 키는 [미확인]이다. " +
                    "시뮬레이터는 P57을 지원하지 않는다.",
                    "달 표면 중력 방향과 별 방향으로 IMU 자세를 맞춰 이륙 유도의 정확한 기준을 확보하기 위해서다.",
                    "N89(착륙지점), N34, N22(짐벌각), N93(토크각)이 표시되고 정렬이 진행된다. 'VB33'로 거친/정밀 정렬로 진행한다.",
                    false, general),
                Ground(
                    "업링크와 P57 재수행",
                    "103:10:15 Duke 'recall P57 and run through the gravity align one more time.' 103:59:41 Aldrin 'The DSKY's yours, and updata link to Data.' " +
                    "104:03:10 Duke 'We got the load in. You can start your P57.'",
                    "정렬 결과가 불확실해 다시 하고, 지상이 상태 값을 올려 컴퓨터를 최신화하기 위해서다.",
                    "업링크가 끝난 뒤 승무원이 P57을 다시 시작한다.",
                    confirmed),
                Ground(
                    "전원 절감 시작",
                    "104:35:00 Duke 'You can start your powerdown now.'",
                    "휴식 시간 동안 전력을 절약하기 위해서다.",
                    "컴퓨터와 장치 일부가 꺼진다. 이륙 준비 때 다시 켜서 P57을 한다(122:45:52, TIG-50분 기준).",
                    confirmed),
                Dsky(
                    "시계 확인", "V16N36E",
                    "V16 N36 ENTR: LGC 시계(MET)를 표시한다.",
                    "이륙(124:22:00)까지 남은 시간을 컴퓨터 시계로 확인하기 위해서다. 실제 시계 갱신 방법은 확인하지 못했다.",
                    "화면에 시·분·초가 나온다.",
                    true, confirmed),
            ],
            "11");

        yield return Guide(
            "상승단 이륙",
            "MET 124:22:00에 Eagle 상승단이 APS 엔진으로 이륙한다. 하강단은 이륙대로 남는다. LGC는 P12(POWERED ASCENT) 유도로 " +
            "수직 상승, 피치오버(124:22:13~14), 궤도 삽입을 수행한다.",
            [
                "P12가 APS 엔진 유도를 맡아 수직 상승 후 피치오버로 궤도에 삽입한다.",
                "AGS(백업)는 DEDA로 주소 500(VG)을 표시해 PGNS와 비교하게 한다.",
            ],
            [
                Ground(
                    "유도 권고와 이륙 허가",
                    "124:04:51 Evans: 'Our guidance recommendation is PGNS, and you're cleared for takeoff.'",
                    "PGNS와 AGS 중 어느 유도를 쓸지 지상이 권고하고 이륙을 허가하기 위해서다.",
                    "PGNS가 주 유도가 된다.",
                    confirmed),
                Dsky(
                    "상승 프로그램 선택", "V37E12E",
                    "V37 ENTR 12 ENTR: P12(POWERED ASCENT)를 선택한다. P12 시작 시각(약 124:21:33)은 확인되지만 선택 키 입력은 [미확인]이다. " +
                    "일반 키 문법에 따른 예시다.",
                    "APS 점화 전에 컴퓨터를 상승 유도 프로그램으로 바꾸기 위해서다.",
                    "PROG 창이 12가 된다. TIG 카운트다운과 평균 G가 시작된다.",
                    true, general),
                Crew(
                    "점화 스위치 조작",
                    "124:20:58 Master Arm On. 124:21:15 Armstrong 'At five seconds I'm gonna get Abort Stage and Engine Arm.' " +
                    "124:21:54 'Abort Stage, Engine Arm, Ascent, Proceed' 후 카운트 9, 8, 7...",
                    "하강단과 분리해 상승 엔진을 점화할 준비를 하기 위해서다.",
                    "점화 시각에 APS가 점화되고 124:22:00 이륙한다. PRO 키는 점화 허가에 해당한다.",
                    confirmed),
                Observe(
                    "수직 상승과 피치오버",
                    "124:22:09 '26, 36 feet per second up', 124:22:13/14 'Pitchover' 'Pitching over. Balance couple, Off.'",
                    "수직으로 올라가 지형을 벗어난 뒤 수평으로 자세를 기울여 궤도로 가속하기 위해서다.",
                    "피치오버 후 궤도 삽입을 향해 가속한다.",
                    confirmed),
            ],
            "12");

        yield return Guide(
            "상승단 궤도 진입",
            "MET 124:29:17에 상승 엔진이 종료한다(수평 5,337.3 ft/s, 수직 32.8 ft/s, 고도 60,666 ft). 계획 궤도는 약 10 × 45 n.mi., " +
            "달성한 궤도는 47.2 × 9.5 n.mi.(124:31:07)이다. 이후 컬럼비아와 랑데부(CSI, CDH, TPI, TPM)를 준비한다.",
            [
                "P12가 컷오프를 수행하고 컷오프 루틴은 V16 N63을 쓴다(소스).",
                "랑데부 레이더(RR)로 CSM을 추적하는 P20을 시작하고 CSI(P32), CDH(P33) 목표를 계산한다.",
            ],
            [
                Observe(
                    "컷오프",
                    "124:29:17 'Shutdown'. 수평 속도 5,337.3 ft/s, 수직 32.8 ft/s, 고도 60,666 ft.",
                    "상승단이 목표 궤도 속도에 도달해 엔진이 종료되었는지 확인하기 위해서다.",
                    "달 궤도에 진입해 랑데부 단계로 넘어간다.",
                    confirmed),
                Dsky(
                    "N63 확인", "V16N63E",
                    "V16 N63 ENTR: 속도, 고도변화율, 고도를 표시한다. P12 컷오프 루틴이 V16N63을 쓰는 것은 소스에서 확인된다. " +
                    "상승 중 N63의 의미는 하강과 다를 수 있다.",
                    "삽입 속도와 고도를 확인하기 위해서다.",
                    "화면에 N63이 나온다.",
                    true, partial),
                Observe(
                    "랑데부 준비",
                    "CSI 125:19:34.7(51.5 ft/s, APS), CDH 126:17:45.58, TPI 127:03:52, 중간수정(TPM) 127:33:50(RCS). 프로그램은 P32~P35와 P20이다. " +
                    "승무원이 누른 키 순서는 녹취에 없다.",
                    "두 우주선의 궤도를 맞춰 도킹 거리까지 접근하기 위해서다.",
                    "각 연소가 순서대로 이어져 도킹으로 끝난다.",
                    partial),
            ],
            "13");

        yield return Guide(
            "도킹",
            "MET 128:03(녹취 128:03:12, AFJ 타임라인 128:03:00)에 이글 상승단이 컬럼비아와 도킹한다. 제동은 127:36:14 '8.2 miles, 40 feet per second'에서 " +
            "수동으로 시작했다. 이후 승무원과 표본이 CM으로 옮겨지고 LM 상승단은 버려진다. 도킹 후 DSKY는 다시 CMC다.",
            [
                "랑데부 중 LGC는 RR로 CSM을 추적했고 CSM의 CMC도 독립적으로 해를 구해 추적했다.",
                "도킹 후에는 CMC DAP가 자세 유지를 맡고 상태벡터를 갱신한다.",
            ],
            [
                Observe(
                    "제동과 도킹",
                    "127:36:14 '8.2 miles, 40 feet per second' 제동, 127:43:02 'let's put P47 in', 128:03:12 'Okay. We're all yours, Columbia.'",
                    "상대 속도를 0으로 줄여 안전하게 접촉하기 위해서다.",
                    "도킹 래치가 걸리면 두 우주선이 하나가 된다. P47 선택의 용도는 확인하지 못했다.",
                    partial),
                Dsky(
                    "시계 확인", "V16N36E",
                    "V16 N36 ENTR: 도킹 후 CMC 시계(MET)를 표시한다. 시뮬레이터에서는 도킹 이후 DSKY가 CSM으로 바뀐다.",
                    "TEI(135:23:41)까지 남은 시간을 확인하기 위해서다.",
                    "화면에 시·분·초가 나온다.",
                    true, confirmed),
                Observe(
                    "LM 투기와 DSKY 조작",
                    "Collins '128:04:29 I went to Retract ... all hell broke loose'. LM 투기 시각과 도킹 직후 DSKY 조작은 읽은 자료에 없다.",
                    "도킹 직후 문제가 있었음을 알리되 근거 자료의 한계를 밝히기 위해서다.",
                    "원인과 경과는 확인하지 못했다.",
                    GuideConfidence.Unconfirmed),
            ],
            "14");
    }

    // ------------------------------------------------------------------ 귀환 ~ 재진입 ~ 착수

    private static IEnumerable<PhaseGuide> ReturnAndEntry()
    {
        const GuideConfidence confirmed = GuideConfidence.Confirmed;
        const GuideConfidence partial = GuideConfidence.Partial;
        const GuideConfidence general = GuideConfidence.General;

        yield return Guide(
            "지구 귀환 연소(TEI)",
            "MET 135:23:42에 달 뒤편에서 SPS를 약 2분 30초 점화해 ΔVT 3,283.6 fps로 달 궤도를 벗어난다(패드 TIG 135:23:41.56, 추진제 약 10,000 lb). " +
            "CMC는 P30으로 TIG/ΔV를 받고 P40이 SPS 연소를 유도한다.",
            [
                "P40이 SPS 추력과 TVC DAP로 연소를 유도하고 컷오프를 자동으로 한다.",
                "EMS ΔV 카운터는 G&N이 고장 날 때를 위한 백업 차단이다.",
                "연소 후 V83으로 온보드 상태벡터와 예측값을 비교한다.",
            ],
            [
                Dsky(
                    "P40 선택", "V37E40E",
                    "V37 ENTR 40 ENTR: Aldrin 135:07:59 'All right. Verb 37, Enter. 40, Enter. Okay.' P40을 선택한다.",
                    "SPS 연소 프로그램을 시작해 점화 자세 기동, 짐벌 점검, 카운트다운을 컴퓨터가 수행하게 하기 위해서다.",
                    "PROG 창이 40이 되고 컴퓨터가 N40을 표시한다.",
                    true, confirmed),
                Crew(
                    "SPS 짐벌 트림",
                    "135:18:40~19:02 GPI 썸휠로 SPS 짐벌 트림(피치 약 -0.6°, 요 약 +0.6°)을 수동 설정한다.",
                    "패드에 적힌 트림으로 엔진 추력 방향이 무게중심을 지나게 하기 위해서다.",
                    "점화 시 추력 방향이 맞아 자세 오차가 작다.",
                    confirmed),
                Observe(
                    "N40 카운트다운",
                    "135:23:08 N40: R1=점화(컷오프)까지 시간, R2=획득해야 할 속도(3,283.6에서 감소), R3=누적 속도(0에서 증가). TIG-35초에 DSKY가 공백이 되며 EMS는 Normal.",
                    "점화 직전과 연소 중 진행 상황을 확인하기 위해서다.",
                    "정상이면 R2가 0으로 줄어들 때 컷오프한다.",
                    confirmed),
                Dsky(
                    "점화 허가", "P",
                    "TIG-5초에 V99가 점멸하면 Collins가 PRO를 눌렀다(AFJ, C055). 시뮬레이터에는 V99 점멸 응답이 없어 이 키는 실행하지 않는다.",
                    "엔진 점화를 승무원이 허가하는 마지막 안전 단계다.",
                    "허가 후 135:23:42에 점화한다.",
                    false, confirmed),
                Crew(
                    "EMS ΔV 스위치 해제",
                    "점화 후 약 2초에 Collins가 EMS ΔV Normal 스위치를 둘 다 OFF로 한다. 컷오프는 컴퓨터가 자동으로 한다.",
                    "EMS 백업 차단이 필요하지 않게 한 것이다(G&N 정상 확인 후).",
                    "EMS 백업 차단이 해제된다.",
                    confirmed),
            ],
            "15");

        yield return Guide(
            "TEI 종료",
            "MET 135:26:12경 TEI 연소가 끝나 지구 귀환 궤도에 진입한다(시뮬레이터 속도 약 2.5 km/s). 연소 결과는 nominal 3,283.5 fps였고 " +
            "Armstrong 보고는 0.7 마일, 0.8 ft/s였다. 연소 후 승무원은 잔류와 궤도를 점검한다.",
            [
                "연소 후 잔류 속도(N85)와 EMS ΔVC를 표시한다.",
                "V83으로 온보드 상태벡터와 예측값을 비교한다.",
            ],
            [
                Observe(
                    "연소 잔류",
                    "'Delta-VGX 0.1, VGY 0.9, VGZ 0.1. Delta-VC minus 17.9'(N85 잔류와 EMS).",
                    "연소가 목표 ΔV에 얼마나 가까운지 확인하고 잔류를 줄일지 판단하기 위해서다.",
                    "잔류가 작아 트림하지 않아도 된다.",
                    confirmed),
                Dsky(
                    "상태벡터 비교", "V83E",
                    "V83 ENTR: 랑데부 파라미터 표시로 온보드 값을 예측값과 비교한다. 시뮬레이터는 V83을 지원하지 않는다.",
                    "연소 결과의 궤도가 지상 예측과 맞는지 확인하기 위해서다.",
                    "Armstrong 보고처럼 0.7 마일, 0.8 ft/s 차이면 정상이다.",
                    false, confirmed),
                Dsky(
                    "승무원 정의 기동", "V49E",
                    "V49 ENTR: R62 승무원 정의 자세 기동을 시작한다. Collins 136:55:39 'Verb 49 maneuver to that attitude is in progress.' " +
                    "시뮬레이터는 V49를 지원하지 않는다.",
                    "연소 후 다음 작업(P52 정렬 등)을 위한 자세로 우주선을 돌리기 위해서다.",
                    "컴퓨터가 자세를 맞춰 기동한다.",
                    false, confirmed),
                Dsky(
                    "P00(대기)으로", "V37E00E",
                    "V37 ENTR 00 ENTR: P40 종료 후 프로그램 00으로 돌아간다(일반 절차).",
                    "연소 프로그램이 끝났으므로 컴퓨터를 대기 상태로 돌리기 위해서다.",
                    "PROG 창이 00이 된다.",
                    true, general),
            ],
            "15");

        yield return Guide(
            "지구로 돌아오는 비행",
            "MET 약 150시간. 지구 중력권으로 되돌아오며 서서히 가속한다. 수동 열 제어(PTC)를 계속하고, 중간 수정은 MCC-5 한 번만 수행한다" +
            "(TIG 150:29:54.53, RCS 4.8 fps, 약 11초).",
            [
                "상태벡터를 유지하고 P52로 정렬하며 PTC 자세 제어를 이어간다.",
                "MCC-5는 SPS가 아니라 RCS 4개 쿼드로 수행하고 울리지는 없다.",
            ],
            [
                Ground(
                    "MCC-5 수행 결정",
                    "144:00 '지금은 MCC-5가 필요 없어 보인다' 했다가 147:30에 수행하기로 한다(TEI+15시간). 148:52:02 패드: 'RCS/G&N: 26025, TIG 150:29:54.53'.",
                    "진입 각도를 허용 범위에 맞추기 위해서다.",
                    "RCS 4.8 fps 연소가 계획된다.",
                    confirmed),
                Crew(
                    "MCC-5 RCS 연소",
                    "150:29:54 RCS 4개 쿼드 연소, ΔVT 4.8 fps, 약 10.5~11초. 150:31:43 Armstrong 'Delta-VC is plus 0.2.'",
                    "진입 회랑을 맞추기 위한 작은 보정이다.",
                    "ΔVC 0.2 fps 오차로 끝나 진입 궤적이 맞춰진다.",
                    confirmed),
                Dsky(
                    "ICDU 각도 표시", "V16N20E",
                    "V16 N20 ENTR: 현재 ICDU 각도를 표시한다. Collins는 이후 재진입 동안 이 화면('16 20 on the DSKY')을 볼 것이라고 했다. " +
                    "시뮬레이터는 N20을 지원하지 않는다.",
                    "우주선 자세를 DSKY로 직접 확인하기 위해서다.",
                    "화면에 롤/피치/요 각도가 나온다.",
                    false, confirmed),
                Dsky(
                    "시계 확인", "V16N36E",
                    "V16 N36 ENTR: AGC 시계(MET)를 표시한다.",
                    "MCC-5와 이후 정렬 시각을 컴퓨터 시계로 확인하기 위해서다.",
                    "화면에 시·분·초가 나온다.",
                    true, confirmed),
                Observe(
                    "PTC 프로그램 없음",
                    "152:49:21 Collins 'Buzz thinks we should have a PTC program built into the computer.'",
                    "PTC가 컴퓨터 프로그램이 아니라 승무원 수동 설정/DAP로 유지되었음을 알기 위해서다.",
                    "승무원이 자세와 회전을 직접 점검해야 한다.",
                    partial),
            ],
            "16");

        yield return Guide(
            "귀환 비행 후반",
            "MET 약 175시간. 지구가 가까워지며 속도가 늘어난다. MCC-6(172시간 예정)은 169:28에 취소, MCC-7은 176:44에 필요 시 0.2 ft/s 정도라 취소해 " +
            "휴식 시간을 한 시간 늘린다. 194:16:46에 지상이 최종 재진입 PAD(상태벡터, 목표, 새 REFSMMAT)를 갱신한다.",
            [
                "상태벡터를 유지하고 지상이 보낸 최종 진입 PAD 데이터를 받는다.",
                "P52로 IMU를 정렬하고(171:04:35 요청) 재진입 자세 기준을 만든다.",
            ],
            [
                Ground(
                    "MCC-6/MCC-7 취소",
                    "169:28 MCC-6 취소(Garriott 171:01:52 'We've cancelled Midcourse number 6.'). 176:44 MCC-7 필요 시 0.2 ft/s 정도라 취소('will add one hour of rest time').",
                    "연소가 필요 없을 만큼 진입 궤적이 이미 정확하기 때문이다.",
                    "추가 연소 없이 재진입 준비로 간다.",
                    confirmed),
                Dsky(
                    "IMU 정렬 요청", "V37E52E",
                    "V37 ENTR 52 ENTR: P52로 정렬한다(171:04:35 Garriott의 P52 요청). 시뮬레이터는 P52를 지원하지 않는다.",
                    "재진입 직전 IMU를 정확히 맞춰 진입 자세 오차를 줄이기 위해서다.",
                    "별 두 개로 IMU가 정렬된다.",
                    false, general),
                Ground(
                    "최종 진입 PAD 갱신",
                    "194:16:46 Evans가 상태벡터, 목표, 새 REFSMMAT를 갱신하고 N60 γ 6.48°, 최대 g 6.3을 낭독한다.",
                    "진입 직전 가장 최신 위치로 컴퓨터를 갱신해 진입 유도 정확도를 높이기 위해서다.",
                    "진입 유도가 갱신된 값으로 계산된다.",
                    confirmed),
                Dsky(
                    "시계 확인", "V16N36E",
                    "V16 N36 ENTR: AGC 시계(MET)를 표시한다.",
                    "진입 인터페이스(195:03:06)까지 남은 시간을 확인하기 위해서다.",
                    "화면에 시·분·초가 나온다.",
                    true, confirmed),
            ],
            "16");

        yield return Guide(
            "대기권 재진입",
            "MET 195:03:06(자료 AFJ 195:03:06, 시뮬레이터 195:03:05)에 CM 컬럼비아가 고도 약 122 km(400,000 ft)에서 대기권에 재진입한다. " +
            "CM-SM 분리는 194:49:12.7에 CM-SM SEP 스위치로 이미 마쳤다. 진입 속도는 약 36,194 fps다.",
            [
                "P61/P62가 EMS 초기화 데이터와 진입 자세를 준비하고, P63이 진입 방정식을 초기화해 자세를 유지하며 0.05 g를 기다린다.",
                "P64(0.05 g)부터 예측형 롤 시스템으로 양력 벡터를 조종해 착수점으로 유도한다.",
                "EMS(독립 가속도계, 범위/ΔV 카운터)가 병행 감시한다.",
            ],
            [
                Ground(
                    "최종 진입 PAD",
                    "속도 36,194 fps(N60), γ 6.49°, 최대 6.4 g, 0.05 g까지 EI에서 0:28, 착수점(N61) 13.32°N 169.17°W, 짐벌 트림 롤 0 피치 152 요 1.",
                    "진입 회랑과 착수점을 지상과 확인하기 위해서다.",
                    "승무원이 EMS 범위와 속도를 로드하고 DSKY 값과 대조한다.",
                    confirmed),
                Dsky(
                    "진입 준비 프로그램", "V37E61E",
                    "V37 ENTR 61 ENTR: P61(EMS 초기화 데이터 계산)을 선택한다. 이어 P62(CM/SM 분리 준비와 진입 자세). " +
                    "승무원이 누른 키는 [미확인]이며 일반 문법이다. 시뮬레이터는 P61을 지원하지 않는다.",
                    "착수 위도/경도, GMAX, γ 같은 진입 초기 데이터를 계산해 EMS에 넣기 위해서다.",
                    "N61, N60, N63(RTGO/VIO/TFE)이 표시되고 P62에서 분리 준비를 알린다.",
                    false, general),
                Crew(
                    "CM-SM 분리 스위치",
                    "194:49:12.7 패널 2(DSKY 바로 아래)의 CM-SM SEP 스위치를 올려 서비스 모듈을 분리한다.",
                    "열 차폐가 있는 CM만 대기권에 진입시키기 위해서다.",
                    "CM이 SM에서 떨어져 나가 진입 자세로 정렬한다.",
                    confirmed),
                Dsky(
                    "진입 방정식 초기화", "V37E63E",
                    "V37 ENTR 63 ENTR: P63(진입 방정식 초기화)을 선택한다. 시뮬레이터의 N63은 LM 정의라 이 구간에서는 참고용이다. " +
                    "승무원이 실제로 누른 키는 [미확인]이다.",
                    "진입 방정식을 초기화하고 자세를 유지하며 0.05 g를 기다리기 위해서다.",
                    "PROG 창이 63이 된다. 0.05 g가 감지되면 P64로 넘어간다.",
                    true, general),
                Observe(
                    "0.05 g",
                    "195:03:06 EI(400,000 ft), 약 195:03:36 0.05 g. EMS가 속도/범위 카운터를 줄이기 시작한다.",
                    "대기 감속이 시작되었음을 확인하기 위해서다.",
                    "컴퓨터 진입 유도(P64)가 시작된다.",
                    confirmed),
            ],
            "17");

        yield return Guide(
            "최대 감속 구간(P64)",
            "MET 195:03~05경 선체가 강하게 가열되며 감속이 최대에 이른다. AFJ는 첫 피크 6.5 g, 둘째 6.0 g(예상 6~7 g)이라고 적는다. " +
            "P64는 0.05 g에서 시작해 순환궤도 속도 미만으로 감속하는 것이 목적이며, 약 195:05:19에 P65로 넘어간다. 블랙아웃은 195:07:00에 끝난다.",
            [
                "P64가 롤 자세로 일정 항력을 유지하며 진입 유도를 한다(표시 N74). 필요하면 P65(상승 제어)를 거쳐 P67로 간다.",
                "Apollo 11에서는 P66이 호출되지 않았고 P65에서 바로 P67로 넘어갔다.",
                "P64는 스킵 아웃을 의도적으로 허용하도록 설계되었다(공식 보호 장치 이름은 미확인).",
            ],
            [
                Observe(
                    "컴퓨터에 조종권 이양",
                    "Armstrong: 'We gave spacecraft control over to the computer after we passed all our pitch attitude cross-checks.' EMS와 병행 감시한다.",
                    "자세 교차 확인이 끝나야 컴퓨터가 안전하게 조종할 수 있기 때문이다.",
                    "컴퓨터가 롤로 양력을 조절해 착수점에 가도록 유도한다.",
                    confirmed),
                Observe(
                    "P64에서 P65로",
                    "약 195:05:19 P64가 P65로 넘어간다. 항력이 Q7 미만이면 P66, 범위율이 음수이고 V가 VL 근처면 P67이 되지만 Apollo 11은 P65에서 P67로 갔다.",
                    "스킵 아웃을 피하려고 초과 순환 구간의 롤을 조정하기 위해서다.",
                    "속도가 줄며 컴퓨터가 마지막 단계(P67)로 간다.",
                    confirmed),
                Observe(
                    "최대 g와 블랙아웃",
                    "첫 피크 6.5 g, 둘째 6.0 g. Armstrong: 컴퓨터가 'did not fly the EMS the same way I would have flown the EMS'. 195:07:00 블랙아웃 종료.",
                    "컴퓨터와 EMS의 감속 프로파일이 서로 다를 수 있음을 알기 위해서다.",
                    "블랙아웃이 끝나면 통신이 복구된다.",
                    confirmed),
            ],
            "17");

        yield return Guide(
            "최종 강하(P67)",
            "MET 195:10:30경 낙하산을 펼치기 직전 마지막 단계다(시뮬레이터 고도 약 8 km, 150 m/s). P67은 CM 속도가 1,000 ft/s에 이르면 " +
            "조종을 끝내고 진입 DAP를 끈다. 드로그 낙하산은 195:12:06.9에 전개된다.",
            [
                "P67이 마지막 롤각을 유지하며 착수점까지 남은 범위를 표시한다(N67: 범위, 위도, 경도).",
                "속도가 1,000 ft/s가 되면 유도를 끝내고 진입 DAP가 꺼진다.",
            ],
            [
                Observe(
                    "P67 종료",
                    "P67은 CM 속도가 지구 기준 1,000 ft/s가 되면 조종을 종료한다(소스). Apollo 11에서는 P66 없이 P65에서 P67로 넘어왔다.",
                    "마지막 구간은 낙하산이 펼쳐질 때까지 안정한 자세로 접근하기 위해서다.",
                    "진입 DAP가 꺼지고 낙하산 시퀀스가 시작된다.",
                    confirmed),
                Dsky(
                    "착수점까지 범위(N67)", "V16N67E",
                    "V16 N67 ENTR: 착수점까지 남은 범위와 현재 위도/경도를 표시한다. 시뮬레이터는 N67을 지원하지 않는다.",
                    "착수점과의 거리를 지상에 보고하기 위해서다.",
                    "화면에 범위와 위도/경도가 나온다. 정확한 노운 의미는 완전히 확인되지 않았다.",
                    false, partial),
                Ground(
                    "DSKY 읽기 요청",
                    "195:11:18 Houston: 'Standing by for your DSKY reading.' P67 후 DSKY는 착수점까지의 범위와 현재 위도/경도를 보여 준다.",
                    "지상이 착수점 오차를 확인해 회수 함정에 위치를 알리기 위해서다.",
                    "값이 지상에 보고된다.",
                    confirmed),
                Observe(
                    "낙하산",
                    "195:12:06.9 드로그 낙하산(예측보다 약 1초 빠름), 이후 주 낙하산(8.5 m/s). 자동 전개이며 승무원 수동 조작은 자료에 없다.",
                    "하강 속도를 안전하게 줄이기 위해서다.",
                    "착수까지 낙하산으로 하강한다.",
                    GuideConfidence.Confirmed),
            ],
            "17");

        yield return Guide(
            "착수",
            "MET 195:18:35(MR)에 컬럼비아가 태평양(약 13.3°N, 169.15°W)에 착수하며 미션이 끝난다. PAO 비공식 시각은 195:18:21이다. " +
            "약 8일 3시간 만의 귀환이다.",
            [
                "낙하산 전개와 착수는 자동으로 진행되며 AGC는 진입 유도를 이미 끝낸 뒤다.",
            ],
            [
                Observe(
                    "착수점",
                    "진입 PAD의 착수점은 13.32°N 169.17°W(N61)이다. 195:18:35 착수(MR).",
                    "계획된 착수점과 실제 착수 위치를 비교하기 위해서다.",
                    "회수 함정이 위치로 이동해 CM을 회수한다.",
                    confirmed),
                Dsky(
                    "시계 확인", "V16N36E",
                    "V16 N36 ENTR: 컴퓨터 시계(MET)를 표시한다. 시뮬레이터 마지막 단계의 MET는 195:18:35다.",
                    "미션 전체 경과 시간을 컴퓨터 시계로 확인하기 위해서다.",
                    "화면에 시·분·초가 나온다.",
                    true, confirmed),
            ],
            "17");
    }
}
