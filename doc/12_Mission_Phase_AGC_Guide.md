# 12. 미션 단계별 AGC 운용 가이드 (Apollo 11)

이 문서는 Apollo 11의 **운용 단계마다 AGC(사령선 CMC, 달착륙선 LGC)가 무엇을 했고, 어떤 프로그램을 돌렸으며, 승무원이 DSKY 키와 스위치로 어떻게 조작했는지**를 정리한다.

- AGC/DSKY의 원리와 키 문법: `doc/10_AGC_Manual.md`
- 우주선(CSM/LM) 구조와 비행 운용: `doc/11_Apollo_Spacecraft_And_Operations.md`
- 이 시뮬레이터의 조작법: `doc/02_Simulator_User_Manual.md`, 개발 계획: `doc/00_Development_Plan.md`

## 0. 읽는 법

### 0.1 확인 수준 태그

모든 주장 끝에는 아래 태그 중 하나를 붙인다. 태그가 없는 문장은 이 문서의 해설/연결 문장이다.

| 태그 | 뜻 |
|---|---|
| [확인] | Apollo 11 당시의 1차 자료(비행 기록 녹취, 미션 보고서, Comanche 055/Luminary 099 소스, Eyles 논문)에서 직접 확인 |
| [부분] | 일부만 확인되었거나, 자료끼리 표현·수치가 다르거나, 요약/2차 자료에만 있음 |
| [일반] | Apollo 11 특정 기록은 없고, 다른 비행(주로 Colossus 3 시대 MIT 사용자 안내서 E-2448)의 일반 절차 |
| [미확인] | 확인하지 못함. 추정하지 않고 비워 둠 |

### 0.2 소프트웨어 버전 주의

- Apollo 11 사령선은 **Comanche 055**(소스 헤더가 "Colossus 2A"라고 부름), 달착륙선은 **Luminary 099**(Luminary 1A 빌드 099)이다. [확인]
- DSKY 키 절차의 상세 자료로 가장 많이 쓴 MIT/IL E-2448 Rev 4(1972)는 **Colossus 3 / Luminary 1E**(Apollo 15~17 시대)용이다. 그래서 E-2448에서만 나온 절차는 [일반]으로 표기한다. 프로그램 번호나 체크리스트 코드가 Apollo 11과 다를 수 있다.
- 같은 Noun 번호도 **CMC와 LGC에서 뜻이 다르다**(예: N62는 CMC에서 VI/HDOT/H, LGC에서 |V|/점화까지 시간/누적 ΔV; N63은 CMC에서 RTGO/VIO/TFE, LGC에서 |V|/고도변화율/계산 고도). 각 단계에서 어느 컴퓨터인지 항상 구분한다.
- MET와 GET는 같은 값이다(발사 = 000:00:00).

### 0.3 시각 표기와 자료 충돌

여러 자료의 시각이 1~2초씩 다른 경우가 많다(비행 저널 녹취 시각 vs 미션 보고서 표). 이 문서는 충돌하는 값을 **나란히** 적는다. 시뮬레이터가 쓰는 값은 `src/Agc.Mission/Apollo11Mission.cs`의 키프레임이다.

### 0.4 출처 약어

| 약어 | 자료 |
|---|---|
| AFJ | Apollo Flight Journal (https://apollojournals.org/afj/ap11fj/ 의 각 페이지) |
| ALSJ | Apollo Lunar Surface Journal (https://apollojournals.org/alsj/a11/) |
| MR | Apollo 11 Mission Report, MSC-00171 (https://www.nasa.gov/wp-content/uploads/static/apollo50th/pdf/A11_MissionReport.pdf). 표는 OCR이 깨진 곳이 있음 |
| E2448 | MIT/IL E-2448 Rev 4 "Users' Guide to Apollo GN&CS Major Modes and Routines" (https://ibiblio.org/apollo/Documents/j2-80-E-2448-REV4_text.pdf), Colossus 3용 |
| C055 | Comanche 055 소스 (https://www.ibiblio.org/apollo/listings/Comanche055/ , https://github.com/chrislgarry/Apollo-11/tree/master/Comanche055) |
| L099 | Luminary 099 소스 (https://github.com/chrislgarry/Apollo-11/tree/master/Luminary099) |
| Eyles | D. Eyles, "Tales from the Lunar Module Guidance Computer" (https://klabs.org/history/apollo_11_alarms/eyles_2004/eyles_2004.htm) |
| Adler | M. Adler, 1201/1202 알람 에세이 (https://www.nasa.gov/wp-content/uploads/static/history/alsj/a11/a11.1201-pa.html) |
| CIS | NASA "Computers in Spaceflight" (https://www.nasa.gov/wp-content/uploads/static/history/computers/Ch2-7.html) |
| COMP | P. Parker, "The Apollo On-board Computers" (https://apollojournals.org/afj/compessay.html) |
| NASSP | NASSP 위키 DSKY 항목 (https://nassp.space/index.php/DSKY), 2차 자료 |

### 0.5 이 문서의 각 단계 구성

각 단계는 ① 개요 ② AGC의 역할 ③ 실행 프로그램 ④ 승무원 조작 ⑤ 관련 하드웨어 ⑥ 지상/교신·판단 ⑦ 이 시뮬레이터에서의 표현 ⑧ 출처 ⑨ 확인 수준 요약 순서로 쓴다.

### 0.6 DSKY 키 입력의 기본 문법 (이 문서에서 쓰는 표기)

- `V37E 40E`처럼 쓰면 VERB 3 7 ENTR, 4 0 ENTR을 뜻한다(`E` = ENTR 키). 녹취에서 "Verb 37, Enter, 40, Enter"라고 말한 것과 같다. [확인: AFJ 079:50:35, 135:07:59]
- `PRO` = PROCEED 키, `KEY REL` = KEY RELEASE, `RSET` = RESET.
- V/N 창이 깜박이는 상태를 `FL`로 적는다(예: FL V50 N25 = Verb 50 Noun 25가 깜박이며 승무원 응답을 기다림). [부분: CIS/COMP는 깜박임을 "컴퓨터가 승무원 동작 필요"로 설명]
- 십진 데이터는 부호가 있으면 십진, 없으면 8진으로 읽는다. [확인: COMP]

---

## 1. 발사 전 점검과 P01–P03/P11 전환

### ① 개요
- MET: 발사 전(T-0까지). 카운트다운 중 G&N 점검이 진행된다. 우주선: 사령선 컬럼비아(새턴 V 꼭대기). [확인]
- 무슨 일: 관성측정장치(IMU)를 발사대 방향에 맞춰 정렬하고, 발사 순간(리프트오프 신호)을 기다려 P11로 넘어간다.

### ② AGC의 역할
- 유도: 없음(발사체 유도는 새턴 IU). 항법: IMU 수평 맞춤(leveling)과 방위각 결정(gyrocompassing)으로 발사 시점의 기준을 만든다. 표시: 발사 방위각 등. [일반: E2448 §2.2.2~2.2.4]
- 리프트오프 신호(새턴 IU가 만듦)를 받으면 P02가 끝나고 P11이 자동으로 시작된다. [확인: C055 P11.agc 헤더, E2448]
- P11 진입 시 CMC 시계를 0으로 맞추고, 궤도 요소 갱신, 평균 중력 항법(AVERAGEG) 시작, 리프트오프 시 짐벌각 저장, 초기 RN/VN 계산, 발사 정렬 기준으로 REFSMMAT 산출/플래그를 만든다. 소스 주석: "TIME2 MUST BE NON-ZERO AT LIFTOFF". [확인: C055 P11.agc]

### ③ 실행 프로그램

| 프로그램 | 이름 | 비고 | 확인 |
|---|---|---|---|
| P00 | CMC Idling | 발사 전 대기. 제목 문구는 직접 인용하지 못함, E2448 목차는 "P00, CMC Power Down"으로 적음 | [부분] |
| P01 | Initialization | IMU 거친 정렬 후 P02로 | [일반] |
| P02 | Gyro Compassing | 리프트오프 신호까지 계속 | [일반] |
| P03 | Optical Verification of Gyro Compassing | 선택. 광학 점검 | [일반] |
| P11 | Earth Orbit Insertion Monitor | 리프트오프 시 자동 시작 | [확인] |

### ④ 승무원 조작

| 순서 | 키/스위치 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 1 | `V37E 01E` | P01 선택(IMU를 발사 방향으로 거친 정렬) | E2448 §2.2.2 | [일반] |
| 2 | (자동) | P01 정렬 완료 시 P02 자동 진입. P02는 V37로 부를 수 없음 | E2448 | [일반] |
| 3 | `V78E` → V06 N29 | 발사 방위각 확인/변경 시작(R1 현재 방위각). 변경은 `V21E`로 로드 후 `PRO` | E2448 | [일반] |
| 4 | `V75E` | 리프트오프 신호가 이상할 때의 **백업 P11 시작**. P02 중에 미리 눌러 두는 것이 권장, 발사 후 P11이 안 뜨면 `ENTR`만 | C055 P11.agc 헤더(V75 존재), E2448(절차) | V75 존재 [확인], 절차 [일반] |
| 5 | `V65E` → FL V06 N41 → `V24E` 수정 → `PRO` → FL V51(MARK) → FL V50 N25 R1=00016 → `PRO` | P03: 고정 방위/고도 표적 두 개를 광학으로 확인. IMU가 45분 이상 gyrocompass 한 뒤, OPT ZERO를 15초 이상 ZERO | E2448 | [일반] |
| 6 | 카운트다운 교신 "G&N, insert your Verb 34" (T-002:12:31), "Verb 34 is clean" (T-002:12:21), "execute H G&N Upline disable" (T-002:12:55), "FDAI source to CMC", "Attitude set IMU"(T-1:05:55 이후) | 발사대 점검 교신. 키 시퀀스 본문은 인용되지 않음. V34의 이 맥락에서의 용도 | AFJ 01launch | 교신 [확인], V34의 용도 [미확인] |
| 7 | P01/P02/P03을 Apollo 11 승무원이 언제 어떻게 눌렀는지 | - | - | [미확인] |

### ⑤ 관련 하드웨어
IMU(안정 부재, 자이로 정렬 루프), PIPA(가속도계), ICDU, 새턴 IU(리프트오프 신호), 광학(P03), OPT ZERO 스위치, FDAI. [일반]

### ⑥ 지상/교신·판단
- MR 8.6.8: 삽입 궤도의 60 ft/s 면외 속도 오차는 0.11도 오정렬에 해당하며 "주로 gyrocompassing 중의 오정렬"이 원인으로 서술됨. 지구 궤도 첫 광학 정렬에서 Z축 자이로 토크각이 이를 뒷받침. [확인: MR]
- 이 맥락에서 P11 전 정렬의 정확도가 TLI 이전 첫 P52까지 영향을 준 것이 요점.

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임: MET 0 "발사"(프로그램 11)부터 시작. P00/P01/P02/P03 및 발사 전 구간은 없다.
- `V35E`(램프 테스트)로 DSKY 점검 시늉은 가능하다. 실제 발사 전 램프 테스트 일정은 [미확인].
- 한계: V37 요청은 타임라인에 나오는 프로그램 번호만 받는다(P01/P02/P03은 `OPR ERR`).

### ⑧ 출처
E2448 §2.2.2~2.2.4, C055 P11.agc (https://www.ibiblio.org/apollo/listings/Comanche055/P11.agc.html), AFJ https://apollojournals.org/afj/ap11fj/01launch.html, MR §8.6.8.

### ⑨ 확인 수준 요약
P11 시작 조건 [확인]; P01~P03 동작과 키 시퀀스 [일반]; Apollo 11 승무원의 실제 키 입력 [미확인].

---

## 2. 발사·상승 (P11)

### ① 개요
- MET 000:00:00 ~ 약 000:11:49. 우주선: 새턴 V(S-IC, S-II, S-IVB)에 실린 CSM. 발사는 1969-07-16 8:32 a.m. EST. [확인: MR]
- 사건: S-IVB 유도 종료로 지구 저궤도 진입(MR: 사령선 컴퓨터의 삽입 궤도 천문력 102.1 × 103.9 mi; MR 요약의 주차궤도 100.7 × 99.2 mi). 진입 시각 000:11:49는 검색 요약 문구에서 얻은 값. [부분]

### ② AGC의 역할
- **새턴을 조종하지 않는다**(정상 시). 새턴 IU가 유도한다. CMC는 (a) 시계를 0으로 맞추고, (b) CM IMU로 AVERAGEG 항법을 하고, (c) 계산된 기준 자세 프로파일과 실제 CM IMU 자세의 차를 **FDAI 오차 바늘**로 보여 주고, (d) **V06 N62**를 2초마다 표시한다. [확인: C055 P11 소스 "VHHDOT ... EVERY 2 SECONDS", E2448]
- 기준 자세: 리프트오프 자세 유지 → 약 +10초에 pitch/roll 다항식 시작 → 약 +133초 POLYSTOP 후 고정. [확인: C055 요약, E2448 일치]
- 선택 기능 "새턴 인수(takeover)": `V46E` + LV GUID 스위치 CMC + 회전 조종기(RHC)로 승무원이 부스터 조종. 백업 기능이며 Apollo 11 사용 여부는 자료에 없음. [일반]
- CMC가 중단(abort)을 판단하는 기능을 가졌는지: [미확인]. 아래 중단 모드 시각은 비행 계획/미션 룰 일정이다.

### ③ 실행 프로그램
P11 "Earth Orbit Insertion Monitor" [확인]. 끝낼 때 `V37E 00E`로 P00 선택(S-IVB 종료 후). [일반: E2448]

### ④ 승무원 조작

| 순서 | 키/스위치 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 1 | (자동) V06 N62 | R1 = VI 관성 속도(fps), R2 = HDOT 고도변화율(fps), R3 = H 고도(n.mi., 패드 반경 기준). 승무원이 발사 단계 큐 카드와 비교 | C055 P11, E2448 표 3.2.1-I | [확인] |
| 2 | `V75E` | 자동 진입 실패 시 백업 | C055 헤더 | [확인] |
| 3 | `V82E` → V16 N44 | 원지점/근지점 고도, 자유낙하 시간. `PRO`로 N62 복귀 | E2448 | [일반] |
| 4 | `N32E` / `N50E` | 근지점 통과 후 시간 / 착수 오차 요청 | E2448 | [일반] |
| 5 | `V46E` | 새턴 인수(백업). R03 DAP 구성 코드 3을 N46 R1에 미리 로드 | E2448 | [일반] |
| 6 | `V37E 00E` | S-IVB 종료 후 P00으로 | E2448 | [일반] |
| 7 | 스위치: LV GUID(IU/CMC) | 정상은 IU. 인수 때만 CMC | E2448 | [일반] |

### ⑤ 관련 하드웨어
새턴 IU, S-IC/S-II/S-IVB, CM IMU/ICDU/PIPA, FDAI 바늘, LV GUID 스위치, RHC(인수 시).

### ⑥ 지상/교신·판단
AFJ 색인의 중단 모드 일정: I-B (000:00:42), I-C (000:01:57), LET 분리 및 II (000:03:17), III S-IVB to COI (000:05:27), IV (000:09:00). 각 시점의 CMC 역할은 서술되지 않음. [부분]

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임: 0:00 "발사"(P11), 0:02:42 "1단 분리"(P11, 67 km, 2400 m/s), 0:09:12 "2단 연소 종료"(P11), 0:11:49 "지구 저궤도 진입"(P00, 185 km). 중간 수치는 보간값이다.
- 지원 표시: `V16 N36E`(AGC 시계=MET 확인). **N62는 미지원**(OPR ERR). 시뮬레이터에는 CMC용 N62가 없다.
- 한계: 1단 분리 MET(0:02:42), 고도/속도는 교육용 근사값. [부분]

### ⑧ 출처
C055 P11.agc, E2448 §3.2.1, MR §4.2, AFJ https://apollojournals.org/afj/ap11fj/01launch.html

### ⑨ 확인 수준 요약
P11/N62/시계 0 [확인]; V82·V46 절차 [일반]; CMC의 중단 판단 기능 [미확인]; 진입 000:11:49 [부분].

---

## 3. 지구 주차궤도 (IMU 정렬 P52, 점검)

### ① 개요
- MET 약 000:12 ~ 002:44(TLI 직전). 우주선: S-IVB에 결합된 CSM. 지구를 약 1.5바퀴 돈다. [확인: MR/AFJ]
- 사건: P00로 전환, 시스템 점검, IMU를 별로 재정렬(P52), 지상의 상태벡터 갱신 업링크(P27), TLI 준비.

### ② AGC의 역할
항법(AVERAGEG 계속, 업링크로 상태벡터 갱신), 정렬(P52로 IMU 자세 기준 재설정, 자이로 토크), 표시(N05 별 각도 차, N93 자이로 토크각). TLI 자체는 S-IVB(IU 유도)가 수행하고 CMC는 감시한다.

### ③ 실행 프로그램

| 프로그램 | 이름 | 확인 |
|---|---|---|
| P00 | CMC Idling | [부분] |
| P27 | CMC Update | [일반: E2448 목차] |
| P52 | IMU Realign | [확인: Apollo 11 교신 + MR 표] |
| P51/P53/P54 | IMU Orientation Determination / Backup IMU Orientation Determination / Backup IMU Realign | [일반: 이름은 E2448] |

### ④ 승무원 조작 — Apollo 11의 첫 P52 (000:40~000:54)

| 순서/MET | 조작 또는 교신 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 000:18:42 | McCandless "we do recommend the P52 alignment." | 지상이 정렬 권고 | AFJ 02earth-orbit-tli | [확인] |
| 000:40:32 | Collins "IMU align. Unstow the optics handles." | 정렬 시작, 광학 손잡이 꺼냄 | AFJ | [확인] |
| (약 000:41~46) | 옵션 3(REFSMMAT), 별 30 Menkent와 37 Nunki | 두 별로 정렬. "Okay, proceed to Menkent" = 자동 광학 구동 | AFJ, MR 표 8.6-I | [확인] |
| 000:47:24 | Armstrong "0.01." | N05 별 각도 차 00001 = 0.01도 | AFJ(해설) | [확인] |
| 000:47:48~48:14 | "Z torque is plus 0.152 instead of 122", "Proceed, torque." | N93 토크각 확인 후 `PRO`로 자이로 토크 | AFJ | [확인] |
| 000:48:34 | 확인(check) 별 34 Atria | 세 번째 별 점검 | AFJ, MR | [확인] |
| 000:49:36~50 | "Verb 37, Enter", "GDC's realigned" | P52 종료, GDC도 IMU에 맞춰 정렬 | AFJ | [확인] |
| 000:53:35 | Aldrin 복창: "Noun 71, we used 30 and 37; four balls 1; Noun 93, plus 00016, plus 00033, plus 00152; GET 00:48:15; check star 34." | 토크각 X=+00016, Y=+00033, Z=+00152 | AFJ | [확인] |
| (대조) | MR 표 8.6-I: 첫 정렬 0:48, 옵션 3, 토크 +0.018 +0.033 +0.152, 별 각도 차 0.01 | X 토크가 복창 00016과 MR 0.018로 다름. AFJ는 설명하지 않음 | MR | 충돌 [부분] |

### ④-2 P52 DSKY 시퀀스 (일반 절차, E2448 §7.2.2.3)

Apollo 11 교신이 확인한 부분(N71, N93, N05, 세 번째 별, PRO로 토크, V37E로 종료)과 일치하는 흐름이다. 정확한 코드는 Colossus 3 기준이므로 [일반].

| 순서 | 표시/키 | 의미 | 확인 |
|---|---|---|---|
| 1 | `V37E 52E` | P52 선택. IMU 상태 점검(알람 00210 IMU 꺼짐, 00220 REFSMMAT 없음) | [일반]; 알람 코드 [확인: C055 표] |
| 2 | FL V04 N06, R1=00001 선호/00002 공칭/00003 REFSMMAT/00004 착륙지점 | 정렬 옵션 선택. 바꾸려면 `V22E <코드> E` 후 `PRO`. C055 소스도 4가지 옵션(LS, PREF, NOM, REF) 명시 | 코드 번호 [일반], 옵션 존재 [확인] |
| 3 | FL V06 N22 | 제안 짐벌각(바깥/안쪽/가운데). 가운데 짐벌 85도 초과 경고. `PRO` | [일반] |
| 4 | FL V50 N25, R1=00013 | "거친 정렬 하라". `PRO`=일반 거친 정렬(약 15초). `ENTR`=자이로 토크 방식 | [일반] |
| 5 | FL V50 N25, R1=00015 | "천체 포착". `PRO`=CMC가 별 쌍 선택(없으면 알람 00405, `V32E` 재시도), `ENTR`=수동 | [일반]; 알람 00405 [확인: C055] |
| 6 | FL V01 N70 | 별 코드 입력(`V21E <코드> E`). 8진 01~45 별, 46 태양, 47 지구, 50 달 | [일반] |
| 7 | V06 N92 | 광학 샤프트/트러니언(정보 표시). OPT MODE를 MAN으로 | [일반] |
| 8 | FL V51 | "마크하라". 별을 중앙에 맞추고 **MARK** 버튼. MARK REJECT로 재시도. 그 다음 FL V50 N25 R1=00016 | [일반] |
| 9 | FL V01 N71 | 기록된 별 코드 확인(다르면 V21E). `PRO`. 두 번째 별 반복 | [일반]; N71 사용 [확인: AFJ 복창] |
| 10 | FL V06 N05 | 별 각도 차(도). 00000 완벽. `PRO` 수락, `V32E` 거부 | [일반]; N05 [확인: AFJ] |
| 11 | FL V06 N93 | 자이로 토크각(X/Y/Z). `PRO`=토크 실행(CMC MODE FREE, 각도당 약 2초), `V32E`=건너뜀 | [일반]; N93·PRO [확인: AFJ] |
| 12 | FL V50 N25 R1=00014 | 정렬 점검. `PRO`=세 번째 별 점검, `ENTR`=종료 | [일반]; 세 번째 별 [확인] |

사전 조건(일반): IMU 약 15분 전 전원, 광학 계통(OSS) 약 30분 전, OPT ZERO를 ZERO로 15초 전. 숙련 시 약 5분 + 암순응 15분. [일반]

### ⑤ 관련 하드웨어
IMU, ICDU, 육분의(SXT, 약 28배, 시야 1.8도)와 스캐닝 망원경(SCT, 1배, 60도), 광학 CDU, OSS, GDC(SCS 자세 기준), OPT ZERO/OPT MODE 스위치. [일반: E2448]

### ⑥ 지상/교신·판단
- 001:43:57 McCandless "If you can give us P00 and Accept, we have a state vector update for you." → 001:47:06 "We've completed the uplink; the computer is yours." (P27 업링크, 승무원은 P00에서 **ACCEPT** 위치로) [확인]
- 001:59:04 "that's Verb 66" 언급(V66은 CSM 상태벡터를 LM 슬롯에 복사한다는 AFJ 다른 페이지 해설). [부분]
- 이후 P52: 5:35, 5:39, 9:36(옵션 1, 별 30 Menkent/32 Alphecca), 24:14 등(MR 표 8.6-I). [부분: OCR]

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임 0:11:49 "지구 저궤도 진입"(P00). P52 단계/옵션은 없다. `V37E 52E`는 **OPR ERR**(타임라인에 P52가 없음).
- 지원 확장: `V16 N36E`로 MET를 본다. 한계: 광학 정렬, N05/N93/N71 표시 미지원.

### ⑧ 출처
AFJ https://apollojournals.org/afj/ap11fj/02earth-orbit-tli.html, MR 표 8.6-I, E2448 §7.2.2, C055 P51-P53.agc (https://www.ibiblio.org/apollo/listings/Comanche055/P51-P53.agc.html)

### ⑨ 확인 수준 요약
Apollo 11 첫 P52 흐름과 수치 [확인]; 키 시퀀스 전체는 [일반](Colossus 3); X 토크 00016 vs 0.018 [부분].

---

## 4. TLI (S-IVB 점화, 승무원이 한 일)

### ① 개요
- MET 002:12 ~ 002:51. 우주선: S-IVB + CSM(+SLA 속의 LM). S-IVB의 J-2 엔진을 재점화(time base 6)하여 달 전이 궤도로 가속한다. [확인]
- 점화 002:44:16(MR) / 002:44:18(AFJ 녹취, PAO 계획 002:44:14). 정상 연소 약 5:47. 종료 약 002:50:03~06. [확인]

### ② AGC의 역할
CMC는 **감시**한다. 새턴 IU가 연소를 유도한다. 승무원은 FDAI(자세), EMS(속도 계수), 이벤트 타이머, 그리고 CMC의 N62를 본다. MR: 0.5~1.5도 자세 오차 표시는 CMC가 확인해 주지 않았고, CMC 표시는 자세와 속도가 기준 궤적과 정확히 일치했다. EMS가 전방 속도 오차 3.3 ft/s 이내임을 재확인. [확인: MR]

### ③ 실행 프로그램
- **P15 (TLI Initiate/Cutoff)**: E2448은 존재와 절차를 기술하지만 Comanche 055 소스 디렉터리에는 P15 파일이 없고 본문 grep에도 나타나지 않는다. Apollo 11에서 쓰였는지 **[미확인]**(증거는 부정 쪽). 이 시뮬레이터의 TLI 키프레임은 P15로 표시하지만, 실제 Apollo 11 CMC에서는 P00에서 감시했을 가능성이 있으며 확정은 못 한다. [미확인]
- TLI 중 승무원이 CMC에서 어떤 프로그램/DSKY 입력으로 감시했는지는 기록을 찾지 못했다(교신은 "Noun 62" 값만 언급). [미확인]

### ④ 승무원 조작

| 순서/MET | 조작 또는 교신 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 001:44:30 | PAD 낭독 "TLI: 2:35:14; 179, 071, 001, burn time 5:47, 10435.6, 35575." | TLI 패드. 2:35:14는 AFJ 해설이 time base 6과 연결하는 시각, 179/071/001은 자세로 보이며, 연소 5:47. 10435.6과 35575의 열 이름은 인용되지 않음 | AFJ | 숫자 [확인], 열 이름 [부분] |
| 002:12:15 | Aldrin "We've got the Verb 48 in, Verb 83, ORDEAL set, and Sequence Pyro Arm." | V48 = DAP 데이터 로드(R03), V83 = 랑데부 파라미터 표시(R31), ORDEAL 설정, 파이로 시퀀스 무장 | AFJ; V48/V83 의미 C055 동사 표 | [확인] |
| 002:12:53 | "Guidance, IU?" "IU, yes." | 유도원이 IU인지 확인 | AFJ | [확인] |
| 002:44:18 | Armstrong "Ignition! Call it at 15." | S-IVB 점화 | AFJ | [확인] |
| 002:50:06 | "We have cut-off", 35,570 ft/s, 177 n.mi. | 종료 | AFJ | [확인] |
| 002:50:35 | McCandless "we copy the numbers in Noun 62." | 종료 시 N62 값을 지상이 받아 적음 | AFJ | [확인] |
| 002:51:21 | Aldrin "VI of 35579 and the EMS was plus 3.3." | 종료 속도와 EMS 차 | AFJ | [확인] |
| (일반) | P15 사용 시: `V48E`, `V83E`, `V37E 15E` → FL V06 N33(TB6 시각) → FL V06 N14(종료 속도) → V06 N95 …, 종료 시 FL V16 N95, `V34E`로 나감 | Colossus 3의 P15 절차 | E2448 | [일반] |
| 백업 규칙 | 정상 연소 "5 plus 47", 수동 종료 백업 5:53 (Collins 002:21:56) | 연소 시간 기반 백업 | AFJ | [확인] |

### ⑤ 관련 하드웨어
새턴 IU와 S-IVB J-2, EMS(속도계수/ΔV), FDAI, 이벤트 타이머, SEQUENCE PYRO ARM, LV GUID 스위치(백업 시). [부분]

### ⑥ 지상/교신·판단
지상과 승무원은 점화·종료 시각, N62 속도, EMS 차이를 대조한다. 종료 후 "TLI+90분 중단(abort)" P37 패드가 001:35~001:37에 낭독되어 있었다. [확인]

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임 2:44:16 "TLI 점화"(**P15**, 190 km, 7800 m/s) → 2:50:03 "TLI 종료"(P00, 10830 m/s). PROG 창에 15가 표시된다. 위 ③의 한계 때문에 이 P15 표기는 Colossus 3 지식에 기반한 단순화다.
- `V37E 15E`는 타임라인에 15가 있으므로 수락된다. N33/N14/N95/N62 표시는 미지원.

### ⑧ 출처
AFJ https://apollojournals.org/afj/ap11fj/02earth-orbit-tli.html, MR §4.3·§8.6, E2448 §3.2.2, C055 디렉터리 목록.

### ⑨ 확인 수준 요약
시각·교신·감시 역할 [확인]; P15 사용 여부 [미확인]; N62 외 CMC 표시 [일반].

---

## 5. 전이·도킹·추출 (TD&E)

### ① 개요
- MET 약 003:15 ~ 004:40. 우주선: CSM이 S-IVB에서 분리, 180도 회전(전이), LM과 도킹, LM을 SLA에서 추출, 이후 S-IVB 회피 SPS 연소. 도킹 003:24:03.1(MR 인용), 12개 래치 003:57:12, LM 추출 004:16:59.1. [확인: AFJ/MR]

### ② AGC의 역할
CSM RCS **디지털 자동조종(DAP)**이 자세를 제어한다. V48(DAP 데이터 로드)로 초기화. 전이는 분리 후 약 20초에 시작. 피치는 수동으로 약 1 deg/s까지 올린 뒤 DAP에 넘기고, 이때 회전 조종기(RHC)가 중립을 벗어난 채 DAP를 켜면 자세 유지로 바뀌어 회전율이 멈춘다(MR 8.6.1) — 4번의 시도 끝에 성공. 추가 RCS 연료 18 lb. EMS 속도 계수기로 병진을 모니터. [확인: MR §4.4, §8.6.1]

### ③ 실행 프로그램
번호 있는 P 프로그램이 아니라 **DAP**(R03 데이터 로드)가 중심. 회피 연소는 P30 → P40. 다만 이 연소에 대해 P30/P40이 인용된 곳은 없다(패드와 연소 시각만). [부분]

### ④ 승무원 조작

| MET | 조작/교신 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 003:16:29 | "CMC Mode, Auto." | CMC MODE 스위치 AUTO | AFJ 03tde | [확인] |
| 003:17:51 | "Mike, you key a Verb 62?" | V62 의미 | AFJ | [미확인] |
| 003:24:50 | "CMC Mode, Free?" | FREE = 자세를 유지하려 하지 않음(해설) | AFJ | [확인] |
| 003:25:56 | "Rate, High; Att Deadband, Max." | DAP 속도/데드밴드 스위치 | AFJ | [확인] |
| 003:28:42 | "give me that Verb 64." | V64 의미 | AFJ | [미확인] |
| 003:38:07 | Collins(사후): "I had to go back to Accel Command and hit what I thought was an extra Proceed on the DSKY." | DSKY PRO를 한 번 더 눌렀다 | AFJ | 인용 [확인], 키 의미 [미확인] |
| 003:59:24 | 회피 패드 "SPS G&N; 63481, plus 0.95, minus 0.20; GETI 004:40:01.00" | 중량, 짐벌 트림, TIG | AFJ | [확인] |

### ⑤ 관련 하드웨어
SM RCS 쿼드, RHC, SC CONT/CMC MODE(AUTO/HOLD/FREE), BMAG/SCS, EMS, 도킹 프로브/드로그, SLA 패널.

### ⑥ 지상/교신·판단
S-IVB 회피 SPS 연소: MR 표 8.6-II 4:40:01.72~4:40:04.65, 2.93초(OCR 표). [부분]

### ⑦ 이 시뮬레이터에서의 표현
TD&E 키프레임은 없다. MET 3~4시간대는 키프레임 2:50:03(TLI 종료)과 30:00:00 사이를 보간하며 P00이 표시된다. DAP/EMS/도킹 표시 없음.

### ⑧ 출처
AFJ https://apollojournals.org/afj/ap11fj/03tde.html, MR §4.4, §8.6.1, 표 8.6-II.

### ⑨ 확인 수준 요약
사건·DAP 거동 [확인]; V62/V64 키 의미 [미확인].

---

## 6. 달 전이 비행 (중간 수정 MCC-2, P23/P52, 수동 열 제어)

### ① 개요
- MET 약 004:40 ~ 075:49(LOI-1 직전). 우주선: 도킹한 CSM+LM. 약 3일 coast. [확인]
- 사건: 항법 조준(P23), 플랫폼 정렬(P52), PTC(수동 열 제어) 롤, 중간 수정 MCC-2(SPS), MCC-4 취소. MR: 4번의 선택 기회 중 SPS로 약 26.5시간에 한 번만 수행, ΔV 20.9 ft/s. [확인: MR]

### ② AGC의 역할
상태벡터 적분과 지상 업링크(P27) 수락, 광학 항법(P23), 플랫폼 재정렬(P52), SPS 연소 유도(P30 → P40), 자세 기동(V49/R60), PTC 자세 유지(RCS DAP가 X축 주위로 약 3회/시 회전). [확인/부분 혼합: 아래 표]

### ③ 실행 프로그램

| 프로그램 | 이름 | 확인 |
|---|---|---|
| P00 | CMC Idling (업링크 수락 대기) | [부분] |
| P23 | Cislunar Navigation (E2448 이름) | 사용 [확인: MR/AFJ], 이름 [일반] |
| P52 | IMU Realign | [확인] |
| P30 | External Delta-V (소스 주석 "EXTERNAL DELTA-V TARGETTING PROGRAM") | [확인] |
| P40 | SPS Maneuver (소스에서 SPS 추력 프로그램) | [확인] |
| P37 | Return-to-Earth. **패드 데이터로만 낭독**, 실행되지 않음 | [확인] |
| P27 | CMC Update | [일반] |

### ④ 승무원 조작

**(a) MCC-2 (TIG 026:44:57.92, 패드 읽기 025:49:20)**

| 순서 | 키/스위치 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 1 | P00에서 지상 업링크(상태벡터 갱신) — "go P00 and Accept" | 업링크 수락 | AFJ 05day2-mcc | [확인] |
| 2 | 패드 낭독: "SPS/G&N; 63059; plus 0.97, minus 0.20; GET ignition 026:44:57.92" | N47 중량 63,059 lb, N48 피치/요 트림, N33 TIG | AFJ | [확인] |
| 3 | `V37E 30E` → FL V06 N33(TIG 로드) → FL V06 N81(ΔV LV 로드) → FL V06 N42(원지점/근지점/ΔV 확인) → FL V16 N45(마크, TFI, MGA) → `PRO` | P30 | C055 P30 소스 + E2448 | 흐름 [확인](소스), 이 연소에서 승무원이 이렇게 눌렀는지는 [미확인] |
| 4 | `V37E 40E` → FL V50 N18(원하는 짐벌각; `PRO`로 R60 자동 기동) → FL V50 N25 R1=00204(SPS 짐벌 구동 시험; `PRO`) → V06 N40 | P40 | E2448 | [일반]. V37E40E는 LOI-2(079:50:35)와 TEI(135:07:59)에서 [확인] |
| 5 | EMS ΔV 카운터 설정: ΔVC 16.8 fps (ΔVT 21.3 fps) | 백업 카운터 | AFJ | [확인] |
| 6 | TIG-5 s FL V99 N40 → `PRO` | 엔진 점화 허가 | C055 P40 주석, AFJ(LOI-1) | [확인] |
| 7 | 연소 2.91초(계획 3초), 20.9 ft/s. 잔류 약 0.5 ft/s 이하, 승무원은 트림하지 않음("No, we're not going to trim those residuals." 026:46:58) | 결과 | AFJ; MR 표는 26:44:58.64~26:45:01.77, 3.13초(OCR 표) | 충돌 [부분] |

**(b) 항법과 정렬**

| MET | 조작/교신 | 의미 | 확인 |
|---|---|---|---|
| 006:09:31 | Collins "I've got it in the sextant now, and I'm about to split the image and Mark." | P23 육분의 조준. 지구 수평선 별-수평선 | [확인] |
| 006:34:50 | "Our Noun 49 is reading: register 1, plus 08793; register 2, all balls." | N49: ΔR, ΔV, 소스 코드(AFJ 해설: 1 = 광학, 2 = VHF) | 인용 [확인], 의미 [부분] |
| 006:36:57 | Duke "reject the Noun 49 stuff" | 갱신 거부 | [확인] |
| 006:56:17 | "These Auto optics maneuvers or P23s ... don't seem to be going to the substellar point." | 자동 광학 기동이 맞지 않음. MR: LM 영향을 고려하지 않은 자동 기동이어서 첫 별 찾기 어려움, 지상 계산 기동 사용 | [확인] |
| 007:33:03 | "Forty-four is just not bright enough" | 별 44 대신 45 Fomalhaut | [확인] |
| 023:57:25 | N87, V94 | "Proceed on Noun 87 after you get two consecutive measurements equal to within 0.003 degrees", V94 = 플래시 51로 돌아가 자동 기동 재시도 | 인용 [확인], N87 의미 [부분] |
| 024:25:08 | "Apollo 11 is now in Program 52" | 플랫폼 정렬 | [확인] |
| 024:29:40 | "do a Verb 49 maneuver to your substellar point" | V49 = 승무원 정의 기동(R62) | [확인: C055 동사 표] |
| 025:43:56 | V59 "Please calibrate." | 광학 교정 | 인용 [확인], V59 정의 [미확인] |
| 008:24:44 | "do a P52, option 1 preferred, and establish PTC" | PTC 준비 | [확인] |

P23 일반 시퀀스(E2448 §4.2.4): `V37E 23E` → FL V50 N25 R1=00015(천체 포착) → `PRO`(CMC 기동)/`ENTR` → FL V01 N70(교정 천체 코드; `V21E`) → 자동 광학 → MARK → N49 확인 → `PRO`. 천체 코드: 00 행성, 01~45 별, 46 태양, 47 지구, 50 달. [일반]. C055 P20 소스: "TERMINATION V37E00E OR V56E". [확인]

**(c) PTC**
- 008:11:00 Duke "go P00 and Accept. We'll have a PTC REFSMMAT for you momentarily." 008:59 PAO: "rotated about its X axis at a rate of about 3 revolutions per hour". 010:03:46 Collins "waiting our 20 minutes here for all thruster activity to damp out." 010:58:19 "PTC is started now; looks good to us." [확인]
- Apollo 11 CMC에 PTC 전용 프로그램은 없었음(Collins 152:49:21, 귀환 비행 중 "Buzz thinks we should have a PTC program built into the computer"). 자세 제어는 승무원 수동 설정/DAP. [부분]

### ⑤ 관련 하드웨어
육분의/스캐닝 망원경, 광학 CDU, OPT ZERO/OPT MODE, IMU, SPS 엔진과 TVC 짐벌, 뱅크 A/B 밸브, EMS ΔV, RCS(PTC, 울리지), S-band 업링크(UPLINK ACTY 램프), GDC.

### ⑥ 지상/교신·판단
- MCC-1은 불필요로 **취소**(AFJ). MCC-2 수행(근월점 약 175 n.mi.에서 낮춤). MCC-3은 AFJ 해당 페이지에서 확인되지 않음(MR "4개 중 1개만 수행"에서 추론: 수행 안 됨) [부분]. MCC-4는 067:28 "has been deleted from the Flight Plan"(Apollo Control), 069:18:26 Evans "Midcourse number 4 is not required." [확인]
- 약 073:20 지상이 착륙 지점 REFSMMAT, LOI-1 상태벡터, 목표 로드를 P27로 올림. 073:09 P52 옵션 3 표류 점검, 073:42 옵션 1 착륙지점 REFSMMAT 정렬(보정 -0.423/+0.508/+0.111도는 AFJ 값이고 MR 표의 행 정렬과 맞지 않음). [부분]
- P37(귀환 계산): 001:37:11 "P37 format, TLI plus 5: 007:44, 6485, minus 165, 025:06.", 005:45:33 "TLI plus 11 hour P37 format: 013:44, 4793, minus 165, 049:23". 실행하지 않음. [확인]

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임: 30:00:00 "달로 향하는 비행"(P00), 61:00:00 "달 접근", 61:39:50 "달 중력권 진입"(기준 천체가 달로 바뀜). MCC-2/P23/P52/PTC는 별도 단계 없음.
- 할 수 있는 것: `V16 N36E`(MET 약 30시간 근처), `V37E 00E`.
- 한계: 광학/정렬/ΔV 표시 미지원.

### ⑧ 출처
AFJ 04nav-housekeep, 05day2-mcc, 11day4-approach (https://apollojournals.org/afj/ap11fj/ 아래), MR §8.6.3~8.6.5, 표 8.6-I/II/IV, C055 P30,P37.agc, P20-P25.agc, E2448 §4.2.4.

### ⑨ 확인 수준 요약
MCC-2/MCC-4 사건과 수치 [확인]; P23 흐름 [일반]+교신 [확인]; MCC-2에서 P30 키 입력 [미확인]; MCC-3 [부분].

---

## 7. 달 궤도 진입 LOI-1 / LOI-2 (P30 → P40)

### ① 개요
- LOI-1: TIG 075:49:50.37(MR) / 패드 075:49:49.65, 연소 357.53초(6분 2초), ΔV 2,917.3 fps(889 m/s). 결과 궤도 169.9 × 60.9 mi(MR) / 169.6 × 60.9(AFJ 075:59:11). 달 뒤편 통신 두절 중(LOS 075:41:23, AOS 076:05:30 연소 없을 때/076:15:29 연소 시). [확인]
- LOI-2: TIG 080:11:36(패드)/080:11:36.75(MR), 연소 16.88초, ΔV 약 159.2~159.3 fps. 목표 66 × 54 n.mi., 결과 66.1 × 54.4. [확인]
- 우주선: 도킹한 CSM+LM. 연소는 SPS(20,500 lbf). [확인]

### ② AGC의 역할
P30으로 TIG/ΔV 입력 및 사전 점검(원지점/근지점/ΔV 크기/MGA 계산), P40이 자세 기동(R60), 짐벌 구동 시험, 점화 카운트다운, 울리지, SPS 점화 허가, 교차곱 조종(cross-product steering)으로 VG를 0으로 줄이다 컷오프, 연소 후 잔류 표시. [확인: C055 P40 소스, E2448, AFJ]

### ③ 실행 프로그램
P30 External Delta-V, P40 SPS Maneuver(소스: SPS 추력 20,500 lb, RCS 울리지 "199.6COS10 LBS", "2.5 SECOND DELAY FOR SPS TAILOFF"), R60 자세 기동, R30(V82 궤도 파라미터), R31(V83 랑데부 파라미터). [확인/일반]

### ④ 승무원 조작

**LOI-1 타임라인 (AFJ 12day4-loi1)**

| MET | 사건/조작 | 의미 | 확인 |
|---|---|---|---|
| 072:51:24~72:53:54 | 패드 낭독/복창: TIG 75:49:49.65, ΔVT 2,917.3 fps, 6분 2초, 예상 HA 169.2/HP 61.0, 자세 롤 358 피치 226 요 347 | N33/N81/N42/N44/N47/N48 값 | [확인] |
| 075:49:15경(TIG-35s) | DSKY가 약 5초 **공백**(컴퓨터가 평균 G 계산 시작), 이후 N40 복귀 | AVERAGEG 시작 | [확인: AFJ] |
| TIG-5 s | V/N 창에 **99 점멸**("5초 남았다") | 엔진 점화 허가 요청 | [확인] |
| 075:49:46 | Collins "I'll proceed on the 99." → `PRO` | 점화 허가 | [확인] |
| 075:49:51 | Armstrong "Burning. We're looking good." | 점화 | [확인] |
| 075:55:49 | "Shutdown." (MR 075:49:50.37 → 075:55:47.90) | 컷오프 | AFJ/MR 시각 차 1~2초 [부분] |
| 075:56:23 | 잔류 "minus 1, minus 1, plus 1" | N85. MR: 트림 후 0.1 ft/s | [확인] |
| 075:59:11 | 결과 궤도 | | [확인] |
| 075:59:43~076:00:07 | `V82E`(원지점/근지점), N85 잔류, V66(LM 슬롯에 상태벡터 복사), V83, V48(DAP 데이터) | 연소 후 점검 | 인용 [확인], V66 의미 [부분] |

- SPS 밸브 뱅크: AFJ는 "자동으로 뱅크 A로 시작, 뱅크 B는 수동", MR §4.6은 "다른 SPS 연소와 같으나 뱅크 A 대신 뱅크 B 밸브로 시작"이라고 서술해 **서로 다르다**. [부분]
- 울리지: AFJ 요약은 LOI-1에서 울리지 연소를 설명하지 않음. [부분]

**LOI-2 타임라인 (AFJ 14day4-loi2)**

| MET | 사건/조작 | 확인 |
|---|---|---|
| 079:50:16 | "Verb 41, Noun 91" — 광학 거친 정렬(광학을 로드된 샤프트/트러니언으로 구동) | [확인] |
| 079:50:35 | Collins "Verb 37, Enter. 40, Enter." → P40 | [확인] |
| 079:53:42 | "19 seconds to ullage; two jets." (울리지 2제트) | [확인] |
| 079:54:06~23 | P30이 준 HA/HP 확인(65.6, 54.6) | [확인] |
| 080:11:39 | "A, open." (뱅크 A만 사용) | [확인] |
| 080:11:53 | "Shutdown!" | [확인] |
| 080:13:05~32 | V82(결과 궤도), V83(거리/거리율/세타) | [확인] |
| 080:14:46~51 | V66 | [확인] |

짐벌 점검(1차/2차 TVC 짐벌 모터), "Limit Cycle On/Off", 데드밴드 ±5/±0.5도. [확인: AFJ]

**P40 일반 시퀀스 (E2448 §6.2.1, Apollo 11 교신/소스와 일치하는 부분은 [확인])**

| 순서 | 표시/키 | 의미 | 확인 |
|---|---|---|---|
| 0 | `V37E 40E` | P40 선택 | [확인: AFJ] |
| 1 | FL V50 N18 | 목표 짐벌각 R1 OGA(롤)/R2 IGA(피치)/R3 MGA(요). `PRO`=R60 자동 기동(SC CONT=CMC, CMC MODE=AUTO) | [일반](N18은 E2448만; C055 소스 첫 부분에서 N18 텍스트 미발견) |
| 2 | FL V50 N25 R1=00204 | SPS 짐벌 구동 시험. `PRO`=실행, `ENTR`=생략 | [일반] |
| 3 | V06 N40(매초 갱신) | R1 TIG 카운트다운, R2 VG, R3 누적 ΔV. TIG-30~-25초에 R3 < 2.0 ft/s를 보아 PIPA 바이어스 확인 | N40 [확인: AFJ TEI/C055], 세부 [일반] |
| 4 | TIG-35 s 공백 5초 후 N40 복귀 | AVERAGEG 시작 | [확인: AFJ] |
| 5 | 울리지 → FL V99 N40 (TIG-5 s) | `V34E`=중지, `ENTR`=RCS로 계속, `PRO`=SPS 점화 허가 | V99/PRO [확인], V34E/ENTR 분기 [일반] |
| 6 | 점화, SPS THRUST 램프, 0.2~1.0 g | | [일반] |
| 7 | 컷오프 시 FL V16 N40(TFC, VG, ΔV) → `PRO` → FL V16 N85(잔류 VG, 몸체 좌표) → RHC/THC로 0으로 | 잔류 트림 | [일반](V16N85 소스 존재: [확인]) |
| 8 | 추력 실패: FL V97 N40 | | [일반] |

### ⑤ 관련 하드웨어
SPS 엔진(20,500 lbf), 볼 밸브 뱅크 A/B, TVC 피치/요 트림(N48 패드), EMS ΔVC, RCS 울리지 제트, SCS/TVC, GPI.

### ⑥ 지상/교신·판단
달 뒤편에서 연소하므로 지상은 AOS 후 결과를 받는다. 승무원은 잔류 EMS -6.8 fps를 보고도 영(null)으로 만들지 않기로 판단. 연소 후 V82로 궤도를 확인하고 V66으로 LM 상태벡터를 갱신. [확인]

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임: 75:49:50 "달 궤도 진입 연소(LOI-1)"(P40, 110 km, 2600 m/s). LOI-2는 키프레임 없음. `V37E 40E`는 수락.
- 한계: N33/N40/N42/N81/N85, V99, V82 미지원. PROG=40이 표시되는 것이 전부다.

### ⑧ 출처
AFJ 12day4-loi1, 14day4-loi2, 11day4-approach; MR §4.6, 표 8.6-II/III; C055 P40-P47.agc; E2448 §6.2.1.

### ⑨ 확인 수준 요약
시각·V99·V37E40E·N40 구조 [확인]; N33/N81 사용과 N18은 [일반]/[부분]; 뱅크 서술 충돌 [부분].

---

## 8. 달 궤도 작업과 LM 분리 (P22, 분리)

### ① 개요
- MET 약 076 ~ 100:12. LOI-2 후 달 궤도 13~14회전. LM 점검(Eagle checkout), CSM의 랜드마크 추적(P22), P52 정렬, 100시간경 LM 분리. [확인]

### ② AGC의 역할
CSM: 랜드마크 추적으로 궤도/착륙 지점 지식 개선(P22), P52 정렬. 정렬 기준: CSM 플랫폼이 LM IMU 정렬의 기준(도킹 인터페이스 오정렬 2.05도 보정, MR 9.6.2). [확인: MR]

### ③ 실행 프로그램

| 프로그램 | 이름 | 확인 |
|---|---|---|
| P22 | Orbital Navigation (E2448 이름) | 사용 [확인], 이름 [일반] |
| P20 | Universal Tracking and Rendezvous Navigation (E2448); Apollo 11 소스 제목은 "RENDEZVOUS NAVIGATION PROGRAM 20", 호출 `V37E 20E` | [확인/일반] |
| P52 | IMU Realign | [확인] |

### ④ 승무원 조작

| MET | 조작/교신 | 의미 | 확인 |
|---|---|---|---|
| 080:51:23 | 패드 "T1, 82:37:35; T2, 82:42:50" | 랜드마크 A1 추적 | [확인] |
| 081:05 | P52 옵션 3, 별 37 Nunki와 44 Enif, 보정 +0.046 X, +0.051 Y, -0.028 Z도, 오차 0.02도 | | [부분](MR 표 행이 불분명) |
| 082:43:00~45:29 | 마크 5회(약 30초 간격). 랜드마크 A1("KAMP" 분화구, Mare Spumans, 약 2.000 N, 65.500 E) | P22 | [확인] |
| 082:47:07 | N49 | | [확인] |
| 082:50:27 | Duke "We see a Noun 89. You can do the Verb 34 now." | N89 = 랜드마크 위도/경도/2/고도, V34 = 종료 | [확인] |
| 082:52:26 | Collins "All that procedure for P22 seemed to work very well." | | [확인] |
| 096:55 | P52 옵션 1(별 4 Achernar, 34 Atria), 큰 토크(-1.166, -0.690, +0.456) | 착륙용 REFSMMAT | [부분] |
| 098:49, 104:39, 122:24 | 이후 랜드마크 추적 각 5마크, "Sextant, manual-resolved" | MR 표 8.6-V | [부분] |

- P22 키 시퀀스 본문(V37E 22E 등)은 AFJ에 인용되어 있지 않음. [미확인] E2448의 일반 절차는 P23과 유사하나 이 문서는 인용하지 않는다.
- 소스에서 확인된 랜드마크 표시: V05N70/V05N71(랜드마크 코드), V06N89. [확인: C055 P20-P25]

**LM 분리**
- MR §7.4.5: "undocked ... at about 100 hours during lunar revolution 13", 이후 CSM이 **3-임펄스 분리 시퀀스**: 실제 연소 9초, ΔV 2.7 ft/s. MR 표의 "Undocking 100:12:00"(OCR 열 밀림)과 "Separation maneuver 100:39:52.9"(OCR). LM 쪽 AFJ(17day5-undock-doi): "there you go" 100:12:03, "Eagle's undocked" 100:18:01. **시각이 서로 다름**: 100:12:00(MR 표 OCR, 시뮬레이터 키프레임) / 100:12:03 / 100:18:01. [부분]
- MR §4.9: 분리 시 LM에 준 속도 0.4 ft/s, CSM은 40 ft 점검 거리까지 유지. [확인]
- 분리 직전 LM IMU는 CSM 플랫폼 기준으로 정렬. [확인: MR 9.6.2]

### ⑤ 관련 하드웨어
육분의/광학, SCT, RCS(P22 피치 기법으로 육분의 사용 중 자세 제어: MR §8.6.6), VHF 레인징(N49 소스 2), 도킹 프로브, LM IMU.

### ⑥ 지상/교신·판단
083:29:29 비상 귀환 TEI-11 패드(N33 TIG 98:05:24.22, N47 37,200 lb, N48 -0.60/+0.47). 실행되지 않음. [확인]. P22는 LM 착륙 후 지표의 LM을 찾는 광학 지향에도 쓰였으나 성공하지 못함(MR). [확인]

### ⑦ 이 시뮬레이터에서의 표현
키프레임 100:12:00 "LM 분리"(P00, 달 궤도 110 km). 그 앞 구간(76~100시간)은 보간. P22/P52 단계 없음. `V37E 22E`/`V37E 52E`는 OPR ERR.

### ⑧ 출처
AFJ 15day4-eagle-checkout (100,000자까지만 읽음), 17day5-undock-doi; MR §4.9, §7.4.5, §8.6.6, 표 8.6-V; C055 P20-P25.agc.

### ⑨ 확인 수준 요약
P22 사용 [확인]; P22 키 시퀀스 [미확인]; 분리 MET [부분].

---

## 9. 하강 궤도 진입 DOI와 PDI 준비 (P63 선택)

### ① 개요
- MET 약 100:12 ~ 102:33. 우주선: Eagle(LM, LGC=Luminary 099). 독립 비행 후 **DOI**(Descent Orbit Insertion): LM 하강 엔진(DPS)으로 근월점을 낮춤. 패드 TIG 101:36:14.07(100:20:32에 낭독), 연소 약 30초, 승무원 보고 "the burn's complete. It was on time - residuals are nulled, and AGS's free." (101:37:47). 정확한 점화 MET는 패드 값 외에 [미확인]. [부분]

### ② AGC의 역할
LGC(PGNCS)가 DOI를 유도(P40이 LM DPS 연소 프로그램). PDI 전: IMU 정렬(P52), 착륙레이더(LR)·랑데부레이더(RR) 준비, P63 선택과 카운트다운.

### ③ 실행 프로그램

| 프로그램 | 이름 | 확인 |
|---|---|---|
| P30 | "ACCEPT ASTRONAUT INPUTS OF TIG, DELV(LV)", 표시 V06N33, V06N81, V06N42 | [확인: L099] |
| P40 | LM DPS 연소: "TO COMPUTE A PREFERRED IMU ORIENTATION AND A PREFERRED VEHICLE ATTITUDE FOR A LM DPS THRUSTING MANEUVER". 표시: V50N25(R1=203 "A/P TO PGNCS, AUTO-THROTTLE MODE, AUTO ATTITUDE CONTROL"), V50N99("PLEASE PERFORM ENGINE ON ENABLE"), V06N40, V16N40, V16N85, V05N09, V50N07("PLEASE SELECT P00") | [확인: L099] |
| P41/P42 | RCS / APS 연소 | [확인: L099] |
| P52 | IMU 정렬: "ALIGNS THE IMU TO ONE OF THREE ORIENTATIONS SELECTED BY THE ASTRONAUT" | [확인: L099] |
| P20 | RENDEZVOUS NAVIGATION PROGRAM 20: 목적은 랑데부 레이더를 "STARTUP THROUGH ACQUISITION AND LOCKON TO THE CSM"까지 제어하는 것 | [확인: L099] |
| P63 | THE LUNAR LANDING, BRAKING PHASE | [확인: L099 헤더] |

### ④ 승무원 조작

| MET | 조작/교신 | 의미 | 확인 |
|---|---|---|---|
| 099:34:48 | "Are you in AGS Cal attitude now?" | AGS 교정 | [확인] |
| 099:46:37 | DPS 가압 | | [확인] |
| 100:22:05 | PDI 중단(abort) 패드 "less than 10 minutes, 105:12:30.00" | | [확인] |
| 101:17:07 | P52 기동 중 S-band 고이득 안테나가 stop에 걸림 | | [확인] |
| 101:47경 | P52 정렬 | | [부분] |
| 101:57:14 | "Now I need a Verb 76" | V76의 용도 | 인용 [확인], 의미 [미확인] |
| 101:59:34, 102:00:44 | "134 17 ... Key Release, Proceed", "13; Key Release; Proceed" | 의미 | [미확인] |
| 102:06:20 | "We haven't got P20 going yet, have we?" | P20 지연 | [확인] |
| 102:06:24 | "Let's get the Rendezvous Radar circuit breaker in." | RR 전원 | [확인] |
| PDI 직전 | **RR 모드 스위치를 LGC 위치로** 두고 RR 전원 켬("RR switched on just before P63 was selected") | 이것이 1201/1202의 원인(10절) | [확인: Eyles] |
| PDI 직전 | `V37E 63E`("Verb 37 Noun 63") | P63 선택 | [확인: Eyles 문구], 키 순서 형태는 [부분] |
| DOI | V37E 30E/40E 키 시퀀스와 N 입력 | | [미확인] |

P63 소스 주석: P63 선택 후 R51 표시 "PROCEED will do a fine alignment / ENTER will return to P63SPOT2", P63SPOT4에서 "INITIALIZE LANDING RADAR". [확인: L099 코드 주석]

### ⑤ 관련 하드웨어
DPS, AGS와 DEDA, PGNCS IMU, 착륙레이더(LR), 랑데부레이더(RR), RCS, S-band 안테나.

### ⑥ 지상/교신·판단
DOI 전에 지상이 LM 상태벡터를 업링크. PDI 전 Go/No-Go는 지상과 교신(상세 [미확인]).

### ⑦ 이 시뮬레이터에서의 표현
키프레임 102:33:05 "동력 하강 시작(PDI)"(P63, 15 km, 1700 m/s). DOI 구간(100:12~102:33)은 키프레임 없음. `V37E 63E`로 PROG를 63으로(이미 63인 구간). 점화 약 1분 전(102:32:05, 시작 시점은 근사값 [미확인])부터 V16 N62(점화까지 시간 카운트다운)가 자동 표시되고, 102:33:00(T-5 s)에 V99가 점멸(허가 요청)하며 `PRO`를 누르면 점멸이 멈추고, 102:33:05에 V16 N63으로 바뀐다. 점멸 표시가 V99라는 점은 P63에서는 [미확인](P40 소스에서만 확인). 1201/1202 알람이 지나가면 Executive 재시작으로 승무원이 띄운 V16 N68 모니터가 사라지고 표시가 프로그램의 N63으로 돌아온다(Eyles, [확인]). 한계: N61 미지원, RESTART 램프 미구현.

### ⑧ 출처
AFJ 17day5-undock-doi; L099 P30_P37.agc, P40-P47.agc, P51-P53.agc, P20-P25.agc, THE_LUNAR_LANDING.agc; Eyles.

### ⑨ 확인 수준 요약
프로그램 명칭·표시 [확인]; DOI 키 시퀀스·점화 MET [미확인]; 일부 교신 의미 [미확인].

---

## 10. 동력 하강 (P63 → P64 → P66 → P68) — 1202/1201 알람 상세

### ① 개요
- MET 102:33:05 ~ 102:45:40. 우주선: Eagle(LM), 승무원 Armstrong(사령관, 조종), Aldrin(LM 파일럿, DSKY 조작: PROCEED, V16N68, V57을 누름). 하강 엔진(DPS)이 달 궤도(약 5,560 ft/s, 약 50,000 ft)에서 착륙까지 감속한다. [확인: Eyles]
- 이 단계는 자료가 가장 풍부하다. 시간순 표를 먼저 보이고, 알람 해설을 이어서 쓴다.

### ② AGC의 역할 (LGC, Luminary 099)
- **유도**: 제동 단계 유도 방정식으로 추력 방향/크기를 계산. 점화 후 약 26초 뒤 스로틀이 10%에서 약 94%(9,870 lb)로 올라감. [확인: Eyles]
- **제어**: DAP가 RCS로 자세 제어, 자동 스로틀.
- **항법**: 평균 G(Average-G) 서보서(SERVICER) 작업이 2초 주기로 IMU 가속도를 적분. LR(착륙레이더) 데이터가 상태벡터에 반영되려면 **승무원과 지상의 승인**이 필요(V57). [확인: Eyles]
- **감시/표시**: 자동 모드 감시(R13 "GUILDENSTERN: AUTO-MODES MONITOR"), 표시 V06N62 → N63 → N64 등. [확인: L099 주석, Eyles]
- **과부하**: 알람 1202/1201(11절 아래 해설).

### ③ 실행 프로그램

| 프로그램 | 이름/기능 | 확인 |
|---|---|---|
| P63 | "THE LUNAR LANDING, BRAKING PHASE". 목적: 수평 속도를 깎는 것("its only purpose was to shed horizontal velocity"). 궤도 속도 약 5,560 fps(약 50,000 ft)에서 접근 속도 450 fps(약 7,300 ft)까지(NESC 메모 요약) | 제목·목적 [확인], 수치는 [부분](요약문) |
| P64 | Eyles: "the mode register changed to 64 indicating the **Visibility Phase**". 공식 한 줄 제목은 [미확인]. N64 표시. 자동 전환 | 부분 |
| P65 | 소스: "WCHPHASE = 2 ---> VERTICAL: P65,P66,P67", "P65 GUIDANCE EQUATION" (자동 수직 하강 유도). 공식 제목 [미확인] | 부분 |
| P66 | Eyles: "the rate-of-descent mode (P66)". 소스: 태도유지 신호 또는 ROD 스위치 "클릭"으로 선택. Fjeld(ALSJ): MIT의 "enhanced P66"으로 수평 영점화를 자동/수동 전환 가능. 공식 제목 [미확인] | 부분 |
| P67 | 수동 스로틀 신호 출현 시 선택(소스 주석). Apollo 11 하강에서는 Eyles가 언급하지 않음 | 부분 |
| P68 | "LANDING CONFIRMATION" | [확인: L099 헤더] |

### ④ 승무원 조작과 사건 — 시간순

| MET | 사건 | 조작/표시 | 출처 | 확인 |
|---|---|---|---|---|
| PDI 전 | PDI 준비 | RR 전원/LGC 모드(9절), `V37E 63E` | Eyles | [확인] |
| 점화 전 | 카운트다운 표시 | **V06 N62** 표시(점화까지 카운트다운; N62 = \|V\| / 점화까지 시간 / 누적 ΔV) | Eyles, L099 노운 표 | [확인] |
| 점화 -35 s ~ -30 s | 표시 공백 35 s → 30 s에 복귀 | **Average-G 시작** | Eyles | [확인] |
| 점화 -7.5 s | 울리지 | | Eyles | [확인] |
| -5 s | 표시 점멸("go" 요청) → **Aldrin이 `PRO`** | 점화 허가. 점멸 표시의 verb/noun은 자료에 없음(P40의 V99는 소스에서 확인되나 P63 표시는 [미확인]) — 시뮬레이터는 V99를 점멸시킴 | Eyles; ALSJ 102:33:05~08 "Proceed. One, Zero." | 동작 [확인], V/N [미확인] |
| 102:33:05 | **PDI 점화**, 10% 스로틀 | | Eyles (ALSJ 녹취는 "Ignition" 102:33:11, ALSJ 주석은 실제 PDI 102:33:07·계획 102:35:13) | **충돌** [부분] |
| 약 +26 s | 스로틀 업(9,870 lb, 약 94%) | ALSJ 녹취 "Throttle up" 102:33:25 | Eyles; ALSJ | [확인] |
| 직후 | **N63** 표시: 5559.7 ft/s, 고도변화율 -2.2 ft/s, 고도 49,971 ft | | Eyles | [확인] |
| 102:33:03~38:04 | LR 고도 램프 "on" → 레이더 록(약 102:38:04) | | ALSJ | [부분] |
| 102:36:55 | Armstrong이 **rate switch 5 → 25 deg/s** | 자세 조종 감도 변경 | 제공된 확인 사실(Eyles) | [확인] |
| 약 102:37~ | LR "data good" | | Eyles | [확인] |
| 약 +5분(약 102:38) | Aldrin이 **`V16 N68E`**로 DELTAH 감시. N68 R3(LR 고도 - 계산 고도) 약 -2,900 ft ("Delta-H is minus 2,900", 102:38:11) | LR 고도와 계산 고도의 차 | Eyles; ALSJ | [확인] |
| 직후 | Houston이 레이더 데이터를 승인 → **`V57`**로 항법이 LR을 수용 | 정확한 키 순서(예: V57E 뒤 입력) | Eyles | 동작 [확인], 키 순서 [미확인] |
| 102:38:26 | **PROG 램프**, Armstrong "Program Alarm." 102:38:30 "It's a 1202." (알람 코드는 V05 N09로 읽음) | 1번째 1202 | ALSJ 녹취; MR는 102:39:02 | **충돌** [부분] |
| 102:39:02 | MR의 1202 일시. 102:39:14 Aldrin "Same alarm, and it appears to come up when we have a 16/68 up." | 2번째 1202 | MR(ALSJ 주석), ALSJ | [부분] |
| 102:39:01~35 | "6 plus 25. Throttle down..." / "Throttle down on time" 102:39:35, 제공 사실 102:39:31 | 스로틀 다운(계획대로) | ALSJ; 제공 사실 | 시각 1~4초 차 [부분] |
| 약 102:40 | DELTAH가 약 900 ft로 줄어듦 | | Eyles | [확인] |
| 102:41:05 | Houston에 N68 정보를 요청해 승무원이 N68을 안 띄우도록(알람 회피) | | ALSJ(요약 표현) | [부분] |
| 102:41:32 | **하이 게이트**: 고도 7,400 ft, 하강율 125 ft/s → **P64**. 102:41:35 Armstrong "P64." | 자동 전환 | Eyles; ALSJ | [확인] |
| 102:42:17 | 1201 (ALSJ: Aldrin "Program Alarm. 1201." 102:42:19, Duke 102:42:25 "same type") | VAC 영역 부족 | Eyles; ALSJ | 시각 2초 차 [부분] |
| +24 s | 1202 | | Eyles | [확인] |
| +16 s | 1202 (고도 770 ft, 27 ft/s) | | Eyles | [확인] |
| 102:42:32 | "Give me an LPD" | 착륙점 지정 각도 읽기(N64 R1에 LPD 각도) | ALSJ, L099 | [확인] |
| 102:43:08 | 650 ft: Armstrong이 **AUTO → ATT HOLD** | 자세 모드 | 제공된 사실 | [확인] |
| 102:43:20 | 430 ft: **P66** 진입, **ROD 스위치**(스프링 복귀형)로 수동 하강율 제어. DSKY가 아님 | | 제공된 사실; ALSJ 102:43:22 | [확인] |
| 102:44:31 | 연료 Quantity 램프 | | ALSJ | [확인] |
| 102:44:45 | "Five percent" | | ALSJ | [확인] |
| 102:45:40 | **접촉(Contact Light)**/착륙 | | ALSJ | [확인] |
| 102:45:43 | "Shutdown" | | ALSJ | [확인] |
| 102:45:44 | "Okay. Engine Stop." | ENGINE STOP | ALSJ | [확인] |
| 102:45:46 | "Out of Detent. Auto." | 자세 | ALSJ | [확인] |
| 102:45:47 | "Mode Control, both Auto. Descent Engine Command Override, Off. Engine Arm, Off. 413 is in." | 스위치 상태 + AGS에 "413" 입력 | ALSJ | [확인] |
| 102:45:47 이후 | **DEDA(AGS)**: "Clear, 413, Enter, +10000, Enter" | 착륙 후 AGS 시퀀스. 413의 의미는 [부분] | ALSJ(O'Brien 주) | 키 [확인] |
| 102:45:45경 | P68 착륙 확인. Fjeld: "the DAP was cycled with the P68 landing confirmation program", 접촉 시 DAP는 ATT HOLD | | ALSJ | [부분] |

- 노운 정의(L099): N60 수평 속도/고도변화율/계산 고도; N61 제동 잔여시간/점화까지 시간/횡거리; N62 |V|/점화까지 시간/누적 ΔV; N63 |V|/고도변화율/계산 고도; N64 재지정 잔여시간·LPD 각도/고도변화율/계산 고도; N68 착륙점까지 사거리(nm)/제동 잔여시간(min,sec)/LR 고도-계산 고도(DELTAH, ft); N66 LR 거리/위치(R1~R3 대응은 완전 확인되지 않음). [확인: PINBALL_NOUN_TABLES.agc]
- N68: ALSJ(O'Brien 주석)은 "착륙점 사거리, 피치오버까지 시간, 속도"라고 쓰지만 L099 표와 다르다. 이 문서는 소스 표를 따른다. [충돌/확인]
- N69: L099에서 모든 성분이 SPARE. [확인]
- 로우 게이트 MET: [미확인].

### ⑤ 관련 하드웨어
- DPS(10% 점화, 스로틀 업, 스로틀 다운 시점), LR(고도 램프, 위치 코드 500: "LR antenna position" 알람 계열), LPD(창문 눈금), RR(LGC 모드), 하강 연료 Quantity 램프, rate switch, ROD 스위치(스프링 복귀형), 자세 모드(AUTO/ATT HOLD), DEDA.
- **알람의 하드웨어 원인**: RR 모드 스위치가 LGC 위치에 있을 때 RR CDU가 LGC 카운터를 거의 끊임없이 증감시킴(최대 6,400 pulses/s). 이것이 컴퓨터 시간의 약 15%(Eyles 다른 곳에서 약 13%)를 소모. 근본 원인: "800-Hz phasing problem"이 LM-1 발사장 시험에서 발견·문서화되었으나 수정되지 않음. Adler는 "레이더 스위치 구성 오류"라고 표현. [확인: Eyles, Adler]

### ⑥ 지상/교신·판단 — 1202 "Go" 결정

**알람 시각 비교 (같은 사건을 서로 다른 자료가 다르게 적는다)**

| 번호 | ALSJ 녹취 | Mission Report(ALSJ 주석) | Eyles | 시뮬레이터 | 2차 목록(검색 요약) |
|---|---|---|---|---|---|
| 1 | 102:38:26 "Program Alarm", 38:30 "It's a 1202" | - | - | 102:38:26 1202 | 102:38:22 |
| 2 | 102:39:14 "Same alarm" | 102:39:02 (첫 1202로 기록) | - | 102:39:02 1202 | 102:39:02 |
| 3 | 102:42:19 1201 (ALSJ 주석 102:42:24) | - | 1201 at 102:42:17 | 102:42:17 1201 | 102:42:18 |
| 4 | - | - | +24 s 1202 | 102:42:41 1202 | 102:42:43 |
| 5 | - | - | +16 s 1202 | 102:42:57 1202 | 102:42:58 |

- 총 5개(1202 ×4, 1201 ×1)이며, Eyles는 "three 1201 and 1202 alarms within 40 seconds"(3개 연속 40초 내)라 적는다. 첫 1202를 녹취(102:38:26)와 MR(102:39:02) 중 어느 쪽 시각으로 볼지가 다르고, 시뮬레이터는 이 둘을 **별개의 알람**으로 둔다(5개 구성). 둘이 같은 사건인지 다른 사건인지는 이 자료만으로 결론낼 수 없다. [부분]
- 총 횟수 "1202 4회 + 1201 1회"는 개발 계획 문서 및 제공 사실과 일치. [확인]

**판단 과정**
- Armstrong이 "It's a 1202"라고 코드를 보고, 지상(유도 담당 Bales)은 위협이 아니라고 판단, Duke "We're Go."(약 102:42:25) [부분: ALSJ 주석 요약]. MIT가 재시작 능력을 시험해 두었음. [확인: ALSJ 주석]
- 알람 이후 Houston은 'go'를 줌(제공된 사실). 이 문서의 자료에는 "알람 간격이 N초 이상이면 Go" 같은 구체적 결정 규칙이 없다. [미확인]
- 승무원은 알람 때마다 DSKY의 PROG 램프와 코드를 확인하고 감시를 이어갔다. 알람 후 V16N68 모니터가 재시작으로 사라졌다("terminated functions that had not been restart protected"). [확인: Eyles]
- Houston이 N68 정보를 구두로 대신 불러 주도록 요청된 것은 알람이 N68 표시와 겹쳐 보였기 때문(Aldrin "it appears to come up when we have a 16/68 up"). [부분]

### ⑥-2 Executive와 BAILOUT 해설

1. **Executive**: AGC의 작업 스케줄러(협력형 멀티태스킹). 각 작업은 12개 레지스터짜리 "core set"을 받고, 벡터를 쓰는 작업은 "VAC(Vector Accumulator) 영역"도 받는다. [확인: Eyles]
2. **자원 한도**: Eyles는 core set 8개(12레지스터씩), VAC 영역 5개라고 쓴다. Adler 에세이는 core set 7개(12워드씩, VAC 44워드), L099 EXECUTIVE.agc 주석은 "SEVEN SETS OF ELEVEN REGISTERS"(NO.CORES)와 "12 REGISTERS PER CORE SET"(JOBWAKE2)이다. Eyles의 8은 실행 중 작업의 세트 0을 포함한 수일 가능성이 있으나 확정하지 못했다. **자료끼리 충돌**. [부분/미확인]
3. **BAILOUT**: 새 작업을 스케줄하려는데 core set이 없으면 알람 **1202**("EXECUTIVE OVERFLOW-NO CORE SETS", 소스 `OCT 1202` "NO CORE SETS AVAILABLE"), VAC 영역이 없으면 **1201**("NO VAC AREAS")로 BAILOUT을 호출한다. 둘은 abort형 알람(1201, 1202, 1203, 1210, 1211). [확인: Eyles, L099, C055 알람 표]
4. **소프트웨어 재시작**: BAILOUT은 소프트웨어 재시작(소프트 리스타트)을 일으킨다. 재시작 보호(restart protection)는 적절한 지점에 "waypoint"를 등록해 두는 방식이며, 재시작 시 가장 최근의 SERVICER 작업 한 개만 재구성된다. 보호되지 않은 기능(V16N68 모니터 등)은 사라진다. 항법/유도/제어는 유지되었다. [확인: Eyles]
5. **원인 요약**: RR CDU의 가짜 카운터 증감 데이터가 컴퓨터 시간의 약 13~15%를 소모(Eyles) → 정규 작업이 끝나기 전에 새 작업이 계속 스케줄되어 core set/VAC이 고갈 → BAILOUT. [확인/부분: 인과의 세부 메커니즘 서술은 이 문서의 요약]
6. RESTART 램프/PROG 램프: Apollo 11 하강 중 DSKY 램프 세부는 자료에서 확인되지 않음. 시뮬레이터는 PROG만 켠다. [미확인]
7. **1203(Waitlist 초과)**은 Apollo 11 하강에서 보고되지 않음(C055 알람 표에 정의만). [부분]

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임: 102:33:05 PDI(P63) → 102:41:32 P64(2,255 m, 90 m/s) → 102:43:20 P66(131 m, 20 m/s) → 102:45:40 P68(고도 0, 속도 0).
- 알람: 102:38:26 1202, 102:39:02 1202, 102:42:17 1201, 102:42:41 1202, 102:42:57 1202 → PROG 램프 점등, `V05 N09E`로 코드(8진 01202/01201) 표시, `RSET`으로 램프 끔.
- 지원 표시: `V16 N63E`(|V|/고도변화율/고도[ft]), `V06 N68E`(사거리 nm/잔여시간 MM SS/R3은 항상 0 — DELTAH 미모델). N62/N64/N60 미지원. V57, V16N68의 알람 후 사라짐 동작, RESTART 램프 모두 미구현. V99 점멸 응답도 없다(M4 예정).
- PDI 직후 N63 값은 Eyles의 5559.7 ft/s, -2.2 ft/s, 49,971 ft와 다를 수 있다(시뮬레이터 값은 보간: 15 km 고도, 1700 m/s ≈ 5,577 ft/s).

### ⑧ 출처
Eyles (https://klabs.org/history/apollo_11_alarms/eyles_2004/eyles_2004.htm), ALSJ 착륙 페이지 (https://apollojournals.org/alsj/a11/a11.landing.html), Adler (https://www.nasa.gov/wp-content/uploads/static/history/alsj/a11/a11.1201-pa.html), L099 THE_LUNAR_LANDING.agc, LUNAR_LANDING_GUIDANCE_EQUATIONS.agc, EXECUTIVE.agc, PINBALL_NOUN_TABLES.agc, C055 ASSEMBLY_AND_OPERATION_INFORMATION.agc(알람 표).

### ⑨ 확인 수준 요약
시간순, 알람, Executive, 상위 원인 [확인]; V57 키 순서, P64/P66/P67 공식 제목, 로우 게이트, DSKY 램프 세부, 코어 세트 개수 [미확인/충돌]; 첫 1202 시각 [충돌].

---

## 11. 달 표면 (P68 착륙 확인, P57 정렬, 전원 절감, 이륙 준비)

### ① 개요
- MET 102:45:40 ~ 124:22:00. 달 표면 체류 약 21.6시간(ALSJ 소개문은 약 22시간). 우주선: Eagle. 첫 EVA는 시뮬레이터 키프레임 109:24:15. [부분]

### ② AGC의 역할
LGC: P68로 착륙 지점 확인, IMU를 중력/별로 재정렬(P57), 전원 절감을 위해 컴퓨터를 꺼 두었다가 이륙 준비 시 다시 켬. AGS는 PGNS에 맞춰 정렬. [부분]

### ③ 실행 프로그램

| 프로그램 | 이름 | 확인 |
|---|---|---|
| P68 | LANDING CONFIRMATION | [확인: L099 헤더] |
| P57 | "LUNAR SURFACE IMU ALIGNMENT PROGRAM". 표시 V06N89, V06N34, V06N22, V06N93; 주석 "VB33 -- PROCEED TO COARSE OR FINE", "VB33 -- PROCEED TO RE-ALIGN" | [확인: L099] |
| P12 | 상승(12절) | [확인] |

### ④ 승무원 조작

| MET | 조작/교신 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 102:48:10 | Aldrin "Noun 43" | N43 = PGNS가 계산한 착륙 지점 위도/경도/고도. 지상이 받아 적음 | ALSJ | [확인] |
| 102:50:59 | "(How would you) like AGS to PGNS align?" | AGS를 PGNS에 정렬 | ALSJ | [확인] |
| 103:08:47 | Aldrin: "I was trying to get time, (Verb)16 (Noun) 65, out, and somehow it proceeded on to the 06 22" | V16N65로 시간을 보려다가 프로그램이 06 22 표시(P57 단계)로 넘어감 | ALSJ | 인용 [확인], N65 정의(표본 AGC 시간) [부분] |
| 103:10:15 | Duke "We'd like you to recall P57 and run through the gravity align one more time." | P57 재수행 | ALSJ | [확인] |
| 103:15:09 | Duke "You are Go for the AGS to PGNS align, and then the lunar align" | | ALSJ | [확인] |
| 103:52:29, 103:53:02 | Duke가 V74 요청 | V74 의미(다운링크 요청으로 추정) | ALSJ | [미확인] |
| 103:59:41 | Aldrin "The DSKY's yours, and updata link to Data." | 지상 업링크 허용, UPDATA LINK 스위치 DATA | ALSJ | [확인] |
| 104:03:10 | Duke "It's your computer. We got the load in. You can start your P57." | | ALSJ | [확인] |
| 104:35:00 | Duke "You can start your powerdown now." | 전원 절감 시작 | ALSJ | [확인] |
| 122:45:52~46:02 | Aldrin "TIG portion of the ascent pad so I can load it in for a P57", Evans: P57은 TIG-50분까지 기다려라 | | ALSJ | [확인] |
| 123:18:43 | 태양이 AOT 6번 detent를 가림, 별 Sirius/Rigel 제안 | 별 정렬 | ALSJ | [확인] |
| 123:22:54, 123:23:36 | RR 위치 재설정 | | ALSJ | [확인] |
| 123:44:35 | Aldrin "I'm not sure the PGNS knows where gravity is." | 정렬 의심 | ALSJ | [확인] |
| 124:03:32 | RR 모드 스위치 LGC | | ALSJ | [확인] |
| (미상) | `V37E 57E`의 정확한 키 입력, 시계 갱신(V25N36E? P27? V70?), P57 구체 입력 | | | [미확인] |

### ⑤ 관련 하드웨어
IMU(중력 정렬 → 별 정렬), AOT(조준 망원경, detent 위치), AGS+DEDA, RR, 전원 계통(powerdown).

### ⑥ 지상/교신·판단
지상은 착륙 지점 위치(N43)를 받고, P57 재수행과 시계/상태 로드를 지시. 이륙 준비 시작점은 TIG-50분 기준 P57. [확인]

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임: 102:45:40 "착륙 — 고요의 바다"(P68, 0.67°N, 23.47°E), 109:24:15 "달 표면 활동(EVA)"(P00). 사이 구간(P57 등) 없음.
- 지원: `V16 N43E`/`V06 N43E`가 L099 정의로 구현되어 있다(위도 ×100, 경도 ×100, 고도 nm ×10). 착륙 지점 위도·경도·고도가 N43으로 나온다.
- 한계: 이 단계에서 승무원이 하는 일(P57, AGS 정렬)은 표현하지 않는다. N65는 미지원, N36 시계 확인만 가능.

### ⑧ 출처
ALSJ https://apollojournals.org/alsj/a11/a11.postland.html, https://apollojournals.org/alsj/a11/a11.launch.html; L099 P51-P53.agc.

### ⑨ 확인 수준 요약
교신·사건 [확인]; P57 키 시퀀스, 시계 갱신 방법, V74 의미 [미확인].

---

## 12. 상승 (P12)

### ① 개요
- MET 124:22:00 ~ 124:29:17. 우주선: 이륙한 Eagle 상승단. 하강단은 이륙대 역할을 하고 남는다. [확인]
- 계획 궤도 약 10 × 45 n.mi. 타원, 달성 47.2 × 9.5 n.mi.(124:31:07). 124:29:17 컷오프: 수평 5,337.3 ft/s, 수직 32.8 ft/s, 고도 60,666 ft. [확인: ALSJ]

### ② AGC의 역할
LGC가 **P12(POWERED ASCENT)**로 APS 엔진 유도: 수직 상승 → 피치오버 → 궤도 삽입. 이륙 시점에 DSKY가 공백이 된 뒤 Average-G 재시작 서술이 Eyles의 주석에 있음("At 35 seconds the display went blank, and at 30 seconds reappeared"). 컷오프 루틴은 V16N63을 쓰고 "TGO=VG/AT WITH RCS". [부분/확인]

### ③ 실행 프로그램
P12 "POWERED ASCENT"(ALSJ 해설의 호칭). L099 P12.agc 주석: "INITIALIZE ENGINE DATA. USED FOR P12 AND P71", "FLASH TIG", "CHECK THE STATUS OF THE IMU". P70/P71은 중단(abort) 프로그램, P42는 APS 연소. [확인/부분]

### ④ 승무원 조작

| MET | 조작/교신 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 124:04:51 | Evans "Our guidance recommendation is PGNS, and you're cleared for takeoff." | 유도 권고 | ALSJ | [확인] |
| 124:20:05 | Aldrin "Guidance steering in the AGS." | | ALSJ | [확인](설명은 [미확인]) |
| 124:20:58 | Master Arm On | | ALSJ | [확인] |
| 124:21:15 | Armstrong "At five seconds I'm gonna get Abort Stage and Engine Arm." | | ALSJ | [확인] |
| 124:21:33경 | P12 시작 | 선택 키 입력(V37E 12E) | ALSJ | 시각 [확인], 키 입력 [미확인] |
| 124:21:54 | "Abort Stage, Engine Arm, Ascent, Proceed" + 카운트 "9, 8, 7..." | 스위치/PRO | ALSJ | [확인] |
| 124:22:00 | **이륙** | | ALSJ | [확인] |
| 124:22:09 | "26, 36 feet per second up" | 수직 속도 | ALSJ | [확인] |
| 124:22:13/14 | "Pitchover" / "Pitching over. Balance couple, Off." | | ALSJ | [확인] |
| DEDA | Aldrin이 DEDA로 주소 500(추력축 +X 따른 획득 속도 VG)을 표시 | AGS 비교 | ALSJ | [확인] |
| 124:29:17 | "Shutdown" | | ALSJ | [확인] |
| 124:39:24 | AGS delta-H 15.5 | | ALSJ | [부분] |

### ⑤ 관련 하드웨어
APS, RCS, IMU/CDU, AGS+DEDA(백업), RR 모드 스위치 LGC, Abort Stage/Engine Arm 스위치, Master Arm.

### ⑥ 지상/교신·판단
유도는 PGNS 권고. 수직 상승 시간은 자료에서 확인하지 못함. [미확인]

### ⑦ 이 시뮬레이터에서의 표현
124:22:00 "상승단 이륙"(P12), 124:29:15 "상승단 궤도 진입"(P00, 17 km, 1690 m/s — MR/ALSJ의 컷오프 124:29:17, 60,666 ft ≈ 18.5 km와 근사). `V37E 12E` 수락. 하강/상승 표시 N63 계열은 LM에 맞고 상승 시 의미는 다르다(소스의 컷오프 루틴이 V16N63를 쓴다는 점은 일치).

### ⑧ 출처
ALSJ https://apollojournals.org/alsj/a11/a11.launch.html; L099 P12.agc, ASCENT_GUIDANCE.agc.

### ⑨ 확인 수준 요약
사건·시각 [확인]; P12 선택 키 [미확인]; 수직 상승 시간 [미확인].

---

## 13. 랑데부 (CSI / CDH / TPI / TPM, P20, P32–P35, 레이더, 제동)

### ① 개요
- MET 124:29 ~ 128:03. 능동 우주선: Eagle(LM). 수동: 컬럼비아(CSM). [확인]

### ② AGC의 역할
LM LGC는 랑데부 레이더(RR)로 CSM을 추적해(P20) 상대 상태를 갱신하고, 연소 4개(CSI, CDH, TPI, TPM)의 목표를 계산. CSM의 CMC는 독립적으로 해를 구하고 트래킹. [확인]

### ③ 실행 프로그램 (L099 제목 그대로)

| 프로그램 | 소스 제목 | 표시 | 확인 |
|---|---|---|---|
| P32/P72 | "COELLIPTIC SEQUENCE INITIATION (CSI) PROGRAMS (P32 AND P72)". P32 = 능동 우주선, P72 = 수동 | V06N11(TCSI), V06N55, V06N37(TTPI), V06N75 | [확인] |
| P33/P73 | "CONSTANT DELTA HEIGHT (CDH) PROGRAMS" | V06N13(TCDH), V06N75 | [확인] |
| P34/P74 | "TRANSFER PHASE INITITATION (TPI) PROGRAMS" (소스의 철자) | V06N37, V06N55, V06N58, V16N45 | [확인] |
| P35/P75 | "RENDEZVOUS MID-COURSE MANEUVER PROGRAMS" | V16N45, V06N59, V06N81 | [확인] |
| P20 | RENDEZVOUS NAVIGATION PROGRAM 20, R21(RR 지정), R22(RR 데이터 읽기) | | [확인] |
| P47/P76 | P47 "let's put P47 in"(127:43:02), P76은 CSI 패드 데이터(Collins 125:15:10 "CSI P76 PAD") | | 언급 [확인], 용도 [부분] |

CSM 쪽 Colossus의 P20/P72~P75 제목: [미확인](읽은 파일은 LM Luminary뿐).

### ④ 승무원 조작과 시각

| MET | 사건 | 확인 |
|---|---|---|
| 125:02:20 | Evans: "we missed your loading of the TPI TIG and P32" (TPI TIG와 P32 로딩 누락) | [확인] |
| 125:15:10 | Collins가 패드 요청: "Noun 84 and Noun 33" | 인용 [확인], N84 정의 [미확인] |
| **CSI** 125:19:34.7 | TIG, 51.5 ft/s, "We're burning" 125:19:46, 완료 125:21:36 (APS) | [확인] |
| **CDH** 126:17:45.58 | TIG(패드값), 완료 126:18:51 (126:13:58 Noun 81 읽음) | [확인] |
| **TPI** 127:03:52 | "We're burning" (CSM TPI TIG 추정 127:02:34.50 [126:33:19], 127:03:39 [126:48:07]) | [확인] |
| **TPM(중간수정)** 127:31:05 "Let's try this midcourse out" → 127:33:50 연소 | RCS | [확인] |
| **제동** 127:36:14 "8.2 miles, 40 feet per second" | 수동 제동(AGS 4제트 127:57:55) | [확인] |
| 127:43:02 | "let's put P47 in" | [확인] |
| 128:03:12 | 도킹: "Okay. We're all yours, Columbia." (AFJ 타임라인 128:03:00) | 시각 차 [부분] |

- 키 입력(예: `V37E 32E`, N11 로드, `V06 N37`)은 소스 표시 정의로는 확인되나 승무원의 키 순서는 녹취에 없음. [미확인]
- RR 모드: SLEW/AUTO/LGC. 이륙 준비에서 RR 스위치 LGC(124:03:32). [확인]

### ⑤ 관련 하드웨어
RR, AGS, APS(CSI), RCS(TPI/TPM/제동), 도킹 프로브.

### ⑥ 지상/교신·판단
CSM 계산과 LM 계산을 비교(AGS delta-H, Collins의 독립 TPI TIG). 지상은 CSI 패드(P76)를 낭독. [확인]

### ⑦ 이 시뮬레이터에서의 표현
키프레임 124:29:15 상승단 궤도 진입 → 128:03:00 "도킹"(P00). 사이 CSI/CDH/TPI 키프레임 없음. 랑데부 P20/P32~P35 문서화 목적의 단계는 M4 예정.

### ⑧ 출처
AFJ https://apollojournals.org/afj/ap11fj/19day6-rendezvs-dock.html; L099 P32-P35_P72-P75.agc, P34-35_P74-75.agc, P20-P25.agc.

### ⑨ 확인 수준 요약
LM 쪽 프로그램/시각 [확인]; CSM 쪽 프로그램 제목, 키 시퀀스 [미확인].

---

## 14. 도킹과 LM 투기

### ① 개요
- MET 128:03 ~ 약 130. 도킹 128:03:12(녹취)/128:03:00(AFJ 타임라인). Collins: "I went to Retract ... all hell broke loose"(128:04:29). 승무원과 표본이 CM으로 이동, LM 상승단 투기. [부분]

### ② AGC의 역할
도킹 중 CMC DAP 자세 유지, 상태벡터 갱신. [일반]

### ③~④ 프로그램/조작
이 구간의 DSKY 조작과 LM 투기 MET는 읽은 자료(AFJ Day 6 part 3 미읽음)에서 확인하지 못함. **[미확인]** 도킹 직후 "Retract all hell broke loose" 인용은 [확인]; 그 상황의 원인과 경과는 읽은 자료에서 확인하지 못했다 [미확인].

### ⑤ 관련 하드웨어 / ⑥ 지상·판단
도킹 프로브/드로그와 CSM RCS. 지상 판단 기록은 읽지 않았다. [미확인]

### ⑨ 정리
위 ③~④와 같다.

### ⑦ 이 시뮬레이터에서의 표현
키프레임 128:03:00 "도킹"(P00). LM 투기는 없음.

### ⑧ 출처
AFJ https://apollojournals.org/afj/ap11fj/19day6-rendezvs-dock.html

### ⑨ 확인 수준 요약
도킹 시각 [부분]; 투기와 DSKY 조작 [미확인].

---

## 15. 지구 귀환 분사 TEI (P30 / P37 / P40)

### ① 개요
- 점화 MET 135:23:42(AFJ 색인), 패드 TIG(N33) 135:23:41.56, 연소 약 2분 28~30초, 추진제 약 10,000 lb, ΔVT 3,283.6 fps(결과 "nominal 3,283.5 fps"). **달 뒤편**에서 수행. 우주선: CSM(LM 투기 후). [확인: AFJ 21day6-tei]
- 시뮬레이터 키프레임은 135:23:42 TEI(P40)와 135:26:12 TEI 종료(P00, 2.5 km/s).

### ② AGC의 역할
P30으로 TIG/ΔV 로드 후 P40이 SPS 연소를 유도(TVC DAP, 짐벌 트림). EMS ΔV 카운터는 G&N 고장 시의 백업 차단. 연소 후 V83으로 온보드 상태벡터와 예측값을 비교(Armstrong 사후 보고: 0.7 마일, 0.8 ft/s). [확인]

### ③ 실행 프로그램

| 프로그램 | 내용 | 확인 |
|---|---|---|
| P30 | "(EXTERNAL DELTA-V TARGETTING PROGRAM)": TIG, ΔV(LV) 입력 → 원지점, 근지점, ΔV 크기, MGA 표시. 코드 표시 V06N33, V06N81, V06N42 | [확인: C055] |
| P40 | 소스 "P40CSM"(SPS 추력). 표시 V06N40, V16N40, V99 점멸("SPS ENGINE-ON-ENABLE V99 FLASH"), V06N85/V16N85(P41) | [확인: C055] |
| P37 | 이 연소에는 사용되지 않은 것으로 서술(AFJ에 P37 언급 없음). 소스 알람 00612 "STATE VECTOR IN WRONG SPHERE OF INFLUENCE", 00613 "REENTRY ANGLE OUT OF LIMITS". 공식 제목 "Return to Earth"는 E2448 목차 | [부분] |

### ④ 승무원 조작

| MET | 조작/교신 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 134:01:16 | 패드 "TEI-30, SPS/G&N: 36691, minus 0.61, plus 0.66; 135:23:41.56." (TEI-31 비상 패드도 낭독) | N47 중량 36,691 lb, N48 피치/요 트림, N33 TIG | AFJ | [확인] |
| 패드 | N81 ΔV(LV): x +3,201.1 (+975.7 m/s), y +681.8 (+207.8), z -265.0 (-80.8). 복창 "Plus 3201.1, plus 0681.8, minus 0265.0; 181, 054, 014"(자세 181/054/014). ΔVT 3,283.6, ΔVC 3,262.8 fps를 EMS ΔV 카운터에 입력 | | AFJ | [확인] |
| 135:07:59 | Aldrin "All right. Verb 37, Enter. 40, Enter. Okay." | P40 선택(해설: "Verb 37 is for changing the program") | AFJ | [확인] |
| 135:18:40~19:02 | SPS 짐벌 트림을 **GPI 썸휠**로 수동 설정(피치 약 -0.6°, 요 약 +0.6°) | | AFJ | [확인] |
| 135:23:08 | **V06 N40** 표시: R1 점화까지/컷오프까지 시간, R2 획득해야 할 속도(3,283.6 시작), R3 누적 속도(0 시작) | | AFJ | [확인] |
| TIG-35 s | Collins "DSKY blanks. EMS is in Normal." | AVERAGEG 시작 | AFJ | [확인] |
| TIG-5 s | **V99 점멸**("enable the burn") → Collins가 **PRO**(카운트가 0이 되기 전) | | AFJ; C055 | [확인] |
| 135:23:42 | 점화, 연소 2분 30초경(Armstrong 135:35:33 "2 plus 30") | | AFJ | [확인] |
| 점화+2 s | Collins가 **EMS ΔV Normal 스위치 둘 다 OFF**, 컷오프는 자동 | EMS 백업 해제 | AFJ | [확인] |
| 연소 후 | "Delta-VGX 0.1, VGY 0.9, VGZ 0.1. Delta-VC minus 17.9" | N85 잔류 / EMS | AFJ | [확인] |
| 136:55:39 | Collins "Verb 49 maneuver to that attitude is in progress" | V49(R62 승무원 정의 기동) | AFJ; C055 | [확인] |
| 연소 후 | P52 정렬: 별 01 Alpheratz, 43 Deneb, 별 각도 차 0.03도, 토크 +0.469° X, -0.217° Y, +0.383° Z | | AFJ(2부) | [확인] |
| (미상) | P30 로드 키 입력(V25 N33/N81 등) | | | [미확인] |

### ⑤ 관련 하드웨어
SPS 엔진, TVC 짐벌 트림(GPI 썸휠), EMS ΔV, RCS 울리지(소스 "RCS ULLAGE"), IMU, 광학(TEI 자체에는 미사용).

### ⑥ 지상/교신·판단
TEI-30과 비상 TEI-31 두 패드를 준비(이름 붙임의 뜻은 자료에 없음). 지상은 연소 후 변화량이 nominal 3,283.5 fps임을 확인. [부분/확인]

### ⑦ 이 시뮬레이터에서의 표현
키프레임 135:23:42 "지구 귀환 연소(TEI)", PROG 40; `V37E 40E` 수락. V99 점멸 응답, N40/N81, V82/V83은 미지원.

### ⑧ 출처
AFJ https://apollojournals.org/afj/ap11fj/21day6-tei.html; C055 P40-P47.agc, P30,P37.agc.

### ⑨ 확인 수준 요약
시각, 패드, V37E40E, V06N40, V99/PRO [확인]; P30 키 입력 [미확인]; "Return to Earth" 제목 [일반].

---

## 16. 귀환 비행 (MCC-5)

### ① 개요
- MET 약 135:30 ~ 194:48. 우주선: CSM. 수동 열 제어(PTC) 계속(142:03, 151:11경 등). [확인]

### ② AGC의 역할
상태벡터 유지, 정렬(P52), RCS 소형 보정(MCC-5), 최종 재진입 PAD(상태벡터/목표/REFSMMAT) 업링크, 자세 모니터(V16N20 = ICDU 각도). [확인/부분]

### ③ 실행 프로그램
P00, P52, MCC-5는 RCS/G&N(어느 P번호인지 AFJ에 없음 — P41/P47 언급 없음). P23은 MR이 귀환 비행에서도 "midcourse navigation"이 수행되었다고 쓰지만 Colossus 쪽 AFJ 페이지에서는 못 찾음. P37 사용도 미확인. [부분/미확인]

### ④ 승무원 조작과 사건

| MET | 사건/조작 | 확인 |
|---|---|---|
| 144:00 | "It does not appear likely that we will perform Midcourse Correction number 5" → 147:30 수행 결정(TEI+15시간) | [확인] |
| 148:52:02 | MCC-5 패드: "RCS/G&N: 26025, pitch and yaw trim NA, TIG 150:29:54.53", ΔV "minus 0004.8, plus all balls, plus 0000.1", "no ullage of course, four-quad thrusting", ΔVT 4.8 fps, 연소 약 11초 | [확인] |
| 150:31:43 | Armstrong "Delta-VC is plus 0.2". 150:35 Apollo Control "burn duration, 10.5 seconds" (약 169,000 n.mi.) | [확인] |
| 151:25:50 | Collins: 이후 재진입 동안 "16 20 on the DSKY" (`V16 N20` ICDU 각도 감시) | [확인] |
| 151:46:29 | V40 N20(CDU 영점화; V40 = ZERO CDU's) | [확인] |
| 152:49:21 | Collins "Buzz thinks we should have a PTC program built into the computer." | [확인] |
| 169:28 | MCC-6(172시간 예정) 취소(Garriott 171:01:52 "We've cancelled Midcourse number 6.") | [확인] |
| 171:04:35 | Garriott: P52 요청 | [확인] |
| 176:44 | MCC-7 필요 시 0.2 ft/s 정도라고 언급 → 취소("will add one hour of rest time") | [확인] |
| 192:12경 | P52 | [부분] |
| 194:16:46 | Evans가 최종 재진입 PAD 갱신(상태벡터, 목표, 새 REFSMMAT): Noun 60 γ 6.48, 최대 g 6.3 | [확인] |

### ⑤ 관련 하드웨어
RCS 쿼드(MCC-5는 SPS가 아님), IMU, 육분의/스캐닝 망원경, EMS, CM RCS 가압 점검(194:16:36, 194:29:00).

### ⑥ 지상/교신·판단
MCC-5만 실행하고 MCC-6, MCC-7은 취소. 취소의 판단 근거 수치(예: 진입 각도 여유)는 읽은 자료에 없다. [미확인]

### ⑦ 이 시뮬레이터에서의 표현
키프레임 150:00:00 "지구로 돌아오는 비행"(P00), 175:00:00 "귀환 비행 후반". MCC, P52 없음. `V16 N36E`로 MET 확인.

### ⑧ 출처
AFJ 22day7-leave-lsi, 24day8-news-checks, 26day9-approach-earth, 27day9-entry (https://apollojournals.org/afj/ap11fj/ 아래).

### ⑨ 확인 수준 요약
MCC-5/6/7 [확인]; MCC-5 P번호와 DSKY 키, P23·P37 사용 [미확인].

---

## 17. 재진입·착수 (P61–P67, EMS)

### ① 개요
- MET 194:48 ~ 195:18:35. 우주선: CM 컬럼비아(SM 분리 후). [확인]
- 시각표(AFJ/MR): CM-SM 분리 예정 194:48:07, **SM 분리 194:49:12.7(MR)**; 진입 인터페이스(EI, 400,000 ft = 121.92 km) **195:03:06**(AFJ 27day9-entry) / PAD "Entry, 195 hours, 3 minutes, 7 seconds"(AFJ 26day9); 블랙아웃 시작 195:03:25; 0.05 g 195:03:36(PAD) / AFJ 타임라인 내 195:03:23~34 변동; P64→P65 약 195:05:19; 블랙아웃 종료 195:07:00; 드로그 낙하산 **195:12:06.9**; 착수 **195:18:35**(MR), PAO 비공식 195:18:21. [확인/부분]
- 이 시뮬레이터는 EI를 195:03:05로 둔다(자료: :06/:07, 시뮬레이터 값은 1~2초 근사). [부분]

### ② AGC의 역할
- 유도: 롤 자세(양력 벡터)로 착수점 도달. P63~P67의 예측형 진입 조종(PREDICTIVE ROLL SYSTEM). 2초마다 계산(WAITLIST). [확인: C055 CM_ENTRY_DIGITAL_AUTOPILOT.agc]
- 제어: 대기권 밖 자동조종(extra-atmospheric DAP)과 대기권 내 DAP. 몸체 속도율 0.1초마다. [확인]
- 감시: EMS(독립 가속도계, 범위/ΔV 카운터, 스크롤)를 승무원이 병행. 승무원은 EMS 범위 카운터와 ΔV를 사전에 로드. [확인: AFJ]

### ③ 실행 프로그램 (Comanche 055 P61-P67.agc의 기능 서술. 공식 제목 문구는 [미확인])

| 프로그램 | 기능 | 표시 | 확인 |
|---|---|---|---|
| P61 | "TO CALCULATE AND DISPLAY EMS INITIALIZATION DATA". V37로 호출. 종료 시 P62 | V06N61(착수 위도/경도, 헤드 업/다운), V06N60(GMAX, VPRED, γ_EI), V06N63(RTGO, VIO, TFE) | [확인] |
| P62 | "TO NOTIFY CREW WHEN GNC SYSTEM IS PREPARED FOR CM/SM SEPARATION" 및 진입 자세 정렬. "REQUEST SEPARATION". CM/DAP ON. 매 2초 .05G 짐벌각 계산 | V06N61, V06N22(진입 CDU 각도) | [확인] |
| P63 | "TO INITIALIZE THE ENTRY EQUATIONS". 자세 유지하며 0.05 g 감지 대기 | V06N64(항력, VI, 착수까지 범위) | [확인] |
| P64 | 0.05 g에서 진입 유도 시작(롤 자세, 일정 항력). V < 27000 fps(0.2 g에서)이면 P67, 상승 제어 해가 유효하면 P65 | V06N74(ROLLC, VI, D) | [확인] |
| P65 | 상승 제어(up control): "UPCONTRL: CONTROLS ROLL DURING THE SUPER-CIRCULAR PHASE". 항력이 Q7 미만이면 P66, 범위율 음수이고 V가 VL 근처이면 P67 | V16N69(소스 "ROLLC, DL, VL") | [확인] |
| P66 | "keeps the CM attitude trimmed to the relative velocity vector"(탄도 단계) | V06N22 | [확인] |
| P67 | "TO TERMINATE STEERING WHEN THE CM VELOCITY WRT EARTH = 1000 FT/SEC". 진입 DAP 끔. 마지막 롤각 유지(P67.1) | V06N66, V16N67(RTOGO, 위도, 경도) | [확인] |

- Apollo 11에서는 **P66이 호출되지 않았다**: "In a conventional entry, P64 is followed by P67", "P66 will not be invoked and instead, P65 will lead directly to P67". 또한 승무원은 P67 진입 시 풀 양력(헤드 다운) 자세가 아닐 수 있다고 통보받음. [확인: AFJ 26day9]
- **스킵 아웃**: 의도적으로 VCIRC를 지난 뒤 약간의 상승 스킵을 하도록 설계. P64의 목적은 순환궤도 속도 VCIRC 미만으로 감속하는 것, P67의 목적은 CM을 착륙점 거의 위로 가져가는 것(롤 최대 15도로 횡거리 보정). 공식 "skip-out 보호" 장치 이름은 [미확인]. [확인/부분]

### ④ 승무원 조작과 사건

| MET | 사건/조작 | 의미 | 출처 | 확인 |
|---|---|---|---|---|
| 194:16:46 | 지상 최종 진입 PAD 갱신 | 상태벡터, 목표, REFSMMAT | AFJ | [확인] |
| (사전) | EMS에 범위-도달(range to go)과 속도 로드, 수평선 점검(pitch 298°; 194:46:06) | | AFJ | [확인] |
| (사전) | P61/P62 호출(`V37E 61E`, `V37E 62E`)과 N61/N60/N63 표시 확인 | 표시 순서는 소스 | | 키 입력은 [미확인]; 일반 `V37E xxE` 문법 [확인](AFJ 다른 곳) |
| 194:49:12.7 | **CM-SM SEP 스위치**를 올림(패널 2, DSKY 바로 아래). SM 분리 | 스위치 | AFJ 27day9 | [확인] |
| 195:03:06 | EI(400,000 ft). 진입 PAD: 속도 36,194 fps(N60), γ 6.49°, 최대 6.4 g, 0.05 g까지 EI에서 0:28, 0.05 g에서 관성 속도 36,275 fps, 0.05 g까지 range 1,404.5 n.mi.; 짐벌 트림 롤 0 피치 152 요 1; 착수점(N61) 13.32°N 169.17°W; 보어사이트 별 없음 | | AFJ 26day9 | [확인] |
| 약 195:03:36 | 0.05 g. EMS가 속도/범위 카운터 감소 시작. CMC는 **P63에서 자세 유지, 0.05 g 대기** | | AFJ | [확인] |
| 195:03~04 | P64: 유도 시작, EMS와 병행 모니터링. Armstrong "We gave spacecraft control over to the computer after we passed all our pitch attitude cross-checks" | | AFJ | [확인] |
| 약 195:05:19 | P64 → P65 | | AFJ | [확인] |
| 195:07:00 | 블랙아웃 종료 | | AFJ | [확인] |
| 피크 g | 첫 피크 6.5, 둘째 6.0 g(예상 6~7 g). Armstrong: 컴퓨터가 "did not fly the EMS the same way I would have flown the EMS" | | AFJ | [확인] |
| 195:11:18 | Houston: "Standing by for your DSKY reading." P67 후 DSKY는 착수점까지의 범위와 현재 위도/경도(N67) 표시 | | AFJ | [확인] |
| 195:12:06.9 | 드로그 낙하산(예측보다 약 1초 빠름), 주 낙하산 8.5 m/s | 자동. 승무원 수동 조작은 자료에 없음 | AFJ | 시각 [확인], 승무원 스위치 [미확인] |
| 195:18:35 | 착수 | | MR | [확인] |

### ⑤ 관련 하드웨어
CM RCS(진입 DAP), IMU/CDU/FDAI, EMS(범위/ΔV 카운터, 스크롤), 열 차폐, 낙하산, CM-SM SEP 스위치. 블랙아웃 중 통신 두절.

### ⑥ 지상/교신·판단
진입 직전 갱신 PAD(최대 g 6.3, γ 6.48°)로 회랑 확인. MCC-7 취소 판단은 16절. [확인]

### ⑦ 이 시뮬레이터에서의 표현
- 키프레임: 195:03:05 "대기권 재진입"(P63, 122 km, 11,000 m/s) → 195:04:30 "최대 감속 구간(P64)" → 195:10:30 "최종 강하(P67)"(8 km, 150 m/s) → 195:18:35 "착수"(P00).
- **주의**: 시뮬레이터 N63은 LM 정의(|V|/고도변화율/계산 고도)를 쓴다. 이 구간의 실제 CMC N63은 RTGO/VIO/TFE(ASSEMBLY_AND_OPERATION_INFORMATION). 표시 형식은 속도 ×10을 쓰므로 속도 3,048 m/s를 넘으면 R1이 +99999로 포화되며, 고도 ft도 99,999 ft(30.5 km) 초과 시 포화된다. 재진입 구간 N63은 따라서 참고용이다.
- P65와 P66/P61/P62, N60/N61/N64/N67/N69/N74 미지원.

### ⑧ 출처
AFJ 26day9-approach-earth, 27day9-entry, 25day8-reentry-stowage; C055 P61-P67.agc, REENTRY_CONTROL.agc, CM_ENTRY_DIGITAL_AUTOPILOT.agc, ASSEMBLY_AND_OPERATION_INFORMATION.agc; MR(착수 시각).

### ⑨ 확인 수준 요약
사건·PAD·P63~P67 거동 [확인]; 공식 제목 문구, N66/N74 정의, 낙하산 수동 조작, P61/P62 승무원 키 입력 [미확인].

---

## 18. 요약 타임라인 (MET, 사건, AGC 프로그램, 핵심 DSKY 조작)

| MET | 사건 | AGC 프로그램 | 핵심 DSKY 조작/표시 | 확인 |
|---|---|---|---|---|
| T-2:12 | 발사 전 G&N 교신("Verb 34") | P00~P02 | V34 용도 미확인 | [부분] |
| 000:00:00 | 발사 | P11 | V06 N62 자동(2초 주기). 백업 `V75E` | [확인] |
| 000:11:49 | 지구 저궤도 진입 | P00 | `V37E 00E`(일반) | [부분] |
| 000:40~54 | 첫 P52(옵션 3, 별 30·37, 확인별 34) | P52 | N71, N05, N93, `PRO`(토크), `V37E` | [확인] |
| 001:43:57 | 상태벡터 업링크 | P00(+ACCEPT) | 승무원은 P00 | [확인] |
| 002:12:15 | TLI 준비 | (P00) | `V48E`, `V83E`, ORDEAL | [확인] |
| 002:44:16 | TLI 점화 | P15 미확인 | N62 감시 | [부분] |
| 002:50:06 | TLI 종료(35,570 ft/s) | | N62 값 지상 보고 | [확인] |
| 003:24:03 | 도킹 | DAP | V48, V62/V64(미확인) | [부분] |
| 004:40:01.7 | S-IVB 회피 SPS 연소 | P30→P40 | | [부분] |
| 006:09~008:35 | P23 항법 조준 | P23 | N49 확인/거부 | [확인] |
| 009:36 | P52 옵션 1(PTC 준비) | P52 | | [확인] |
| 010:58 | PTC 시작 | | 승무원 자세 설정 | [확인] |
| 026:44:57.92 | MCC-2 SPS 20.9 ft/s | P30→P40 | N33, N47, N81 패드; V99/PRO | [부분] |
| 069:18 | MCC-4 불필요 | | | [확인] |
| 075:49:50 | LOI-1 (357.5 s, 2,917 ft/s) | P30→P40 | V99 → `PRO`; 연소 후 `V82E` | [확인] |
| 080:11:36 | LOI-2 (16.9 s, 159 ft/s) | P40 | `V37E 40E`; V99/PRO; `V82E`, `V83E` | [확인] |
| 082:43 | P22 랜드마크 A1 | P22 | MARK ×5, N89, `V34E` | [확인] |
| 100:12 | LM 분리 | P52 | | [부분] |
| 101:36:14 | DOI (LM DPS) | P40 | | [부분] |
| 102:33:05 | PDI | P63 | `V37E 63E`, V06N62, `PRO`, N63 | [확인] |
| 102:36:55 | rate switch 5→25 deg/s | | 스위치 | [확인] |
| 102:38:26 | 첫 1202 | P63 | V05 N09, V16N68, V57 | [부분] |
| 102:41:32 | 하이 게이트 | P64 | N64, LPD | [확인] |
| 102:42:17 | 1201, +24 s 1202, +16 s 1202 | P64 | | [확인] |
| 102:43:08 | AUTO → ATT HOLD | P64 | 스위치 | [확인] |
| 102:43:20 | P66, ROD 스위치 | P66 | ROD(스위치) | [확인] |
| 102:45:40 | 착륙 | P68 | `Engine Stop`, DEDA 413 | [확인] |
| 102:48:10 | 착륙점 보고 | P68 | N43 | [확인] |
| 103:08~104:03 | P57 정렬(재수행) | P57 | V16N65(Aldrin) → 06 22 | [확인] |
| 104:35:00 | 전원 절감 | | | [확인] |
| 124:22:00 | 상승단 이륙 | P12 | Abort Stage, Engine Arm, `PRO` | [확인] |
| 124:29:17 | 컷오프 | P12 | | [확인] |
| 125:19:34.7 | CSI | P32 | | [확인] |
| 126:17:45.58 | CDH | P33 | | [확인] |
| 127:03:52 | TPI | P34 | | [확인] |
| 127:33:50 | TPM | P35 | | [확인] |
| 128:03:12 | 도킹 | | | [부분] |
| 135:23:41.56 | TEI (패드 TIG) | P30→P40 | `V37E 40E`, N40, V99 → `PRO` | [확인] |
| 150:29:54.53 | MCC-5 (RCS 4.8 fps) | | | [확인] |
| 194:49:12.7 | SM 분리 | P62 | CM-SM SEP 스위치 | [확인] |
| 195:03:06 | EI | P63 | | [확인] |
| 195:04~05 | 0.05 g → P64 → P65 | P64/P65 | | [확인] |
| 195:12:06.9 | 드로그 낙하산 | P67 | | [확인] |
| 195:18:35 | 착수 | | | [확인] |

---

## 19. 시뮬레이터에서 따라 해 보기

공통 준비: 앱을 실행하고(`dotnet run --project src/Agc.Wpf`) 오른쪽 미션 뷰의 **단계 이동** 상자에서 단계를 고른다. 재생(▶)은 정지 상태로 시작한다. 지원 명령은 V05/V06/V16, V21~V25, V33, V34, V35, V37과 N09/N36/N43/N63/N68이다(`doc/02_Simulator_User_Manual.md`). V37로 고를 수 있는 프로그램은 타임라인에 나오는 번호(00, 11, 15, 40, 63, 64, 66, 67, 68, 12)다.

### 연습 1. DSKY 점검 — `V35E`, `KEY REL` (실제: 발사 전 DSKY 램프 점검)
1. 단계 이동에서 "발사".
2. `V` `3` `5` `ENTR`.
3. **예상 표시**: 모든 램프 점등, 모든 창 88, V/N 깜박임. 약 5초 후 램프 소거, 88은 남음.
4. `KEY REL`(`K`)로 88을 지운다.
- 실제 연결: V35는 램프 시험 동사(C055 동사 표 "TEST LIGHTS"). 이 시험을 Apollo 11 발사 전에 언제 했는지는 [미확인].

### 연습 2. P11과 AGC 시계 — `V16 N36E`, `V25 N36E` (실제: 1절 P11이 리프트오프에 시계를 0으로)
1. 단계 이동 "발사"(MET 0). `V16 N36E`.
2. **예상**: R1 `+00000`, R2 `+00000`, R3 `+00000`(시·분·초×100). ▶를 누르면 초가 올라간다(R3 증가, 60× 배속 기본).
3. `V25 N36E` → V/N 깜박임 → `+00102 ENTR` `+00033 ENTR` `+00500 ENTR`(R1 시, R2 분, R3 초×100)로 시계를 102:33:05로 설정. 깜박임이 멈추고 설정값부터 흐른다. 포기하려면 `V34E`.
- 실제 연결: 실제 CMC는 리프트오프 신호로 시계를 0으로 맞춘다(C055 P11). 실제 시계 갱신 방법(V25N36E 사용 여부)은 [미확인]. 시뮬레이터의 N36 로드는 학습용.

### 연습 3. 프로그램 선택 — `V37E` (실제: 단계마다 승무원이 P번호를 고름)
1. 단계 이동 "TLI 점화": PROG 창 **15**.
2. `V37E 00E`: PROG 00. 다음 단계에서 15가 아닌 그 단계의 프로그램으로 대체됨.
3. `V37E 15E`: 수락. `V37E 52E`: **OPR ERR**(타임라인에 없음) → `RSET`.
4. 단계 이동 "달 궤도 진입 연소(LOI-1)": PROG 40. 실제도 `V37E 40E`(AFJ 079:50:35 LOI-2, 135:07:59 TEI).
- 한계: TLI의 P15는 실제 Apollo 11에서 [미확인](4절).

### 연습 4. 동력 하강 감시 — `V16 N63E`, `V06 N68E` (실제: 10절 PDI, N63 표시)
1. 단계 이동 "동력 하강 시작(PDI)"(MET 102:33:05, PROG **63**).
2. `V16 N63E`: **예상**(첫 순간 근사) R1 약 `+55774`(= 5577.4 ft/s), R2 음수(하강율), R3 약 `+49213`(ft). 실제 PDI 직후 값은 5559.7 ft/s, -2.2 ft/s, 49,971 ft. 시뮬레이터는 키프레임 보간.
3. `V06 N68E`: R1 사거리(약 240~270 nm), R2 `+08 27`(다음 단계 P64까지의 시간), R3 `+00000`(DELTAH 미모델).
4. ▶(60×): 고도 감소, 고도변화율 음수. 102:41:32에 PROG **64**, 102:43:20에 **66**, 102:45:40에 **68**.

### 연습 5. 1202/1201 알람 — `V05 N09E`, `RSET` (실제: 10절 알람)
1. 연습 4에서 ▶ 재생. 102:38:26에 **PROG 램프** 점등.
2. `V05 N09E`: R1이 8진 코드(최근 알람) `01202`.
3. `R`(RSET)로 램프 소거. 102:39:02(1202), 102:42:17(1201), 102:42:41(1202), 102:42:57(1202)에서 반복.
4. 1201 이후 `V05 N09E`: 최근 순서로 최대 3개(R1 최근, R2 이전, R3 그 이전) 표시.
- 실제 연결: 알람 코드 읽기는 V05 N09(L099 P40의 "V05N09 POSSIBLE ALARMS"). 시뮬레이터에는 재시작에 따른 V16N68 소멸, RESTART 램프, 점멸 표시는 없다.

### 연습 6. 착륙점 확인과 재진입 N63의 한계 — `V16 N43E`
1. 단계 이동 "착륙 — 고요의 바다"(PROG **68**). `V16 N43E`: **예상** R1 `+00067`(0.67°N ×100), R2 `+02347`(23.47°E), R3 `+00000`(고도 nm).
2. 실제 연결: 착륙 후 Aldrin의 "Noun 43"(102:48:10)과 같은 개념(PGNS가 계산한 위치).
3. 단계 이동 "대기권 재진입"(PROG 63). `V16 N63E`: R1이 `+99999`로 포화(속도 11,000 m/s). 이 구간의 실제 CMC N63은 RTGO/VIO/TFE이므로 시뮬레이터 N63은 참고용이다(17절).

---

## 20. 자료 충돌과 미확인 항목 정리

### 20.1 자료 충돌

| 항목 | 값 A | 값 B | 비고 |
|---|---|---|---|
| 첫 1202 | ALSJ 녹취 102:38:26 | MR 102:39:02 | 시뮬레이터는 둘을 별개 알람으로 둔다 |
| PDI 점화 | Eyles 102:33:05 | ALSJ 녹취 "Ignition" 102:33:11, ALSJ 주석 실제 PDI 102:33:07 | |
| 1201 | Eyles 102:42:17 | ALSJ 녹취 102:42:19(주석 102:42:24) | |
| Executive core set 수 | Eyles 8개 | Adler 7개, L099 주석 7개(11레지스터)/12레지스터 | 미해결 |
| TLI 점화 | MR 2:44:16 | AFJ 녹취 002:44:18 | |
| TLI 종료 | 시뮬레이터 2:50:03 | AFJ 002:50:06 | |
| LOI-1 점화 | MR 75:49:50.37 | PAD 75:49:49.65 / AFJ 075:49:51 | |
| LOI-1 밸브 뱅크 | AFJ "A 자동, B 수동" | MR "A 대신 B 밸브로 시작" | |
| 분리(undocking) | MR 표 100:12:00 | AFJ 100:12:03 / 100:18:01 | |
| 도킹(LM-CSM) | 녹취 128:03:12 | AFJ 타임라인 128:03:00 | |
| EI | AFJ 195:03:06 | PAD 195:03:07 / 시뮬레이터 195:03:05 | |
| 0.05 g | PAD 195:03:36 | AFJ 타임라인 195:03:23~34 | |
| MCC-2 연소 시간 | AFJ 2.91 s | MR 3.13 s (표 OCR) | |
| 첫 P52 X 토크 | 복창 00016 | MR 0.018 | |
| N68 정의 | ALSJ 주석 "사거리, 피치오버까지 시간, 속도" | L099 표 "사거리, 제동 잔여시간, DELTAH" | L099 채택 |
| PROG 램프 의미 | CIS "승무원 입력 대기" | NASSP "프로그램 알람" | 시뮬레이터는 알람 쪽 |

### 20.2 [미확인] 항목 목록 요약
- P01~P03 승무원 키 입력, 발사 전 V34의 용도, 발사 중 CMC의 중단 판단 기능(1~2절)
- Apollo 11에서 P15 사용 여부, TLI 감시 키 입력(4절)
- V62/V64 의미, 도킹 중 PRO 의미(5절)
- MCC-2 P30 키 입력, V59 정의(6절)
- P22 키 시퀀스(8절)
- DOI 키 시퀀스와 점화 MET, V76, 134 17/13 콜(9절)
- V57 키 순서, 점멸 표시의 V/N, 로우 게이트, DSKY 램프 세부, 코어 세트 수, P64~P67 공식 제목, 알람 Go 규칙(10절)
- P57 키 입력, 시계 갱신 방법, V74(11절)
- P12 선택 키, 수직 상승 시간(12절)
- CSM 쪽 랑데부 프로그램 제목, 랑데부 키 순서, N84(13절)
- LM 투기, 도킹 후 DSKY 조작(14절)
- TEI P30 키 입력(15절)
- 귀환 비행 MCC-5 P번호와 키(16절)
- P61/P62 키 입력, P61~P67 공식 제목, N66/N74, 낙하산 수동 조작(17절)

### 20.3 참고: DSKY 램프와 알람 코드(Comanche 055 알람 표, 이 문서에서 쓴 것)

| 코드 | 의미 | 확인 |
|---|---|---|
| 00210 | IMU NOT OPERATING | [확인] |
| 00220 | IMU NOT ALIGNED - NO REFSMMAT | [확인] |
| 00401 | DESIRED GIMBAL ANGLES YIELD GIMBAL LOCK | [확인] |
| 00405 | TWO STARS NOT AVAILABLE (P52, P54) | [확인] |
| 00612 | STATE VECTOR IN WRONG SPHERE OF INFLUENCE (P37) | [확인] |
| 00613 | REENTRY ANGLE OUT OF LIMITS (P37) | [확인] |
| 01201 | EXECUTIVE OVERFLOW - NO VAC AREAS | [확인] |
| 01202 | EXECUTIVE OVERFLOW - NO CORE SETS | [확인] |
| 01203 | WAITLIST OVERFLOW - TOO MANY TASKS | [확인] |

램프(CIS): UPLINK ACTY("지상 데이터 수신 중"), NO ATT, STBY, KEY REL("컴퓨터가 프로그램을 끝내려면 DSKY 제어가 필요"), OPR ERR("키보드 오류 검출"), TEMP, GIMBAL LOCK("가운데 짐벌각 70도 초과"), RESTART("재시작 프로그램 중"), TRACKER, COMP ACTY, PROG(해석이 갈림, 위 표). [확인/부분]
