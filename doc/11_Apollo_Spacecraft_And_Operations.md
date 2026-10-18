# 11. 아폴로 우주선과 운용 — 새턴 V, CSM, LM, 그리고 유도 컴퓨터의 위치

이 문서는 AGC 시뮬레이터(`AGC_Simulation`)가 다루는 Apollo 11 임무의 하드웨어(발사체·사령/기계선·달착륙선)와 운용 방식, 그리고 그 안에서 Apollo Guidance Computer(AGC)가 맡는 역할을 설명한다.

- AGC 자체(명령어, 메모리, DSKY 조작 규칙)는 `doc/10`(AGC 매뉴얼)에서, 비행 단계별 AGC 프로그램/DSKY 상세는 `doc/12_Mission_Phase_AGC_Guide.md`에서 다룬다. 이 문서는 "우주선이 무엇이고 어떻게 운용되었는가"에 집중한다.
- 신뢰도 태그: **[확인]** = 인용한 출처에서 직접 확인, **[부분]** = 일부만 확인/2차 출처 또는 출처 간 차이 있음, **[미확인]** = 이 작업에서 출처로 검증하지 못함(일반 지식 또는 추정이며 참고용).
- 출처 약칭은 11장에 URL과 함께 정리했다. 본문 표기: `[W-SV]` 식으로 출처를 붙인다.
- 단위는 출처 원문 단위(ft/s, nmi 등)를 보존하고 필요한 곳만 병기한다.

---

## 1. 아폴로 계획과 미션 구조

### 1.1 Apollo 11 임무 개요

Apollo 11은 최초의 유인 달 착륙 임무다. 사령선(CSM) "Columbia"와 달착륙선(LM) "Eagle"이 달 궤도에서 분리되어, LM이 Tranquility Base에 착륙하고, 상승단이 다시 CSM과 랑데부·도킹한 뒤 CSM이 지구로 귀환했다. [확인: `[MR]`, `[AFJ]`]

임무 구조는 "LOR(Lunar Orbit Rendezvous, 달 궤도 랑데부)" 방식이다. 달에 내려가는 것은 LM뿐이고, CSM은 달 궤도에 남아 귀환 수단이 된다. 이 방식 때문에 AGC가 두 대(CM 1대, LM 1대) 필요했다. [부분: 구조는 `[MR]`의 비행 순서와 `[W-AGC]`의 CM/LM 소프트웨어 구분에서 확인, "LOR 채택 이유"는 [미확인]]

### 1.2 임무 프로필 개요 (Apollo 11, MET = 발사 후 경과시간 hhh:mm:ss)

| 단계 | 대표 이벤트 | MET | 비고 |
|---|---|---|---|
| 발사/상승 | 이륙 (LC-39A) | 0:00:00 | `[MR]` [확인] |
| | S-IC 연소 종료 | 약 0:02:42 | 임무 사실 입력값 [확인] |
| | S-II 연소 종료 | 약 0:09:12 | [확인] |
| | 지구 궤도 진입 | 약 0:11:49 (약 185 km) | [확인] |
| 지구 궤도 | TLI(S-IVB 재점화) | 2:44:16 (MR) / 2:44:18 (교신록) | 출처 간 2초 차이, 병기 [부분] |
| | TLI 연소 종료 | 약 2:50 | [확인] |
| 달 전이 | MCC-2 | 약 26:44:58 (20.9 ft/s) | MCC-1 취소, MCC-4 67:28에 삭제 [확인] |
| 달 궤도 진입 | LOI-1 | 75:49:50 (357.5 s, 2,917 fps) | SPS [확인] |
| | LOI-2 | 80:11:36.75 (16.88 s) | [확인] |
| 달 궤도/하강 | LM 분리 | 약 100:12 | [확인] |
| | PDI(동력 하강 개시) | 102:33:05 (Eyles) / 102:33:07 (ALSJ 주석) | 출처 간 2초 차이 [부분] |
| | High gate(P64) | 102:41:32 | [확인] |
| | P66 진입 | 102:43:20 | [확인] |
| | 착륙(접지) | 102:45:40 | [확인] |
| 달 표면 | EVA 시작 | 약 109:24 | [확인] |
| 상승/랑데부 | 상승단 이륙 | 124:22:00 | [확인] |
| | 궤도 진입 | 124:29:17 (47.2 x 9.5 nmi) | [확인] |
| | CSI / CDH / TPI | 125:19:35 / 126:17:45 / 127:03:52 | [확인] |
| | 도킹 | 약 128:03 | [확인] |
| 귀환 | TEI | 135:23:42 (3,283.6 fps, 2:28) | PAD TIG 135:23:41.56 [확인] |
| | MCC-5 | 150:29:54 (RCS 4.8 fps) | MCC-6/7 취소 [확인] |
| 재진입 | SM 분리 | 194:49:12.7 | [확인] |
| | Entry Interface | 195:03:06 | PAD에는 195:03:07 [부분] |
| | 드로그 낙하산 | 195:12:06.9 | [확인] |
| | 착수 | 195:18:35 | Mission Report 기준 [확인] |

(위 표의 출처: 임무 사실 입력값 = `[MR]`, `[EYLES]`, `[AFJ]`, `[ALSJ]`. 개별 URL은 11장.)

### 1.3 임무 프로필 ASCII 도식

```
 지구                                           달
  (1) 발사 ──> (2) 궤도 진입 ──> (3) TLI ═══════════════> (6) LOI-1/LOI-2
   0:00        ~0:12 (185km)    2:44                       75:49 / 80:11
                                  │ MCC-2 (26:44)           │
                                  │ (MCC-1 취소, MCC-4 삭제)  ├─ LM 분리 100:12
                                                            ├─ DOI -> PDI 102:33
                                                            ├─ 착륙 102:45:40
  (11) 착수 <── (10) 재진입 <── (9) 귀환 비행 <═══ (8) TEI ───┤  표면 ~22 h
   195:18:35   EI 195:03      MCC-5 150:29     135:23       ├─ 상승 124:22:00
                SM 분리                                      ├─ CSI/CDH/TPI
                194:49                                       └─ 도킹 ~128:03
```

---

## 2. 새턴 V 발사체

### 2.1 개요 규격

| 항목 | 값 | 출처/태그 |
|---|---|---|
| 전고(아폴로 우주선 포함) | 111 m (363 ft) | `[W-SV]` [부분] (Wikipedia 1개 출처) |
| 직경(핀 제외, 기부) | 10 m (33 ft) | `[W-SV]` [부분] |
| 이륙 질량 | 2,822,171 ~ 2,965,241 kg (기체별 범위) | `[W-SV]` [부분] |
| 이륙 추력 | 34.5 MN | `[W-SV]` [부분] |
| 구성 | 3개 단 + Instrument Unit | `[W-SV]` [확인] |

> 주의: 위 값은 Wikipedia 한 곳 기준이며, 개별 기체(SA-506, Apollo 11)의 정확한 질량은 확인하지 못했다. [미확인: Apollo 11(SA-506) 개별 값]

### 2.2 단별 규격

| 항목 | S-IC (1단) | S-II (2단) | S-IVB (3단) |
|---|---|---|---|
| 엔진 | 5 x F-1 | 5 x J-2 | 1 x J-2 |
| 최대 추력 | 33,000 kN(해면) | 4,400 kN(진공) | 1,000 kN(진공) |
| 추진제 | LOX/RP-1 | LOX/LH2 | LOX/LH2 |
| 연소 시간 | 150 s | 395 s | 165 s + 312 s (2회 연소) |
| 길이 | 42 m | 24.87 m | 17.86 m |
| 직경 | 10 m | 10 m | 6.60 m |
| 총질량 | 2,214,000 kg | 470,000 kg | 120,500 kg |
| 건조질량 | 137,000 kg | 43,000 kg | 15,200 kg |

출처: `[W-SV]` [부분]. 연소 시간은 공칭값이며 Apollo 11 실측과는 다르다. 실측 컷오프 시각은 1.2절 표(S-IC 약 2:42, S-II 약 9:12)를 따른다. 3개 값(150 s / 395 s / 165 s)과 실측의 차이를 그대로 두고 설명하지 않는다. [부분]

### 2.3 각 단의 역할

- **S-IC**: 지상에서 이륙해 약 2분 42초 동안 연소, 가장 큰 추력을 낸다. LOX/RP-1. [확인: 연소 종료 시각 `[MR]` / 엔진 `[W-SV]`]
- **S-II**: LOX/LH2 J-2 5기. S-IC 분리 후 점화되어 약 9분 12초에 연소를 마친다. [확인: 시각, `[W-SV]`]
- **S-IVB**: J-2 1기를 2회 점화한다. 1차 연소로 지구 저궤도에 진입(약 11:49, 약 185 km), 궤도 점검 후 2차 연소(TLI, 2:44:16)로 달 전이 궤도에 보낸다. [확인: `[MR]` 임무 사실 / 2회 연소 `[W-SV]`]
- **Instrument Unit (IU)**: S-IVB 위에 얹힌 링 구조물로, 발사체 유도·항법·제어 장비와 텔레메트리·통신·추적·승무원 안전 장비를 담고 있다. 직경 6.6 m, 높이 0.91 m, 질량 약 2,000 kg, MSFC 설계/IBM 제작. [부분: `[W-SV]`]

### 2.4 유도 책임: IU가 주(主), CMC는 감시/백업

- 새턴 V 상승·TLI 중 **발사체를 실제로 조종하는 것은 IU의 컴퓨터**(Launch Vehicle Digital Computer)이다. [부분: IU가 GN&C 장비를 포함함은 `[W-SV]`; "LVDC"라는 명칭은 [미확인]]
- CM의 AGC는 발사 중 **P11(지구 궤도 진입 감시)**을 수행한다. 발사체 유도를 대신하는 것이 아니라 IMU로 상태를 추적하며 승무원에게 궤도 파라미터를 보여주는 역할이다. [확인: `Apollo11Mission.cs` 설명 및 research A의 P01/P02/P03 → P11 전환(발사 이륙 디스크리트)]
- 발사체 유도 장애 시 승무원이 수동으로 인계하는 절차가 존재했으나, 이 작업에서는 구체적 절차를 확인하지 못했다. [미확인]

### 2.5 단 분리/아폴로 우주선 분리 순서 (개요)

```
[발사 0:00]  S-IC 연소 ─> (약 2:42 연소 종료, S-IC 분리) ─> S-II 점화
             S-II 연소 ─> (약 9:12 연소 종료, S-II 분리) ─> S-IVB 점화
             S-IVB 1차 연소 ─> 궤도 진입 (약 11:49)
             ... 지구 궤도 2~3회전 점검 ...
             S-IVB 2차 연소 = TLI (2:44:16 ~ 약 2:50)
             이후 CSM이 S-IVB(+SLA 안의 LM)에서 분리, 전위·도킹, LM 추출
```

- 위 흐름에서 S-IC/S-II 연소 종료 시각과 TLI 시각은 [확인]. 단 분리의 세부 순서(점화 전 지연, 사이드 로켓, 벤트 등)와 "TLI 후 CSM의 전위·도킹·LM 추출(Transposition, Docking and Extraction)" 구체 시각은 [미확인].
- 추가 설명: LM은 발사 시 CSM 아래 어댑터(SLA) 안에 접혀 들어 있다. SLA의 구성은 [미확인].

---

## 3. 사령/기계선 (CSM)

### 3.1 구성

CSM = 사령선(Command Module, CM) + 서비스선(Service Module, SM). 승무원은 3명(사령관/CMP/LMP; Apollo 11 CSM에는 CMP Collins가 주로 탑승). [부분: 인원수는 일반 지식, Collins의 역할은 `[AFJ]` 교신]

| 항목 | CM | SM |
|---|---|---|
| 질량 | 12,250 lb (5,560 kg) | 54,060 lb (24,520 kg) |
| 길이 | 11.4 ft (3.5 m) | 24.8 ft (7.6 m) |
| 직경 | 12.8 ft (3.9 m) | 12.8 ft (3.9 m) |

출처: `[W-CSM]` [부분]. (위키 값은 기체별 변형 사이의 평균이므로 Apollo 11 개체값과는 다를 수 있다 [미확인].)

### 3.2 사령선 (CM)

- **거주 공간**: 생활 공간 210 ft3 (5.9 m3), 가압 용적 366 ft3 (10.4 m3). [부분: `[W-CSM]`]
- **재진입 열차폐(heat shield)**: 삭마(ablative) 페놀 수지, 후단부 약 2 in (5.1 cm), 승무원 구획/전방부 약 0.5 in (1.3 cm). 출처 내 질량값이 1,869 lb(848 kg, 사양표)와 약 3,000 lb(1,400 kg, 구조 설명) 두 개로 상충 - 병기한다. [부분: `[W-CSM]`]
- **CM RCS**: 12개 추력기, 각 93 lbf (410 N), MMH/N2O4, 연료 270 lb(120 kg). 재진입 중 자세 제어에 쓰인다. [부분: `[W-CSM]`]
- **낙하산**: 드로그 2개(지름 16.5 ft, conical ribbon), 파일럿 3개(7.2 ft, ringslot), 주 낙하산 3개(83.5 ft, ringsail). [부분: `[W-CSM]`] Apollo 11의 드로그 전개는 195:12:06.9, 착수 195:18:35 [확인: `[MR]`]. 낙하산 전개 시 승무원의 수동 조작 여부는 [미확인].
- 주 SM 분리 스위치: AFJ는 "패널 2의 DSKY 바로 아래 CM-SM Sep 스위치를 올려" 분리를 시작했다고 설명한다. [확인: `[AFJ-D9b]`]

### 3.3 서비스선 (SM)

- **SPS (Service Propulsion System)**: 엔진 AJ10-137, 추력 20,500 lbf (91 kN), 연료 Aerozine 50(출처 한 곳은 UDMH/N2H4로 표기 - 상충 병기) + N2O4. 비추력 314~314.5 s(출처 간 0.5 s 차). 추진제 40,590 lb (18,410 kg), 우주선 전체 delta-v 약 9,200 ft/s (2,800 m/s). [부분: `[W-CSM]`]
- Apollo 11 SPS 연소: LOI-1 357.5 s / LOI-2 16.88 s / TEI 2:28. [확인: `[MR]`] TEI는 약 10,000 lb 추진제 소모(PAD/교신) [확인: `[AFJ-TEI]`]
- **SM RCS**: 4개 쿼드(quad) x 4개 R-4D 추력기(총 16개), 각 100 lbf (440 N), MMH/NTO. [부분: `[W-CSM]`] MCC-5는 SPS가 아닌 이 RCS 4-quad로 4.8 ft/s를 냈다. [확인: `[AFJ-D7]`]
- **전력/생명유지**: 연료전지 3기, 각 1.4 kW, 30 V DC. [부분: `[W-CSM]`] 연료전지의 반응물(수소/산소), 물 생산, ECS 구성은 이 작업에서 [미확인].

### 3.4 CSM에서 AGC가 쓰이는 곳

- 소프트웨어: **Colossus 2A (= Comanche 055)**, Apollo 11 CM 소프트웨어. [부분: 임무 사실 입력값 + `[AGC-SRC]` 헤더. 다른 자료는 Colossus 249라고도 부르나 이 작업에서는 미확인]
- SPS 연소: P40 (SPS 추력 프로그램), RCS 연소: P41, 정렬: P51/P52, 항법: P20 등. 단계별 상세는 `doc/12`. [확인: `[AGC-SRC]`]

---

## 4. 달착륙선 (LM)

### 4.1 구성과 규격

LM은 **하강단(Descent Stage)**과 **상승단(Ascent Stage)**으로 이루어진다. 하강단은 달 표면에서 발사대 역할을 하고 남는다. 승무원 2명. [부분: `[W-LM]`]

| 항목 | 값 | 출처/태그 |
|---|---|---|
| LM 질량(표준형) | 33,500 lb (15,200 kg), 건조 9,430 lb (4,280 kg) | `[W-LM]` [부분](Apollo 11 LM-5 개체값은 위키에 없음 [미확인]) |
| 전체 높이 | 23 ft 1 in (7.04 m) | `[W-LM]` [부분] |
| 직경(다리 제외) | 13 ft 10 in (4.22 m) | `[W-LM]` [부분] |
| 다리 전개 폭 | 31 ft (9.4 m) | `[W-LM]` [부분] |
| 승무원 | 2 | `[W-LM]` [부분] |

### 4.2 하강단

- **DPS (Descent Propulsion System)**: 추력 10,125 lbf (45,040 N), 출력 10%~60% 범위에서 스로틀 가능(그 사이 구간 및 풀 스로틀 운용 상세는 [미확인]). 추진제 18,000 lb (8,200 kg), 비추력 311 s. [부분: `[W-LM]`]
  - Apollo 11 PDI는 10% 추력으로 점화, 이후 "Throttle up" (102:33:25), "Throttle down on time" (102:39:35)을 교신에서 확인. [확인: `[ALSJ-LND]` via research B]
- **착륙 다리**: 접이식 4개. 접촉 프로브 길이 67 in (1.7 m). [부분: `[W-LM]`] "Contact Light" 점등 102:45:40, "Shutdown" 102:45:43 [확인: `[ALSJ-LND]`]
- **착륙 레이더(LR)**: 연속파 도플러 레이더, 하강 엔진 열차폐 근처 장착. 세부 규격은 출처에 없음 [미확인]. 하강 중 "altitude light"가 102:33:03에 점등되고 102:38:04경 레이더 락온 [확인: `[ALSJ-LND]`]
- **하강단 전력**: 은-아연 배터리 4기(Apollo 9~14 기준), 각 415 Ah. [부분: `[W-LM]`]

### 4.3 상승단

- **APS (Ascent Propulsion System)**: 추력 3,500 lbf (16,000 N), 추진제 5,187 lb (2,353 kg), 비추력 311 s, delta-v 7,280 ft/s (2,220 m/s). [부분: `[W-LM]`] Apollo 11 이륙 124:22:00, 연소 종료 124:29:17. [확인: `[ALSJ-LCH]`]
- **LM RCS**: 4쿼드 x 4 = 16개, 각 100 lbf (440 N), 비추력 290 s. [부분: `[W-LM]`]
- **랑데부 레이더(RR)**: 독립 조향 접시 안테나. RR 모드 스위치 SLEW/AUTO/LGC. P20이 RR을 제어하고 CSM에 락온한다(R21/R22). [확인: LM 소스 P20-P25 주석 `[LUM-SRC]`, 스위치 모드는 교신 `[AFJ-RDV]`]
- **상승단 전력**: 은-아연 배터리 2기, 각 296 Ah. [부분: `[W-LM]`]
- **AGS**: TRW가 개발한 백업 유도 시스템(DEDA 키패드로 조작). [부분: `[W-LM]`, DEDA 교신 `[ALSJ-LCH]`]

### 4.4 생명유지

- 승무원실 대기 100% 산소, 4.8 psi (33 kPa), ECS는 Hamilton Standard가 하도급. [부분: `[W-LM]`] 그 외 물/냉각 루프/CO2 제거 구성은 [미확인].

---

## 5. 유도·항법·제어 시스템 (G&N / PGNCS)

### 5.1 구성요소와 역할

| 구성요소 | 역할 | CM | LM | 출처/태그 |
|---|---|---|---|---|
| IMU | 3축 짐벌 안정 플랫폼. 자이로 3 + 가속도계 3. 6인치 베릴륨 큐브(stable member) | O | O | `[W-PGNCS]` [부분] |
| CDU (Coupling Data Unit) | IMU 짐벌 각도 등을 컴퓨터가 읽을 수 있는 값으로 변환, 컴퓨터 명령을 구동 신호로 변환(정의는 [미확인], 호칭만 확인) | O | O | 호칭 `[ASM]` [부분] |
| 광학계 | 정밀 육분의(SXT)와 저배율·광시야(60도) 스캐닝 망원경(SCT) - IMU 정렬과 항법 마킹에 사용 | O | (LM은 AOT 정렬 망원경) | `[W-PGNCS]` [부분]; LM의 AOT는 교신에서 확인 [부분] |
| AGC + DSKY | 컴퓨터와 표시/입력 장치 | O(DSKY 2) | O(DSKY 1) | 7장 참조 |
| DAP | 컴퓨터 소프트웨어 내 디지털 자동조종기(TVC, RCS, 재진입) | O | O | 6장 |
| SCS (Stabilization & Control System) | CM의 아날로그 백업 자세/엔진 제어 | O | - | 일반 지식 [미확인] |
| AGS (Abort Guidance System) | LM의 독립 백업 유도 컴퓨터 + DEDA | - | O | `[W-LM]` [부분] |
| EMS (Entry Monitor System) | 재진입 감시: 가속도계, 속도/거리 카운터, 스크롤 | O | - | `[AFJ-D9b]`, `[AFJ-TEI]` [부분] |
| LR / RR | 착륙 레이더 / 랑데부 레이더 | - | O | 4장 |

> 요점: AGC는 "혼자 모든 것을 하는 컴퓨터"가 아니라, IMU·CDU·광학계·레이더에서 읽은 데이터를 처리하고 엔진/제트 명령을 내보내는 G&N 시스템의 중앙 처리 요소다.

### 5.2 AGC 규격 요약

- 16비트 워드(15 데이터 비트 + 패리티), 소거 가능(erasable) 메모리 2,048 워드, 고정(fixed) 메모리 36,864 워드, 메모리 사이클 11.72 us, 덧셈 23.4 us. [확인: `[COMP]`, `[W-AGC]`] 상세는 `doc/10`.
- 질량/전력(약 32 kg, 55 W)은 Wikipedia 단독 출처: [부분: `[W-AGC]`]
- 소프트웨어: CM = Colossus 2A(Comanche 055), LM = Luminary 1A(Build 099). [확인: 임무 사실 입력값, `[LUM-SRC]` P12.agc 헤더]

### 5.3 G&N 블록 다이어그램과 정보 흐름

```
                    ┌──────────────────────────────────────────┐
  (CM) 광학: SXT/SCT │                                          │
  (LM) AOT ─ 마크 ──>│                                          │
                    │          AGC  (Executive/Waitlist,       │
  IMU ─ 짐벌각 ─> CDU ─>│          Interpreter, 유도 방정식)      │──> DSKY (표시)
  IMU ─ PIPA 속도 ──>│                                          │<── DSKY (V/N 키 입력)
  LR/RR 데이터 ─────>│                                          │
  지상 업링크 ───────>│                                          │──> 다운링크(텔레메트리)
                    └───────┬───────────────┬──────────────────┘
                            │ 엔진 점화/정지  │ 자세/오프셋 명령
                            v               v
                  SPS/DPS/APS 엔진     RCS 제트(DAP), TVC 짐벌, 스로틀
                            │
              (CM) EMS ─ 독립 감시/백업   (CM) SCS ─ 아날로그 백업
              (LM) AGS ─ 독립 백업 컴퓨터
```

- IMU 가속도계(PIPA)가 측정하는 비중력 가속도 적분 → 상태 벡터(위치/속도) 갱신 → 유도 프로그램이 필요한 delta-v와 방향 산출 → DAP가 TVC/RCS 명령으로 변환. [부분: 구조는 소스·교신에서 일관되게 확인, "PIPA" 호칭은 `[W-PGNCS]`에 캡션으로 존재, 상세 단계는 [부분]]
- 정렬: IMU는 P51/P52(CM, 별 관측)와 P57(LM 달 표면 정렬)로 REFSMMAT 기준에 정렬한다. [확인: `[COMP]`, `[LUM-SRC]` P51-P53]
- 경고 램프: GIMBAL LOCK은 중간 짐벌 각이 70도를 넘을 때, NO ATT는 IMU가 자세 기준을 줄 수 없을 때 점등. [확인: `[CIS]`] (일부 문서의 85도 등 수치는 [미확인])

### 5.4 백업 계층

| 단계 | CM | LM |
|---|---|---|
| 1차 | PGNCS(AGC) | PGNCS(AGC, LGC) |
| 2차 | SCS (아날로그) [미확인] | AGS (독립 컴퓨터) [부분] |
| 감시 | EMS (재진입/SPS delta-v) [부분] | - |
| 지상 | 상태 벡터 업링크, 텔레메트리 | 동일 |

- EMS delta-V 카운터는 G&N 고장 시 SPS 차단 백업으로 쓰인다. TEI에서 Collins가 EMS Delta-V Normal 스위치를 끄고 컴퓨터 컷오프와 거의 동시에 정지. [확인: `[AFJ-TEI]`]
- LM 상승에서 지상은 "guidance recommendation is PGNS"라고 조언했다(124:04:51). 하강 중 AGS는 크루가 모니터했고(`AGS` 호출), 상승 중 "Guidance steering in the AGS"(124:20:05) 교신이 있다. [확인: `[ALSJ-LCH]`]

---

## 6. 추진 / 자세 제어 시스템과 AGC의 제어 방식

### 6.1 추진 시스템 정리

| 시스템 | 운반체 | 엔진 | 추력(출처) | 비고 |
|---|---|---|---|---|
| SPS | SM | AJ10-137 | 20,500 lbf | LOI/TEI/MCC `[W-CSM]` [부분] |
| DPS | LM 하강단 | (스로틀형) | 10,125 lbf, 10~60% 스로틀 | 하강 `[W-LM]` [부분] |
| APS | LM 상승단 | - | 3,500 lbf | 상승/CSI `[W-LM]` [부분] |
| CM RCS | CM | 12 x 93 lbf | - | 재진입 `[W-CSM]` [부분] |
| SM RCS | SM | 16 x 100 lbf | - | 자세/MCC-5/ullage `[W-CSM]` [부분] |
| LM RCS | LM | 16 x 100 lbf | - | 자세/랑데부 `[W-LM]` [부분] |

### 6.2 AGC의 엔진 연소 프로그램 흐름 (P40 예시: Apollo 11 TEI)

TEI 때 AFJ 교신에서 확인되는 순서다. [확인: `[AFJ-TEI]`, `[AGC-P40]`]

1. 지상이 PAD로 TIG(135:23:41.56), delta-V(LV) 성분, 자세를 전달. 승무원이 P30 입력(상세 키 순서는 [미확인])
2. `V37 E 40 E`로 P40 선택 (135:07:59)
3. `V06 N40` 표시: R1 점화/컷오프까지 시간, R2 획득해야 할 속도(VG, 시작 3,283.6), R3 누적 속도(0에서 시작)
4. 점화 5초 전 V99 깜박임 ("PLEASE ENABLE ENGINE") → 승무원이 PRO(PROCEED)
5. 점화(135:23:42) → TVC DAP가 짐벌로 추력 방향 조종 → VG가 0이 되면 컷오프(2:28)
6. 컷오프 후 잔여 속도 확인(Delta-VGX 0.1, VGY 0.9, VGZ 0.1 등)

TVC 짐벌 초기 트림은 승무원이 GPI 엄지휠로 설정(약 -0.6도 피치, +0.6도 요). [확인: `[AFJ-TEI]`]

### 6.3 TVC DAP / RCS DAP 개요

- **TVC DAP**: SPS(CM) 또는 DPS(LM)의 짐벌/추력 방향으로 자세 오차를 줄이는 자동조종. P40 코드 주석에 "TVC ENABLE", "GIMBAL TRIM... AFTER A 4 SECOND DELAY, ENGINE COMMANDED TO PRE-COMPUTED TRIM POSITION", SPS 추력 후 RCS DAP로 복귀 시 2.5초 지연이 있다. [확인: `[AGC-P40]`]
- **RCS DAP**: 비연소 구간의 자세 유지/기동, 데드밴드 로직. V48(DAP 데이터 로드 루틴 R03)로 DAP 매개변수를 설정한다. [확인: `[ASM]`]
- **재진입 DAP**: `CM_ENTRY_DIGITAL_AUTOPILOT.agc`에 "PREDICTIVE ROLL SYSTEM ENTRY STEERING PROVIDES ROLL COMMAND IN LOC ROLLC", 2초 주기 유도, 0.1초 주기 각속도 계산 등이 있다. [확인: `[AGC-CE]`] 대기권 밖 DAP(extra-atmospheric)와 구분된다.
- **LM 하강 스로틀**: P63~P67에서 자동 스로틀(Auto-throttle)로 DPS를 제어하고 P66/P67에서 승무원이 ROD 스위치로 하강률을 조절. [확인: `[LUM-SRC]` LUNAR_LANDING_GUIDANCE_EQUATIONS.agc 주석]

### 6.4 구체적 이야기: 연소 프로그램 종류

| 프로그램 | 의미 | 출처 |
|---|---|---|
| P30 | 외부 delta-V 타겟팅(TIG, delta-V LV 입력, 결과 표시) | `[AGC-P30]` [확인] |
| P40 | SPS(CM) / DPS(LM) 추력 연소 | `[AGC-P40]` [확인] |
| P41 | RCS 연소 | `[AGC-P40]` [확인] |
| P42 | APS 연소(LM) | research B [부분] |
| P63~P68 | LM 하강/착륙 | `[LUM-SRC]` [부분: P64~P67 공식 제목은 [미확인]] |
| P12 | LM 동력 상승 | research B [확인: 제목 ALSJ 해설] |
| P61~P67 (CM) | 재진입 준비~최종 단계 | `[AGC-P61]` [부분: 공식 제목 [미확인]] |

상세한 프로그램별 설명은 `doc/12_Mission_Phase_AGC_Guide.md` 참조.

---

## 7. 승무원 역할 분담과 조작 인터페이스

### 7.1 역할

| 우주선 | 직책 | Apollo 11 인원 | 확인 |
|---|---|---|---|
| CSM Columbia | CDR/CMP/LMP 3명 | Armstrong, Collins, Aldrin | 이름은 교신에서 `[AFJ]` [확인] |
| LM Eagle | CDR(Armstrong), LMP(Aldrin) | 2명 | 하강 교신 `[ALSJ-LND]` [확인] |
| 달 궤도 CSM 단독 운용 | Collins | 1명 | `[AFJ]` [확인] |

- 하강 중 Armstrong이 조종, Aldrin이 DSKY/고도/속도 호출과 컴퓨터 조작(예: V16N68 모니터, V57 칼만 필터 입력)을 담당. [확인: `[EYLES]`, `[ALSJ-LND]`]
- 달 궤도에서 Collins는 CSM의 항법과 TPI 해 독립 계산 등 감시를 수행. [확인: `[AFJ-RDV]`]

### 7.2 DSKY

- 위치: CM에는 DSKY 2개(주 계기판, 하부 장비 베이), LM에는 1개. [부분: "CM 2개/LM 1개"는 작업 요청 사실로 취급, 구체 장착 위치는 [미확인]. 다만 SM 분리 스위치가 "DSKY 바로 아래 패널 2"에 있다는 서술 `[AFJ-D9b]`은 확인]
- 구성: 숫자 키 10개, 부호 키 2개, 명령 키 7개(VERB, NOUN, CLR, PRO, KEY REL, ENT, RSET). [확인: `[COMP]`, `[CIS]`]
- 입력 규칙: 부호 키가 있으면 10진수, 없으면 8진수. 표준 입력은 `Verb V1 V2 Noun N1 N2 Enter`. [확인: `[COMP]`]
- 상세 조작 규칙, 램프 의미, 알람 코드는 `doc/10` 참조.

### 7.3 주요 램프 의미 (참고)

| 램프 | 의미 | 출처/태그 |
|---|---|---|
| UPLINK ACTY | 지상 데이터 수신 중 | `[CIS]` [확인] |
| NO ATT | IMU가 자세 기준을 못 줄 때 | `[CIS]` [확인] |
| GIMBAL LOCK | 중간 짐벌각 70도 초과 | `[CIS]` [확인] |
| KEY REL | 컴퓨터가 DSKY 제어권 필요 | `[CIS]` [확인] |
| PROG | `[CIS]`는 "추가 정보 입력 대기", `[NASSP]`는 "프로그램 알람" | 출처 상충 [부분] |
| RESTART | 컴퓨터 재시작 프로그램 실행 중 | `[CIS]` [확인] |

### 7.4 LM 조종 관련 확인 항목

- `Engine Arm`, `Abort Stage`, `Master Arm`, `Mode Control`, `Descent Engine Command Override`가 상승/착륙 교신에 등장. [확인: `[ALSJ-LND]`, `[ALSJ-LCH]`]
- 착륙 후 AGS 입력은 "Clear, 413, Enter, +10000, Enter" (DEDA). [확인: `[ALSJ-LND]` O'Brien 주석]
- 스위치 패널 배치, 각 스위치 위치, 계기 전체 목록은 [미확인].

---

## 8. 지상 관제와의 협업

### 8.1 업링크/텔레메트리

- AGC는 업링크로 데이터를 받을 때 UPLINK ACTY 램프를 점등한다. 다운링크는 V74 등으로 요청하며 의미는 [미확인]. [확인: `[CIS]`; V74는 교신에서만 확인, [미확인]]
- Apollo 11 LM 표면 정지 중 지상은 "updata link"로 상태 데이터를 올렸다(103:59:41 Aldrin: "The DSKY's yours, and updata link to Data"; 104:03:10 Duke "We got the load in"). [확인: `[ALSJ-POST]`]
- 반응 코드 상세(업링크 포맷, 지상 장비)는 [미확인].

### 8.2 상태 벡터 갱신과 P27

- 하강 전 지상이 LM 상태 벡터를 업링크했다. [확인: research B]
- P27(업링크 데이터 갱신 프로그램 명칭)은 이 작업에서 소스로 확인하지 못했다. [미확인]
- 재진입 직전 최종 PAD(updated state vector, target, new REFSMMAT)가 전달되었다. [확인: `[AFJ-D9a]`]

### 8.3 PAD 데이터 (Apollo 11 예)

| PAD | 내용 | 출처 |
|---|---|---|
| TEI-30 | SPS/G&N: 36691, 트림 -0.61/+0.66, TIG 135:23:41.56, delta-V(LV) +3,201.1/+681.8/-265.0 | `[AFJ-TEI]` [확인] |
| MCC-5 | RCS/G&N: 26025, TIG 150:29:54.53, delta-V 4.8 | `[AFJ-D7]` [확인] |
| 재진입 | EI 195:03:07, 경로각 6.49도, 최대 g 6.4, EI 속도 36,194 ft/s, 착수점 13.32N/169.17W | `[AFJ-D9a]` [확인] |
| PDI abort | 100:22:05에 전달, "less than 10 minutes, 105:12:30.00" | `[AFJ-UND]` [확인] |

### 8.4 임무 규칙과 후속 대응

- 하강 중 1202/1201 알람에 대해 지상(Bales 등)이 위험하지 않다고 판단, "We're Go" 회신(102:42:25경). [확인: `[ALSJ-LND]`, `[EYLES]`]
- 원인: RR이 LGC 모드로 CDU 카운터 펄스를 지속 입력해 Executive 작업 포화. [확인: `[EYLES]`]

---

## 9. 달 착륙 임무의 전체 비행 순서 요약

단계별 AGC 프로그램/DSKY 상세는 `doc/12_Mission_Phase_AGC_Guide.md`를 참조한다. 여기서는 우주선 중심으로만 정리한다.

| MET | 이벤트 | 담당 우주선 | 주 엔진/제어 |
|---|---|---|---|
| 0:00:00 | 이륙 | 새턴 V | S-IC (IU 유도, CMC P11 감시) |
| 0:02:42 | S-IC 종료 | 새턴 V | S-II 점화 |
| 0:09:12 | S-II 종료 | 새턴 V | S-IVB 점화 |
| 0:11:49 | 궤도 진입 | S-IVB/CSM | 185 km |
| 2:44:16 | TLI | S-IVB | J-2 재점화 |
| 26:44:58 | MCC-2 | CSM | SPS(20.9 ft/s) |
| 75:49:50 | LOI-1 | CSM | SPS 357.5 s |
| 80:11:36.75 | LOI-2 | CSM | SPS 16.88 s |
| 100:12 | LM 분리 | CSM/LM | RCS |
| 102:33:05 | PDI | LM | DPS, P63 |
| 102:41:32 | High gate | LM | P64 |
| 102:43:20 | P66 | LM | 수동 보조 |
| 102:45:40 | 접지 | LM | P68 |
| 109:24 | EVA | LM/승무원 | - |
| 124:22:00 | 상승단 이륙 | LM | APS, P12 |
| 124:29:17 | 궤도 진입 | LM | 47.2 x 9.5 nmi |
| 125:19:35 | CSI | LM | APS/RCS |
| 126:17:45 | CDH | LM | RCS 등 [부분] |
| 127:03:52 | TPI | LM | RCS 등 [부분] |
| 128:03 | 도킹 | LM+CSM | RCS |
| 135:23:42 | TEI | CSM | SPS 2:28, P40 |
| 150:29:54 | MCC-5 | CSM | RCS 4-quad |
| 194:49:12.7 | SM 분리 | CM | - |
| 195:03:06 | EI | CM | P63~P67 |
| 195:12:06.9 | 드로그 | CM | 낙하산 |
| 195:18:35 | 착수 | CM | - |

(위 표 전체의 출처는 1.2절과 동일. CSI/CDH/TPI의 사용 추진 시스템은 Research B에서 "APS for CSI, RCS for TPI/TPM" 기재 - CDH는 [미확인].)

---

## 10. 이 프로젝트에서의 표현

`src/Agc.Mission/Apollo11Mission.cs`의 `BuildKeyframes()`가 보간 입력으로 사용하는 키프레임과 실제 이벤트의 대응이다. 파일 주석이 명시하듯 값은 "공개된 미션 기록 기반 반올림 근사값이며 실제 텔레메트리가 아니다".

| 키프레임(코드) | MET(코드) | 실제 이벤트(근거) | 차이/한계 |
|---|---|---|---|
| 발사 | 0:00:00 | 이륙 | 일치 [확인] |
| 1단 분리 | 0:02:42 | S-IC 연소 종료 ~2:42 | 일치(근사) [확인] |
| 2단 연소 종료 | 0:09:12 | ~9:12 | 일치 [확인] |
| 지구 저궤도 진입 | 0:11:49 | ~11:49, ~185 km | 일치 [확인] |
| TLI 점화 | 2:44:16 | MR 2:44:16 / 교신 2:44:18 | 2초 차이 [부분] |
| TLI 종료 | 2:50:03 | 약 2:50 | 초 단위 근거는 [미확인] |
| 달 궤도 진입(LOI-1) | 75:49:50 | 75:49:50 | 일치 [확인] |
| LM 분리 | 100:12:00 | ~100:12 (AFJ는 undock 100:12:03/100:18:01 표기 혼재) | 근사 [부분] |
| PDI | 102:33:05 | Eyles 102:33:05 / ALSJ 102:33:07 | 2초 차이 [부분] |
| 접근 단계 P64 | 102:41:32 | high gate | 일치 [확인] |
| P66 | 102:43:20 | Eyles 102:43:20 | 일치 [확인] |
| 착륙 | 102:45:40 | Contact Light 102:45:40 | 일치 [확인] |
| EVA | 109:24:15 | ~109:24 | 초 단위 [미확인] |
| 상승단 이륙 | 124:22:00 | 124:22:00 | 일치 [확인] |
| 상승단 궤도 진입 | 124:29:15 | 124:29:17 (Shutdown) | 2초 차이 [부분] |
| 도킹 | 128:03:00 | ~128:03 (교신 128:03:12) | 근사 [부분] |
| TEI | 135:23:42 | 135:23:42 | 일치 [확인] |
| TEI 종료 | 135:26:12 | 135:23:42 + 2:28~2:30 = 약 135:26:10 | 근사 [부분] |
| 대기권 재진입 | 195:03:05 | EI 195:03:06(AFJ)/:07(PAD) | 1~2초 차이 [부분] |
| 착수 | 195:18:35 | 195:18:35 | 일치 [확인] |
| 알람 5건 | 102:38:26 ~ 102:42:57 | 출처 간 시각이 수 초 상이 | 근사 [부분] |

### 10.1 수치 근사의 한계

- 코드의 고도/속도/위치는 시각화용 보간값이다. 예: 달 중력권 진입 "61:39:50"과 달 반경 고도 값(72,000 km 등)은 이 작업에서 출처로 검증하지 않았다. [미확인]
- 일부 키프레임(30:00:00, 61:00:00, 150:00:00, 175:00:00)은 사건이 아닌 보간용 점이다. [부분: 코드 설명에서 확인]
- 알람 시각: 코드는 5건(1202 4회, 1201 1회)으로 두었는데, 출처에서는 시각이 서로 달라(예: 첫 1202가 102:38:26 교신 / 102:39:02 Mission Report 표) 코드 값은 대표 근사다. [부분]
- P-number 라벨(예: 재진입 시작을 P63, 착수 직전을 P67)은 AGC 소스 설명과 부합하나, 실제 P62/P64/P65/P66 전환 시각은 키프레임에 모두 반영되지 않는다. [부분]
- 코드 설명 "TLI 종료 속도 약 10.8 km/s" 등은 출처 미확인. [미확인]

---

## 11. 참고 자료

| 약칭 | 출처 | URL |
|---|---|---|
| `[MR]` | Apollo 11 Mission Report (MSC-00171) | https://www.nasa.gov/wp-content/uploads/static/apollo50th/pdf/A11_MissionReport.pdf |
| `[AFJ]` | Apollo 11 Flight Journal (apollojournals.org 미러) | https://apollojournals.org/afj/ap11fj/ |
| `[AFJ-TEI]` | AFJ Day 6 TEI | https://apollojournals.org/afj/ap11fj/21day6-tei.html |
| `[AFJ-D7]` | AFJ Day 7 | https://apollojournals.org/afj/ap11fj/22day7-leave-lsi.html |
| `[AFJ-D9a]` | AFJ Day 9 접근 | https://apollojournals.org/afj/ap11fj/26day9-approach-earth.html |
| `[AFJ-D9b]` | AFJ Day 9 재진입 | https://apollojournals.org/afj/ap11fj/27day9-entry.html |
| `[AFJ-UND]` | AFJ 분리/DOI | https://apollojournals.org/afj/ap11fj/17day5-undock-doi.html |
| `[AFJ-RDV]` | AFJ 랑데부/도킹 | https://apollojournals.org/afj/ap11fj/19day6-rendezvs-dock.html |
| `[ALSJ-LND]` | ALSJ 착륙 | https://apollojournals.org/alsj/a11/a11.landing.html |
| `[ALSJ-POST]` | ALSJ 착륙 후 | https://apollojournals.org/alsj/a11/a11.postland.html |
| `[ALSJ-LCH]` | ALSJ 이륙 | https://apollojournals.org/alsj/a11/a11.launch.html |
| `[EYLES]` | Don Eyles, 알람 설명 | https://klabs.org/history/apollo_11_alarms/eyles_2004/eyles_2004.htm |
| `[AGC-SRC]`/`[ASM]`/`[AGC-P40]`/`[AGC-P61]`/`[AGC-CE]`/`[AGC-P30]` | Comanche055 소스 | https://github.com/chrislgarry/Apollo-11/tree/master/Comanche055 / https://www.ibiblio.org/apollo/listings/Comanche055/ |
| `[LUM-SRC]` | Luminary099 소스 | https://github.com/chrislgarry/Apollo-11/tree/master/Luminary099 |
| `[COMP]` | Parker, "The Apollo On-board Computers" | https://apollojournals.org/afj/compessay.html |
| `[CIS]` | NASA, Computers in Spaceflight | https://www.nasa.gov/wp-content/uploads/static/history/computers/Ch2-7.html |
| `[W-AGC]` | Wikipedia, Apollo Guidance Computer | https://en.wikipedia.org/wiki/Apollo_Guidance_Computer |
| `[W-PGNCS]` | Wikipedia, Apollo PGNCS | https://en.wikipedia.org/wiki/Apollo_PGNCS |
| `[W-SV]` | Wikipedia, Saturn V | https://en.wikipedia.org/wiki/Saturn_V |
| `[W-CSM]` | Wikipedia, Apollo command and service module | https://en.wikipedia.org/wiki/Apollo_command_and_service_module |
| `[W-LM]` | Wikipedia, Apollo Lunar Module | https://en.wikipedia.org/wiki/Apollo_Lunar_Module |
| `[NASSP]` | NASSP wiki (시뮬레이터, 2차) | https://nassp.space/index.php/DSKY |

추가로 참고할 1차 자료(이 작업에서 읽지 못함): Apollo Operations Handbook, Apollo News Reference, 및 Virtual AGC 문서 https://www.ibiblio.org/apollo/ . 이들로 위키 기반 규격의 교차 검증을 권한다.

---

## 12. 미확인 항목

1. Apollo 11(SA-506) 개체별 새턴 V/CSM-107/LM-5 정확한 질량·치수 (위키는 평균/기종 대표값).
2. S-IC/S-II/S-IVB 단 분리의 세부 순서와 시각, 단 사이 사이드 로켓/ullage 구성.
3. IU 컴퓨터의 명칭(LVDC)과 승무원 수동 인계 절차.
4. TLI 후 CSM의 전위·도킹·LM 추출(Transposition, Docking and Extraction) 구체 시각.
5. SLA(우주선-LM 어댑터) 구성.
6. CSM 연료전지 반응물, 물 생산, ECS 구성; LM의 물/냉각/CO2 제거 구성.
7. CDU의 공식 정의, REFSMMAT의 공식 정의, PIPA 세부.
8. SCS(CM 아날로그 백업)의 구성 및 운용 방식.
9. DSKY의 CM 내 정확한 장착 위치(주 계기판/하부 장비 베이), 주요 스위치/계기 전체 목록.
10. LM DPS의 10~60% 사이 스로틀 영역 운용 및 풀 스로틀 사용 구간.
11. 착륙 레이더/랑데부 레이더의 세부 규격.
12. P27(업링크 데이터 갱신), V74(다운링크 요청)의 공식 정의 및 업링크 포맷.
13. P64, P65, P66, P67(LM/CM)의 공식 프로그램 제목; low gate 시각.
14. Apollo 11 CMC 소프트웨어 버전 표기(Colossus 2A / Comanche 055 대 Colossus 249).
15. CSI/CDH/TPI 각 연소에 사용된 추진 시스템(CDH 특히).
16. 낙하산 전개 시 승무원 수동 조작 여부.
17. 코드 키프레임 중 달 중력권 진입 시각(61:39:50), TLI 종료 속도(약 10.8 km/s) 등 보간용 수치.
18. TLI 종료 초 단위(2:50:03), EVA 시작 초 단위(109:24:15)의 근거.
19. LOR 방식 채택 이유(프로그램 역사).
20. 재진입 PAD의 0.05 g 시각이 AFJ 안에서도 195:03:23~:36으로 상이하여 단일값 확정 불가 (출처 간 상충).
