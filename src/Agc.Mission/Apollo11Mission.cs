using Agc.Core;

namespace Agc.Mission;

/// <summary>
/// Apollo 11 미션 타임라인 데이터.
/// 주의: 시각(MET)과 수치는 공개된 미션 기록을 바탕으로 한 반올림 근사값이며 실제 텔레메트리가 아니다.
/// 시각화/교육용 보간 입력이므로, 정확도가 필요해지면 Apollo 11 Flight Journal 등과 대조해 보정한다.
/// 단위: 고도 m, 속도 m/s(기준 천체 상대), 위·경도 도.
/// </summary>
public static class Apollo11Mission
{
    private const double Km = 1000.0;
    private const double LaunchLatitude = 28.6;
    private const double LaunchLongitude = -80.6;
    private const double TranquilityLatitude = 0.67;
    private const double TranquilityLongitude = 23.47;
    private const double SplashdownLatitude = 13.3;
    private const double SplashdownLongitude = -169.15;

    public static MissionTimeline Create()
    {
        return new MissionTimeline(
            BuildKeyframes(),
            BuildAlarms(),
            new MissionTarget(CelestialBody.Moon, TranquilityLatitude, TranquilityLongitude),
            BuildDisplayRequests());
    }

    /// <summary>
    /// 프로그램이 단계 시작 때 스스로 띄우는 표시.
    /// 발사(P11): 사령선 CMC가 V06 N62(관성속도/고도변화율/고도)를 계속 갱신해 보여 준다(Comanche 055 P11).
    /// 동력 하강: 점화 후 N63(속도/고도변화율/계산 고도)이 표시된다(Eyles). 점화 전 V06 N62 카운트다운과
    /// 이후 N64는 아직 모델링하지 않는다.
    /// </summary>
    private static List<MissionDisplayRequest> BuildDisplayRequests()
    {
        return
        [
            new MissionDisplayRequest(Met(0, 0, 0), 16, 62, false),
            new MissionDisplayRequest(Met(102, 33, 5), 16, 63, false),
        ];
    }

    private static TimeSpan Met(int hours, int minutes, int seconds)
    {
        return new TimeSpan(hours, minutes, seconds);
    }

    private static MissionKeyframe Frame(
        TimeSpan met, string name, string description, int program, CelestialBody body,
        double altitude, double speed, double lat, double lon, AgcVehicle vehicle = AgcVehicle.Csm)
    {
        return new MissionKeyframe(met, name, description, program, body, altitude, speed, lat, lon, vehicle);
    }

    private static List<MissionKeyframe> BuildKeyframes()
    {
        const CelestialBody earth = CelestialBody.Earth;
        const CelestialBody moon = CelestialBody.Moon;
        List<MissionKeyframe> frames =
        [
            Frame(Met(0, 0, 0), "발사", "새턴 V가 케이프 케네디 39A 발사대에서 이륙한다. AGC는 P11(지구 궤도 진입 감시)을 수행한다.",
                11, earth, 0, 0, LaunchLatitude, LaunchLongitude),
            Frame(Met(0, 2, 42), "1단 분리", "S-IC 1단 연소가 끝나고 분리되며 S-II 2단이 점화된다.",
                11, earth, 67 * Km, 2400, 29.0, -78.0),
            Frame(Met(0, 9, 12), "2단 연소 종료", "S-II 2단이 연소를 마치고 S-IVB 3단이 이어받는다.",
                11, earth, 185 * Km, 6900, 30.0, -50.0),
            Frame(Met(0, 11, 49), "지구 저궤도 진입", "약 185km 원 궤도에 진입한다. 승무원이 점검을 수행한다.",
                0, earth, 185 * Km, 7800, 32.0, -35.0),
            Frame(Met(2, 44, 16), "TLI 점화", "S-IVB 3단을 재점화하여 달 전이 궤도로 가속한다(P15).",
                15, earth, 190 * Km, 7800, -10.0, 170.0),
            Frame(Met(2, 50, 3), "TLI 종료", "달 전이 궤도에 진입. 속도는 약 10.8km/s.",
                0, earth, 334 * Km, 10830, -5.0, 175.0),
            Frame(Met(30, 0, 0), "달로 향하는 비행", "달을 향해 관성 비행하며 중간 궤도 수정을 준비한다.",
                0, earth, 150000 * Km, 2200, 0, 0),
            Frame(Met(61, 0, 0), "달 접근", "지구에서 멀어질수록 속도가 줄어든다.",
                0, earth, 310000 * Km, 1100, 0, 0),
            Frame(Met(61, 39, 50), "달 중력권 진입", "달의 중력이 지구보다 커지는 영역에 들어선다.",
                0, moon, 72000 * Km, 910, 0, 0),
            Frame(Met(75, 49, 50), "달 궤도 진입 연소(LOI-1)", "서비스 추진계통을 점화하여 달 궤도에 포획된다(P40).",
                40, moon, 110 * Km, 2600, 0, 0),
            Frame(Met(100, 12, 0), "LM 분리", "이글(LM)이 컬럼비아(CSM)에서 분리된다. 이 시점부터 DSKY는 착륙선 컴퓨터(LGC)로 본다.",
                0, moon, 110 * Km, 1630, 0, 0, AgcVehicle.Lm),
            Frame(Met(102, 33, 5), "동력 하강 시작(PDI)", "하강 엔진을 점화하여 감속을 시작한다. AGC는 P63(제동 단계)을 수행한다.",
                63, moon, 15 * Km, 1700, 0.7, 8.0, AgcVehicle.Lm),
            Frame(Met(102, 41, 32), "접근 단계(P64)", "착륙 지점이 창문에 보이기 시작한다. 목표 지점이 바위 지대로 확인된다.",
                64, moon, 2255, 90, 0.68, 22.0, AgcVehicle.Lm),
            Frame(Met(102, 43, 20), "수동 조종 착륙(P66)", "암스트롱이 수동으로 비행하며 안전한 착륙 지점을 찾는다.",
                66, moon, 131, 20, TranquilityLatitude, 23.4, AgcVehicle.Lm),
            Frame(Met(102, 45, 40), "착륙 — 고요의 바다", "\"Houston, Tranquility Base here. The Eagle has landed.\" AGC는 P68(착륙 확인)을 수행한다.",
                68, moon, 0, 0, TranquilityLatitude, TranquilityLongitude, AgcVehicle.Lm),
            Frame(Met(109, 24, 15), "달 표면 활동(EVA)", "닐 암스트롱이 달 표면에 첫 발을 내딛는다.",
                0, moon, 0, 0, TranquilityLatitude, TranquilityLongitude, AgcVehicle.Lm),
            Frame(Met(124, 22, 0), "상승단 이륙", "상승 엔진을 점화해 달 표면에서 이륙한다(P12).",
                12, moon, 0, 0, TranquilityLatitude, TranquilityLongitude, AgcVehicle.Lm),
            Frame(Met(124, 29, 15), "상승단 궤도 진입", "달 궤도에 진입하여 컬럼비아와의 랑데부를 준비한다.",
                0, moon, 17 * Km, 1690, 0.7, 30.0, AgcVehicle.Lm),
            Frame(Met(128, 3, 0), "도킹", "이글 상승단이 컬럼비아와 도킹한다.",
                0, moon, 110 * Km, 1630, 0, 0),
            Frame(Met(135, 23, 42), "지구 귀환 연소(TEI)", "서비스 추진계통을 점화하여 달 궤도를 벗어난다(P40).",
                40, moon, 110 * Km, 1630, 0, 0),
            Frame(Met(135, 26, 12), "TEI 종료", "지구 귀환 궤도에 진입. 속도는 약 2.5km/s.",
                0, moon, 120 * Km, 2500, 0, 0),
            Frame(Met(150, 0, 0), "지구로 돌아오는 비행", "지구 중력권으로 되돌아오며 서서히 가속한다.",
                0, earth, 300000 * Km, 1200, 0, 0),
            Frame(Met(175, 0, 0), "귀환 비행 후반", "지구가 가까워지며 속도가 늘어난다.",
                0, earth, 150000 * Km, 2500, 0, 0),
            Frame(Met(195, 3, 5), "대기권 재진입", "고도 122km에서 대기권에 재진입한다(P63).",
                63, earth, 122 * Km, 11000, 4.0, -140.0),
            Frame(Met(195, 4, 30), "최대 감속 구간(P64)", "선체가 강하게 가열되며 감속이 최대에 이른다.",
                64, earth, 75 * Km, 11000, 8.0, -155.0),
            Frame(Met(195, 10, 30), "최종 강하(P67)", "낙하산을 펼치기 직전 마지막 단계.",
                67, earth, 8 * Km, 150, SplashdownLatitude, SplashdownLongitude),
            Frame(Met(195, 18, 35), "착수", "태평양에 착수하며 미션이 끝난다.",
                0, earth, 0, 0, SplashdownLatitude, SplashdownLongitude),
        ];

        // 각 단계에 어떤 장면을 보여 줄지(화면 렌더러가 사용). 단계 이름이 키이며 누락되면 즉시 오류로 드러난다.
        return frames.Select(f => f with { Scene = SceneByPhase[f.PhaseName] }).ToList();
    }

    private static readonly Dictionary<string, MissionScene> SceneByPhase = new()
    {
        ["발사"] = MissionScene.Launch,
        ["1단 분리"] = MissionScene.Launch,
        ["2단 연소 종료"] = MissionScene.Launch,
        ["지구 저궤도 진입"] = MissionScene.EarthOrbit,
        ["TLI 점화"] = MissionScene.EarthOrbit,
        ["TLI 종료"] = MissionScene.Translunar,
        ["달로 향하는 비행"] = MissionScene.Translunar,
        ["달 접근"] = MissionScene.Translunar,
        ["달 중력권 진입"] = MissionScene.Translunar,
        ["달 궤도 진입 연소(LOI-1)"] = MissionScene.LunarOrbit,
        ["LM 분리"] = MissionScene.LunarOrbit,
        ["동력 하강 시작(PDI)"] = MissionScene.Descent,
        ["접근 단계(P64)"] = MissionScene.Descent,
        ["수동 조종 착륙(P66)"] = MissionScene.Descent,
        ["착륙 — 고요의 바다"] = MissionScene.Surface,
        ["달 표면 활동(EVA)"] = MissionScene.Surface,
        ["상승단 이륙"] = MissionScene.Ascent,
        ["상승단 궤도 진입"] = MissionScene.Ascent,
        ["도킹"] = MissionScene.Docking,
        ["지구 귀환 연소(TEI)"] = MissionScene.LunarOrbit,
        ["TEI 종료"] = MissionScene.TransEarth,
        ["지구로 돌아오는 비행"] = MissionScene.TransEarth,
        ["귀환 비행 후반"] = MissionScene.TransEarth,
        ["대기권 재진입"] = MissionScene.Entry,
        ["최대 감속 구간(P64)"] = MissionScene.Entry,
        ["최종 강하(P67)"] = MissionScene.Splashdown,
        ["착수"] = MissionScene.Splashdown,
    };

    /// <summary>
    /// 하강 중 발생한 프로그램 알람(1202 4회, 1201 1회). 시각은 Flight Journal/Eyles 기록을 바탕으로 한 근사값이다:
    /// 앞의 둘은 P63 제동 중(V16N68 표시 후), 1201은 102:42:17경, 이어서 +24초, +16초 뒤 1202.
    /// </summary>
    private static List<MissionAlarm> BuildAlarms()
    {
        return
        [
            new MissionAlarm(Met(102, 38, 26), 1202),
            new MissionAlarm(Met(102, 39, 2), 1202),
            new MissionAlarm(Met(102, 42, 17), 1201),
            new MissionAlarm(Met(102, 42, 41), 1202),
            new MissionAlarm(Met(102, 42, 57), 1202),
        ];
    }
}
