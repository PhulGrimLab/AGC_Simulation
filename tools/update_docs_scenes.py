"""장면 렌더러/단계 가이드/우주선 전환 반영으로 문서(00, 02, README)를 갱신하는 일회성 보조 스크립트."""
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent


def edit(path, pairs):
    p = ROOT / path
    s = p.read_text(encoding="utf-8")
    for old, new in pairs:
        assert old in s, (path, old[:50])
        s = s.replace(old, new, 1)
    p.write_text(s, encoding="utf-8")


edit("doc/00_Development_Plan.md", [
    ("| 오른쪽 절반 | 미션 뷰: 단계명/설명, 벡터 도식(천체+우주선 마커), MET·고도·속도·수직속도·위치, 재생/일시정지, 배속, 타임라인 슬라이더, 단계 이동 |",
     "| 오른쪽 절반 | 미션 뷰: 단계명/설명, MET·고도·속도·수직속도·위치, 재생/일시정지, 배속, 타임라인 슬라이더, 단계 이동, **단계별 장면 렌더링**(발사·궤도·달 전이·하강·표면·재진입·착수 등 11가지, NASA 지구/달 텍스처 + 벡터 우주선), **단계 가이드 패널**(미션 설명, AGC 역할, 조작 순서와 목적·영향, [실행] 버튼). 장면과 가이드 사이는 스플리터로 크기 조절 |"),
    ("Agc.Wpf      (net10.0-windows)  UI: SegmentDisplay, DskyPanel, MissionIllustration, MainWindow",
     "Agc.Wpf      (net10.0-windows)  UI: SegmentDisplay, DskyPanel, MissionIllustration(+Scene/: PlanetRenderer, SpacecraftArt, TerrainArt, StarField, SceneRenderer), PhaseGuidePanel, MainWindow"),
    ("| M3 | 실제 NASA 이미지/영상 자산 도입(저작권/출처 목록 관리), 단계별 이미지, 도식 → 사진 전환 | 예정 |",
     "| M3 | 화면 고도화: NASA 공개 텍스처(지구 Blue Marble, 달 LROC)를 구체로 렌더링(터미네이터·대기광·구름), 우주선(새턴 V/CSM/LM/캡슐)·지형·바다·플라스마 벡터 아트, 단계별 11가지 장면. 출처는 `doc/ASSETS_CREDITS.md` | **완료**(실제 아폴로 사진/영상 도입은 후속) |\n"
     "| M3b | 단계 가이드 패널: 27개 단계의 미션 설명·AGC 역할·조작 순서(조작/목적/영향/확인 수준), 지원 DSKY 입력은 [실행] 버튼. 데이터는 doc/12 근거 | **완료** |"),
    ("| M4 | 단계별 AGC 프로그램 시나리오 확장: 프로그램이 스스로 표시를 띄우는 동작(P63 시작 시 V06N63 등), P63→P64→P66→P68 하강 Noun(N60/N64), N62, 1201/1202 시 깜박임·RESTART 램프, 랑데부(P20/P34/P35), 재진입(P61~P67) | 예정 |",
     "| M4 | 단계별 AGC 프로그램 시나리오: **(완료)** 사령선/착륙선 DSKY 전환(ALT/VEL 칸), 노운의 우주선별 정의(N62 CM/LM, N63/N68 LM 전용), 프로그램의 자동 표시(P11 N62, P63 N63)와 프로그램 표시 교체/KEY REL 보호. **(예정)** 점화 전 V06N62 카운트다운 자동 표시, N60/N64, 사령선 재진입 노운(N63 RTGO/VIO/TTE, N68), 1201/1202 시 깜박임·RESTART 램프, 랑데부(P20/P34/P35), 재진입(P61~P67) | **진행 중** |"),
    ("- 이 단계에서는 NASA 이미지 없이 벡터 도식을 사용한다.",
     "- 오른쪽 장면의 지구/달은 NASA 공개 텍스처, 우주선·지형·바다·별은 코드로 그린 벡터이다. 실제 아폴로 사진/영상은 쓰지 않는다(출처·라이선스: `doc/ASSETS_CREDITS.md`).\n"
     "- 장면의 우주선/지형 그림 코드는 비율 상수가 많아 CLAUDE.md 규칙 7(매직 넘버 금지)을 구역별 주석 수준으로만 지킨다. 상수화는 후속 정리 과제이다.\n"
     "- 발사 장면의 고도는 이륙 직후가 비현실적으로 빠른 선형 보간을 피하려고 시각용 가속 곡선(고도 ∝ 시간^1.9)을 쓴다. 텔레메트리/DSKY 값은 타임라인 그대로이다."),
])

edit("doc/02_Simulator_User_Manual.md", [
    ("│ 왼쪽: DSKY                  │ 오른쪽: 미션 뷰                 │\n│  - 램프 패널 (2열 x 7행)    │  - 단계명 / 설명                │\n│  - COMP ACTY, PROG          │  - 도식 (천체 + 우주선 마커)    │\n│  - VERB, NOUN               │  - MET, 기준 천체, 고도,        │\n│  - R1, R2, R3               │    속도, 수직속도, 위치         │\n│  - 키패드 19키              │  - 타임라인 슬라이더            │\n│                             │  - ▶재생 / 배속 / 단계 이동     │",
     "│ 왼쪽: 컴퓨터 캡션 + DSKY    │ 오른쪽: 미션 뷰                 │\n│  - 사령선 CMC / 착륙선 LGC  │  - 단계명 / 설명                │\n│  - 램프 패널 (2열 x 7행)    │  - MET, 기준 천체, 고도,        │\n│  - COMP ACTY, PROG          │    속도, 수직속도, 위치         │\n│  - VERB, NOUN, R1~R3        │  - 타임라인 / ▶재생 / 배속 /    │\n│  - 키패드 19키              │    단계 이동                    │\n│                             │  - 장면 (NASA 지구·달, 우주선)  │\n│                             │  ═══ 스플리터(크기 조절) ═══    │\n│                             │  - 단계 가이드 (설명, AGC 역할, │\n│                             │    조작 순서·목적·영향, [실행]) │"),
    ("### 미션 뷰 조작",
     "### 단계 가이드 패널 (오른쪽 아래)\n\n현재 단계에 맞춰 자동으로 바뀐다(단계가 바뀔 때만 다시 그려진다). 위에서부터:\n\n1. **무슨 일이 일어나나** — 이 단계의 미션 설명(실제 MET 포함)\n2. **AGC는 무엇을 하나** — 이 단계에서 컴퓨터의 역할\n3. **조작 순서 — 무엇을, 왜, 어떤 영향이** — 번호 카드. 각 카드에는 종류(DSKY / 승무원 조작 / 지상 교신 / 관찰), 근거의 확인 수준(확인 / 부분 / 일반 절차 / 미확인), DSKY 키 순서(키패드 글자 그대로), 그리고 **조작 · 목적 · 영향** 설명이 있다.\n\n- **[▶ 실행]** 버튼이 있는 DSKY 단계는 이 시뮬레이터가 지원하는 입력이라 버튼으로 DSKY에 그대로 눌러 볼 수 있다(오른쪽 위 DSKY 화면이 바로 바뀐다). \"시뮬레이터 미지원\"이라고 적힌 단계는 실제로는 있었던 조작이지만 이 프로그램이 아직 구현하지 않았다.\n- \"일반 절차\" 표시는 실제 키 시퀀스를 공개 자료에서 확인하지 못해 일반적인 방식으로 설명한 것이다.\n- 장면과 가이드 사이의 막대를 끌어 높이를 조절할 수 있고, 가이드는 스크롤된다. 내용의 근거는 `12_Mission_Phase_AGC_Guide.md`에 있다.\n\n### 장면\n\n단계에 따라 11가지 장면이 나온다: 발사(새턴 V 상승, 단 분리, 연기·불꽃), 지구 궤도/TLI, 달 전이(지구·달과 궤적), 달 궤도(LOI/LM 분리), 동력 하강(고도계와 지표 접근, 분사 먼지), 달 표면(착륙선, EVA 이후 우주인·성조기), 상승, 랑데부·도킹, 귀환, 대기권 재진입(플라스마), 낙하산 강하와 착수(회수 함정). 지구와 달은 NASA 공개 텍스처를 구에 입혀 태양 방향 음영·대기광·구름까지 계산한다(출처: `ASSETS_CREDITS.md`).\n\n### 미션 뷰 조작"),
])

edit("README.md", [
    ("![실행 화면](doc/screenshots/03_descent_alarm.png)",
     "![실행 화면 — 동력 하강](doc/screenshots/03_descent_alarm.png)\n\n![발사](doc/screenshots/10_launched_app.png)\n\n장면 모음: [doc/screenshots/scenes](doc/screenshots/scenes) (발사 · 지구 궤도 · 달 전이 · 달 궤도 · 하강 · 표면 · 상승 · 도킹 · 귀환 · 재진입 · 착수)"),
    ("- 테스트: `dotnet test tests/Agc.Tests` (218개: 단위/연동/UI/실제 창)", "- 테스트: `dotnet test tests/Agc.Tests` (300개: 단위/연동/UI/실제 창/장면 렌더링)"),
    ("| [doc/Test_Scenarios.xlsx](doc/Test_Scenarios.xlsx) | 기능별 UI 테스트 시나리오와 최신 결과, 발견된 결함 |",
     "| [doc/Test_Scenarios.xlsx](doc/Test_Scenarios.xlsx) | 기능별 UI 테스트 시나리오와 최신 결과, 발견된 결함 |\n| [doc/ASSETS_CREDITS.md](doc/ASSETS_CREDITS.md) | 화면에 쓰는 NASA 텍스처의 출처와 라이선스 |"),
])
print("docs updated")
