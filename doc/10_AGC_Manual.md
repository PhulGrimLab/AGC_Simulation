# Apollo Guidance Computer (AGC) 매뉴얼

하드웨어, 소프트웨어 구조, DSKY 조작법을 정리한 문서이다. 시뮬레이터 조작법은 `02_Simulator_User_Manual.md`, 우주선/운용은 `11`, 미션 단계별 AGC 사용은 `12`를 본다.

## 0. 읽는 법: 확인 수준 태그와 출처 약호

| 태그 | 뜻 |
|---|---|
| [확인] | 이 문서 작성에 사용한 출처(조사 노트 포함)에서 직접 읽어 확인한 내용 |
| [부분] | 일부만 확인했거나, 2차 자료(Wikipedia/NASSP/시뮬레이터 위키 등)에서만 확인했거나, 출처끼리 표현이 다른 내용 |
| [미확인] | 어떤 출처에서도 확인하지 못함. 추정으로 채우지 않고 빈칸으로 둔다 |

- 출처가 서로 충돌하면 한쪽을 고르지 않고 **나란히** 적는다.
- 키 입력 순서는 출처에 있는 것만 적는다. 출처가 일반(generic) 절차인 경우 해당 소프트웨어 버전을 표시한다.
- 중요한 버전 주의: `E-2448`(S7)은 **Colossus 3 / Luminary 1E**(1972년) 문서이며 Apollo 11(Colossus 2A, Luminary 1A)의 것이 아니다. 키 순서와 코드값은 Apollo 11과 다를 수 있다.
- 출처 약호는 10장에 URL과 함께 정리했다. 본문에서는 `S1`, `S2`처럼 쓴다.
- "조사 노트"는 이 프로젝트 작업 중 만든 임시 조사 파일(research_A/B/C)이며, 각 항목이 인용한 원 URL을 `S` 약호로 옮겼다.

---

## 1. 개요와 역사

### 1.1 AGC란

Apollo Guidance Computer(AGC)는 아폴로 사령선(Command Module, CM)과 달착륙선(Lunar Module, LM)에 각각 탑재된 디지털 유도/항법/제어 컴퓨터이다. [확인; S1]

- 사령선 쪽 소프트웨어는 **Colossus**, 달착륙선 쪽은 **Luminary**이다. Apollo 11은 Colossus 2A(소스 이름 Comanche 055)와 Luminary 1A(빌드 099)를 썼다. [확인; S1, S16, S17] (사령선 쪽 Apollo 11 실제 비행 버전 이름 "Colossus 249"는 읽은 출처에서 확인하지 못했다 [미확인]; S16 헤더는 "Colossus 2A, AKA Comanche 055"라고 적는다.)
- 사령선에는 DSKY가 2개(주 계기판 + 하부 장비 베이), 달착륙선에는 1개 있다. [확인; 인계 검증 목록, S1]
- 일반적인 소프트웨어 규모는 "약 40개 프로그램과 30개의 동시 루틴"이라고 기술된다. [확인; S3]

### 1.2 왜 필요했나 (기록으로 확인되는 범위)

- "왜 탑재 컴퓨터가 필요했는가"에 대한 설계 근거를 직접 서술한 문장은 이번 조사에서 인용하지 못했다. [미확인]
- 운용 기록으로 확인되는 사실: Apollo 11의 TEI(지구귀환분사)는 달 뒷면에서 수행되었고 사령선 컴퓨터가 P40 프로그램으로 SPS 분사를 유도했다. [확인; S12 TEI 페이지, S16 P40]
- 달 착륙 하강 중 LM의 컴퓨터가 하강 유도, 스로틀 명령, 자세 제어, 레이더 데이터 처리를 맡았다. [확인; S2]

### 1.3 개발 주체 (MIT / Draper)

- 소프트웨어는 MIT에서 개발되었고 Executive는 J. Halcombe Laning이 설계했다. [확인; 인계 검증 목록, S1]
- "MIT Instrumentation Laboratory"가 후일 "Draper Laboratory"가 되었다는 연혁과 제작사 분담은 이번에 읽은 출처에서 직접 확인하지 못했다. [미확인]

### 1.4 Block I과 Block II

| 항목 | Block I | Block II | 확인 |
|---|---|---|---|
| 소거성 코어(erasable) | 1K 워드 | 2,048 워드 | [확인; S1] |
| 고정 메모리(fixed) | 12K 워드에서 24K 워드로 | 36,864 워드 | [확인; S1] |
| 논리소자 | 약 4,100개 IC, 1-NOR 게이트 | 약 2,800개 IC, 이중 3입력 NOR (Fairchild RTL) | [확인; 인계 검증 목록, S1] |
| 명령어 수 | 11 | 34 (입출력 채널 사용) | [확인; 인계 검증 목록, S1] |

- Block I의 고정 메모리 증설 경위("12K에서 24K로")의 시점은 읽은 출처에서 구분하지 못했다. [부분]
- 이 문서의 하드웨어 서술은 별도 표기가 없으면 **Block II**(Apollo 11 비행 하드웨어)를 기준으로 한다. 이 "Apollo 11이 Block II를 썼다"는 문장은 이번에 직접 확인한 출처가 없다. [미확인]

### 1.5 제작 의의

- 집적회로를 대량 사용한 초기 탑재 컴퓨터라는 설명은 흔하지만 "최초"라는 주장은 읽은 출처에서 직접 확인하지 못했다. [부분]
- 워드 길이 16비트, 메모리 약 38K 워드 수준의 작은 기계로 다중 작업(Executive), 타이머 작업(Waitlist), 가상 기계(Interpreter)를 구현했다. [확인; S1, S4]

---

## 2. 하드웨어 (Block II 기준)

### 2.1 요약 표

| 항목 | 값 | 확인 |
|---|---|---|
| 워드 길이 | 16비트 = 데이터 15비트 + 홀수 패리티 1비트 | [확인; S1, S4] |
| 수 표현 | 1의 보수(ones' complement). CPU 내부는 14 데이터비트 + 오버플로 + 부호 | [확인; 인계 검증 목록, S1] |
| 클록 | 2.048 MHz 수정 발진기에서 4상(4-phase) 1.024 MHz 내부 클록 | [확인; 인계 검증 목록, S1] |
| 메모리 사이클 | 12 타이밍 펄스 = 11.72 µs | [확인; S1] (S4는 11.7 µs, 덧셈 1회 23.4 µs) |
| 소거성 메모리(RAM 상당) | 코어 2,048 워드 | [확인; S1, S4] |
| 고정 메모리(ROM 상당) | 코어 로프 36,864 워드 | [확인; S1, S4] |
| 논리 | 약 2,800 IC, 이중 3입력 NOR (Fairchild RTL) | [확인; 인계 검증 목록, S1] |
| 명령어 | 34개. 3비트 opcode + 12비트 주소 | [확인; 인계 검증 목록, S1] |
| 중앙 레지스터 | A, Z, Q, L | [확인; 인계 검증 목록] |
| I/O 레지스터 | 입력 4개 + 출력 5개(각 16비트) | [확인; 인계 검증 목록] |
| 크기/무게 | 약 24 x 12.5 x 6.5 in, 약 70 lb (32 kg) | [부분; S1 정보상자만, 1차 자료 미확인] |
| 소비 전력 | 약 55 W | [부분; S1 정보상자만] |

### 2.2 워드와 산술

- 16비트 워드의 15비트가 데이터, 1비트가 패리티이다. [확인; S1, S4] 패리티는 홀수 패리티이다. [확인; 인계 검증 목록]
- 음수는 1의 보수 표현이다. [확인; 인계 검증 목록] CPU 내부 레지스터는 14 데이터비트에 오버플로 비트와 부호 비트를 더한 형태이다. [확인; 인계 검증 목록]
- 단정밀도 하나로 담는 정수 범위, 배정밀도(2워드) 조합 방식은 읽은 출처에 없다. [미확인]

### 2.3 CPU와 레지스터

- 중앙 레지스터는 **A**(누산기), **Z**(프로그램 카운터), **Q**(복귀 주소), **L**(하위 누산기)이다. [확인; 인계 검증 목록]
- 각 레지스터의 세부 역할 설명(Z=프로그램 카운터, Q=복귀 주소 등)은 이름 외에는 이번에 읽은 출처에서 직접 확인하지 못했다. [부분]
- 그 외 특수 레지스터(메모리 뱅크 레지스터 EB/FB/BB, 시프트 레지스터 등)는 이번 조사에서 출처를 읽지 못했다. [미확인]
- 명령어 형식은 3비트 opcode + 12비트 주소이다. [확인; 인계 검증 목록] 12비트 주소로 메모리를 다 가리킬 수 없는 부분의 뱅킹 방식은 [미확인].

### 2.4 메모리

```
 소거성(erasable) 코어   2,048 워드    읽기/쓰기, 전원이 꺼지면 중요한 값 보존 여부 [미확인]
 고정(fixed) 코어 로프   36,864 워드   읽기 전용, 배선으로 프로그램을 기록
```

- 두 메모리 모두 **자기 코어** 방식이다. 고정 메모리는 core rope이다. [확인; S1, S4]
- 코어 로프에 소프트웨어를 "짜 넣는" 제조 방식(배선이 코어를 통과/우회)에 대한 서술은 이번에 읽은 출처에 없다. [미확인]
- 메모리 사이클은 12 타이밍 펄스(11.72 µs)이고, 이로부터 덧셈 1회가 약 23.4 µs라는 설명이 S4에 있다. [확인; S1, S4]

### 2.5 입출력(I/O)

- 입력 레지스터 4개와 출력 레지스터 5개(각 16비트), Block II는 입출력 채널을 쓴다. [확인; 인계 검증 목록]
- DSKY는 키 입력과 표시 출력을 이 입출력 구조로 주고받는다. 키 한 번마다 **5비트 코드**가 생성되며 PROCEED 키만 코드가 없다. [확인; S4] (PRO 키가 코드 없이 별도 입력으로 처리됨. 구체 채널 번호는 [미확인])
- 디스플레이는 릴레이로 구동되는 녹색 전계발광 7-세그먼트이다. [확인; 인계 검증 목록]
- 지상 업링크(UPLINK ACTY 램프), 다운링크(알람 01105 "DOWNLINK TOO FAST", 01106 "UPLINK TOO FAST")도 컴퓨터의 입출력 기능이다. [확인; S5 알람표] 데이터 형식과 속도는 [미확인].

### 2.6 인터럽트

- 벡터형 인터럽트: **Dsrupt, Erupt, Keyrupt1, Keyrupt2, T3Rupt, Uprupt**. [확인; 인계 검증 목록]
- 각 인터럽트의 우선순위, 벡터 주소는 읽은 출처에 없다. [미확인]
- Keyrupt는 키 입력, Uprupt는 업링크와 관련된다고 이름에서 짐작되나, 직접 확인하지 못했다. [미확인] T3Rupt와 Waitlist의 관계도 읽은 출처에서 확인하지 못했다. [미확인; S4 조사에서 "T3 interrupt NOT CONFIRMED"]

### 2.7 타이머

- 2.048 MHz 수정 발진기가 4상 1.024 MHz 클록의 기준이다. [확인; 인계 검증 목록]
- AGC 시계는 N36(TIME OF AGC CLOCK, 시/분/초)으로 표시되며 레지스터 TIME2에 대응한다. [확인; S6 (Luminary 099 표)]
- 스케일러(F17 신호)가 1.28초 간격으로 대기 전원을 켠다는 서술이 있다. [부분; S1]
- 11개 자유 타이머(TIME1 ~ TIME6 등) 구성은 읽은 출처에 없다. [미확인]

### 2.8 전원과 대기(Standby) 모드

- 소비 전력 약 55 W, 무게 약 70 lb (32 kg). [부분; S1 정보상자]
- 대기(standby) 모드는 "standby allowed" 스위치로 제어되고 약 5~10 W를 줄인다는 서술이 있으나(70 W 기준), 앞의 55 W와 수치가 서로 다르다. 이 충돌은 해소하지 못했다. [부분; S1]
- 인계 지침: 대기 모드는 비행 중 한 번도 쓰이지 않았다. [확인; 인계 검증 목록]
- STBY 램프는 "컴퓨터가 대기 상태일 때" 켜진다. [확인; S3]

### 2.9 물리 제원

- 약 24 x 12.5 x 6.5 in, 약 70 lb (32 kg), 약 55 W. [부분; S1 정보상자, 1차 자료 미확인]

### 2.10 로직 IC와 코어 로프

- Block II는 Fairchild의 이중 3입력 NOR 게이트 RTL 집적회로 약 2,800개로 구성되었다. [확인; 인계 검증 목록, S1]
- Block I은 1-NOR 게이트 IC 약 4,100개였다. [확인; 인계 검증 목록, S1]
- RTL(Resistor-Transistor Logic)이라는 풀이는 약어 확장으로서 이번 조사에서 별도 출처를 읽지 않았다. [부분]
- 코어 로프의 물리적 구조와 제조(배선 짜기) 방식은 이번 조사에서 읽은 출처에 없다. [미확인]

---

## 3. 소프트웨어 구조

### 3.1 전체 구도

```
         +------------------------- AGC ---------------------------+
 DSKY -> | PINBALL(키/표시) -> Verb/Noun 해석 -> 프로그램/루틴 호출  |
 업링크 ->|                                                         |
         |  Executive (협력형 우선순위 작업 스케줄러)               |
         |  Waitlist  (인터럽트 구동 단기 타이머 작업)              |
         |  Interpreter (의사 명령어 가상 기계, 평균 약 24 ms)      |
         |  재시작 보호 (phase table / waypoint)                    |
         +---------------------------------------------------------+
```

- 위 구도의 Executive/Waitlist/Interpreter/재시작은 [확인; S1, S2]. "PINBALL"이라는 이름은 소스 파일명(PINBALL_NOUN_TABLES.agc)에서만 확인한 것이므로 그림 속 "PINBALL(키/표시)" 표기는 [부분; S6 파일명].

### 3.2 Executive

- **협력형 멀티태스킹**(cooperative multi-tasking) 배치 작업 스케줄러이다. [확인; S1] 작업(job)은 우선순위를 가지며 스스로 제어권을 양보한다. 설계자는 J. Halcombe Laning. [확인; 인계 검증 목록]
- 작업마다 **core set**(작업용 레지스터 묶음)이 필요하고, 벡터 계산용 **VAC**(Vector Accumulator) 영역이 필요한 작업도 있다. [확인; S2]
- 자원이 없으면 BAILOUT 알람이 난다. [확인; S2, S5]
  - **1202**: core set 없음(NO CORE SETS). **1201**: VAC 영역 없음(NO VAC AREAS). (소스 알람표 표기는 01202/01201.)
- **core set 개수 충돌(나란히 표기)**:

| 출처 | core set | 레지스터 수 | VAC 영역 |
|---|---|---|---|
| Eyles (S2) | 8개 | 세트당 12 | 5개 (43 레지스터는 인계 값) |
| Adler 에세이 (S10, 조사 노트 인용) | 7개 | 세트당 12 | 44 워드 |
| Luminary099 EXECUTIVE.agc (S11) | 7개 ("SEVEN SETS OF ELEVEN REGISTERS EACH", NO.CORES 주석) | 11 (NO.CORES 주석) / 12 (JOBWAKE2 주석) | 5개 여부는 [미확인] |

  - 8개와 7개가 서로 다른 이유(예: 실행 중인 작업용 set 0을 포함하는지)는 확인하지 못했다. [미확인] 소스 주석 안에서도 11과 12가 섞여 있다. 어느 쪽을 정답으로 고르지 않는다.
- 알람 01203 "WAITLIST OVERFLOW-TOO MANY TASKS"는 Waitlist 쪽 한도이다. [확인; S5]
- Verb 30 "REQUEST EXECUTIVE", Verb 31 "REQUEST WAITLIST"가 있다. [확인; S5]

### 3.3 Waitlist

- 인터럽트 구동의 **짧은 타이머 작업(task)** 관리이다. [확인; 인계 검증 목록, S1] 긴 계산은 Executive 작업으로 돌린다는 구분의 상세 한계값(예: 몇 ms 이하)은 [미확인].
- 예: 진입 DAP는 Waitlist 호출로 2초마다 동작한다. [확인; S16 CM_ENTRY_DIGITAL_AUTOPILOT 주석 요약]

### 3.4 Interpreter

- 네이티브 AGC 명령어보다 복잡한 **의사 명령어**(pseudo-instruction)를 실행하는 가상 기계이다. [확인; S1] 평균 약 24 ms라는 값은 인계 검증 목록의 수치이다. [확인; 인계 검증 목록]
- 벡터/행렬 연산 등 어떤 명령이 있는지의 목록은 읽은 출처에 없다. [미확인]

### 3.4.1 Average-G와 SERVICER

- SERVICER(AVERAGEG 포함)는 PIPA 가속도 측정값으로 상태 벡터를 갱신하는 반복 작업이다. P11 시작 시 servicer(AVERAGEG)가 시작된다. [부분; S16 P11 주석 요약]
- P40 점화 35초 전 DSKY가 약 5초 비워지고, 컴퓨터가 average G를 측정한 뒤 N40이 돌아온다. [확인; S12 LOI-1 페이지] (E-2448에서도 TIG-35에서 DSKY 공백, TIG-100 s에서 AVERAGEG 시작/복귀 서술이 있다. [부분; S7])
- 달 착륙 하강 중 SERVICER 작업이 반복 실행되고, 1202 알람의 재시작은 "가장 최근 SERVICER 작업"만 복원했다. [확인; S2]
- Average-G의 수학적 구조(PIPA 적분과 중력 평균)는 읽은 출처에 없다. [미확인]

### 3.5 재시작 보호(restart protection)

- 알람 BAILOUT 또는 하드웨어 이상이 발생하면 소프트 리스타트가 수행된다. [확인; S1]
- 재시작 보호는 적절한 지점에서 **waypoint**를 등록하는 방식이다. [확인; S2] 재시작 시 보호되지 않은 기능은 종료된다. [확인; S2]
- 소스에는 "FRESH START AND RESTART", "RESTART TABLES" 절과 알람 01107 "PHASE TABLE FAILURE. ASSUME ERASABLE MEMORY IS DESTROYED"가 있다. [확인; S5]
- phase table의 구조와 해석 규칙은 읽은 출처에 없다. [미확인]
- Verb 69 "CAUSE RESTART", Verb 36 "REQUEST FRESH START". [확인; S5]
- RESTART 램프는 "컴퓨터가 restart 프로그램에 있을 때" 켜진다. [확인; S3]

### 3.6 Apollo 11 LM 1201/1202 알람 사건 (사례)

| 항목 | 내용 | 출처 |
|---|---|---|
| 원인 | 랑데부 레이더(RR) CDU 카운터에 의사 사이클 스틸이 최대 6,400 펄스/초로 발생, 약 13% 부하(Eyles 본문 다른 곳은 약 15%) | [확인; S2, 인계 검증 목록] |
| 근본 원인 | ICD가 두 800 Hz 신호의 위상 동기를 요구하지 않았다. LM-1 시험에서 발견되어 문서화됐으나 수정되지 않았다 | [확인; S2] |
| 추가 부하 | V16N68 표시가 약 10% 부하를 더했다 | [확인; 인계 검증 목록] |
| 결과 | 1202(core set 없음), 1201(VAC 없음) BAILOUT 후 재시작. 재시작은 오래된 SERVICER 작업을 지우고 V16N68 모니터를 떨어뜨림(표시는 N63으로 복귀) | [확인; S2, 인계 검증 목록] |
| 크루 보고 시각 | ALSJ 전사: 102:38:26 "Program Alarm" / 102:38:30 "It's a 1202". 미션 리포트상 첫 1202는 102:39:02. 1201은 102:42:17~19(출처별 차이) | [부분; S12, S2] (출처별 시각이 서로 다르며 수 초 차이를 맞추지 않았다) |
| 운용 판단 | 지상(Bales)은 위협이 아니라고 판단, Duke "We're Go" | [부분; 조사 노트 B → S12] |

### 3.7 버전: Colossus와 Luminary

| 기체 | 소프트웨어 | Apollo 11 | 확인 |
|---|---|---|---|
| CM(사령선) | Colossus | Colossus 2A = Comanche 055 | [확인; S1, S16] |
| LM(달착륙선) | Luminary | Luminary 1A (build 099) | [확인; S1, S17] |
| 후기 | Colossus 3 / Luminary 1E (E-2448) | Apollo 15~17 시기 | [확인; S7 표지] |

- 버전마다 프로그램 목록과 Verb/Noun 정의가 다르다(예: P15는 E-2448에 있으나 Comanche 055 디렉터리에서는 찾지 못함). [부분; 조사 노트 A]

### 3.8 프로그램, 루틴, Verb/Noun 체계

- **Program (Pnn)**: 주요 모드(major mode). PROG 창(MAJ MODE)에 번호가 표시된다. Verb 37로 바꾼다. [확인; S5, S3]
- **Routine (Rnn)**: 프로그램이 호출하는 하위 기능. 예: R30 궤도 매개변수 표시(V82), R31 랑데부 매개변수 표시(V83), R03 DAP 데이터 로드(V48), R62 승무원 지정 기동(V49), R60 자동 자세 기동, R21/R22 레이더 지정/데이터 읽기(LM). [확인; S5, S7, S17]
- **Verb**: "동작", **Noun**: "그 동작이 다룰 데이터"를 정한다. [확인; S15, S4]
- 일반 Verb(00~37)는 Noun이 필요하고, 확장 Verb(40~99)는 Noun 없이 실행되는 것이 보통이라는 설명이 있다. [부분; S15 2차 자료] (Verb 48, 49, 82, 83 등 일부는 Noun 없이 쓰이는 것으로 보이나 모두를 확인한 것은 아니다.)
- 프로그램 이름 규칙: P00~P07 대기/점검, P11~P15 발사/상승, P20대 항법/랑데부, P30대 기동 목표, P40대 기동 실행, P50대 정렬, P60대 진입 등은 이번에 읽은 프로그램 번호와 일치하나 "대역별 규칙" 자체를 설명한 출처는 읽지 못했다. [부분]

---

## 4. G&N 시스템에서 AGC의 위치

### 4.1 구성

```
        +---------+   가속도/각도    +---------+   조종/점화     +------------+
        |  IMU    |----------------->|  AGC    |---------------->| 엔진(SPS/  |
        | (PIPA,  |<-- 정렬 토크 ----| (CMC/   |<-- 상태 -------| DPS/APS)   |
        |  자이로)|                  |  LGC)   |                 | RCS 분사기 |
        +---------+                  |         |---> DAP ------->| TVC 짐벌   |
   광학 (SXT/SCT) --> CDU --> 마크 -->|         |                 +------------+
   랑데부/착륙 레이더 (LM) --------->|         |---> DSKY, 업/다운링크
        +---------------------------+
```

- 이 블록도는 아래 항목의 서술을 모은 개념도이며 출처 문서의 그림이 아니다. [부분]
- G&N(Guidance and Navigation) 전체를 LM에서는 **PGNCS**(Primary Guidance, Navigation and Control System)라고 부른다. [확인; S8 제목] 사령선 쪽 약칭 CMC, 달착륙선 컴퓨터 LGC는 소스/전사에서 사용되는 이름이다. [확인; S2, S16]

### 4.2 IMU

- 3축 짐벌, 가장 안쪽 안정 부재(stable member)는 6인치 베릴륨 정육면체이고 **자이로 3개와 가속도계 3개**를 포함한다. [부분; S8 (Wikipedia PGNCS 교차 확인용 출처)]
- 가속도계는 PIPA로 불린다(Apollo accelerometer (PIPA)). [부분; S8 캡션] PIPA 약어 풀이는 [미확인].
- IMU가 얼마나 정렬되어 있는지를 NO ATT 램프, 알람 00210("IMU NOT OPERATING"), 00220("IMU NOT ALIGNED - NO REFSMMAT") 등이 알린다. [확인; S5, S3]
- REFSMMAT의 정의("reference stable member matrix")는 읽은 출처에서 확인하지 못했다. [미확인] REFSMMAT 존재 플래그를 P30이 점검한다는 소스 서술은 있다. [부분; S16 P30]
- 짐벌락: 가운데 짐벌 각도가 70도 초과이면 GIMBAL LOCK 램프가 켜진다. [확인; S3] E-2448은 P52에서 가운데 짐벌 85도 초과 시 경고라고 쓴다(조사 노트: "Gimbal-lock warning if middle > 85 deg"). [부분; S7] 70도(램프)와 85도(P52 경고 기준)가 서로 다른 값인 이유는 확인하지 못했다.

### 4.3 CDU(Coupling Data Unit)

- DSKY에서 Verb 40 "ZERO CDU-S", Verb 41 "COARSE ALIGN CDU-S", Noun 20 "ICDU ANGLES"가 있다. [확인; S5]
- "coupling data unit"이라는 풀이는 읽은 출처에 없다. [미확인] (P52 표에는 IMU CDU(ICDU)와 광학 CDU가 구분되어 나온다. [확인; S7])
- TRACKER 램프는 "광학 커플링 유닛 중 하나가 고장일 때" 켜진다. [확인; S3]
- LM의 레이더 CDU 카운터 스퓨리어스 입력이 1201/1202 사건의 원인이었다(3.6절). [확인; S2]

### 4.4 광학

- 정밀 **육분의**(sextant, SXT)와 저배율 광시야(60도) **스캐닝 망원경**(SCT)이 있다. [부분; S8]
- E-2448은 SXT 28배/시야 1.8도, SCT 1배/60도, 광학 CDU 해상도(축 40", 트러니언 10")를 적는다. [부분; S7 Colossus 3 기준, 2차 인용]
- 조사 노트의 NASSP 정렬 체크리스트는 SCT 60도, SXT 10도라고 적는다. 이는 E-2448의 SXT 1.8도와 충돌하는 값이며 시뮬레이터 위키라는 2차 자료이다. [부분; 조사 노트 C → NASSP] (두 값을 나란히 적는다. 1.8도(E-2448) 대 10도(NASSP))
- LM은 AOT(정렬 광학 망원경)로 별을 이용한 정렬을 한다(별 Sirius/Rigel 제안, 123:18:43 Sun 가림 detent 6). [부분; S12 ALSJ]

### 4.5 DAP (Digital Autopilot)

- CM: RCS DAP 초기화는 Verb 48 "REQUEST DAP DATA LOAD ROUTINE (R03)". [확인; S5]
- Apollo 11 TD&E 중 DAP가 전이 기동을 수행했고, RHC가 detent를 벗어난 채 DAP를 켜서 자세 유지로 바뀌는 문제가 있었다. [확인; S13 8.6.1]
- 진입 DAP(entry DAP)와 대기권 밖 DAP(extra-atmospheric)가 있다. 몸체 각속도 계산은 0.1초 주기, 진입 조종은 2초 주기 Waitlist 호출이다. [확인; S16]
- SPS 분사는 TVC(추력 벡터 제어)를 쓴다. 점화 약 4초 후 사전 계산 트림 위치로 짐벌 지령, 분사 종료 후 2.5초 지연 뒤 RCS DAP가 시작된다. [확인; S16 P40]

### 4.6 엔진 인터페이스

| 엔진 | 기체 | AGC가 하는 일 | 확인 |
|---|---|---|---|
| SPS | CSM | P40이 TVC로 유도, ullage는 RCS +X, 점화 5초 전 V99 깜박임으로 점화 허가를 요청 | [확인; S16 P40, S12 TEI/LOI] |
| DPS | LM | 자동 스로틀 모드로 점화(점화 시 10%), P63 유도. 스로틀업 102:33:25, 스로틀다운 102:39:35 | [확인; S12] |
| APS | LM | P12 상승 유도, P42는 APS 분사 프로그램 | [확인; S17] |
| RCS | 둘 다 | P41(RCS 분사), DAP 자세 제어 | [확인; S16, S17] |

- Apollo 11 TEI의 EMS(Entry Monitor System) Delta-V 카운터는 G&N 실패 시 엔진을 정지시킬 수 있는 백업이다. [확인; S12 TEI 페이지]

### 4.7 AGS와의 관계 (간단히)

- LM에는 PGNCS 외에 AGS가 백업으로 있고 DEDA(키패드)로 조작된다. 예: AGS 정렬(PGNS에 맞춤), 착륙 후 `CLEAR, 413, ENTER, +10000, ENTER`. [확인; S12 ALSJ O'Brien 주석]
- 상승 시 "Our guidance recommendation is PGNS"가 승인 조건이었다. [확인; S12 124:04:51]
- AGS의 정식 명칭과 내부 구조는 읽은 출처에 없다. [미확인] AGS와 PGNCS의 독립성(별도 컴퓨터)은 전사에서 두 시스템을 별도로 부르는 것으로 보이나 직접 서술은 확인하지 못했다. [부분]

---

## 5. DSKY 완전 사용법

DSKY = Display and Keyboard 장치이다. [확인; S9 제목]

### 5.1 표시부

```
 [UPLINK ACTY]  [TEMP]           [COMP ACTY]        PROG  [ 88 ]
 [NO ATT     ]  [GIMBAL LOCK]    VERB [88]          NOUN  [ 88 ]
 [STBY       ]  [PROG]           ---------------------------
 [KEY REL    ]  [RESTART]        R1  [+/- 88888]
 [OPR ERR    ]  [TRACKER]        R2  [+/- 88888]
 [    -      ]  [ALT]            R3  [+/- 88888]
 [    -      ]  [VEL]
```

- 배열은 프로젝트 설계 문서(doc/00)가 Virtual AGC 도면/사진에 맞춘 그림이다. [부분; S14, S18] 램프 색(왼쪽 흰색, 오른쪽 호박색, COMP ACTY 녹색)은 프로젝트 문서에서 "사진 기억에 근거한 가정"이라고 명시된 것이다. [미확인]
- 숫자 창은 녹색 전계발광 7-세그먼트. [확인; 인계 검증 목록]
- 21자리 표시: R1/R2/R3 각 5자리(15) + VERB 2 + NOUN 2 + PROG 2 = 21. [확인; 인계 검증 목록] 각 R 레지스터에는 부호 칸이 따로 있다. [확인; S14, S18]
- **십진 값은 부호와 함께, 8진 값은 부호 없이** 표시된다. [확인; 인계 검증 목록, S14]
- 알람 코드 N09는 8진으로 표시된다. [확인; S6]

### 5.2 키 19개

| 구분 | 키 | 개수 |
|---|---|---|
| 숫자 | 0 1 2 3 4 5 6 7 8 9 | 10 |
| 부호 | + − | 2 |
| 명령 | VERB, NOUN, ENTR, CLR, PRO, KEY REL, RSET | 7 |
| 합계 | | 19 |

- S4와 S3 모두 위 구성을 적는다. [확인; S3, S4] S4: "각 키는 5비트 코드를 생성, PROCEED만 코드가 없다". [확인; S4]
- 키 배열(7열 x 3행, VERB/NOUN과 ENTR/RSET이 반 칸 아래)은 프로젝트 문서의 공개 설명 기반 재구성이며 사진과의 최종 대조가 필요하다고 스스로 밝히고 있다. [부분; S18]

### 5.3 키 기능

| 키 | 기능 | 확인 |
|---|---|---|
| VERB | 새 Verb 입력 시작 | [부분; S9] |
| NOUN | 새 Noun 입력 시작 | [부분; S9] |
| ENTR | 입력한 명령/값을 실행/입력 | [부분; S9] |
| CLR | 입력 중인 데이터 줄을 지우고 다시 시작 | [부분; S9] |
| PRO(PROCEED) | 컴퓨터가 요청한 데이터가 맞음을 확인하고 프로그램이 계속 진행. 키 코드가 없는 특수 키 | [부분; S9, S4] |
| KEY REL | 키보드를 AGC에 돌려줘 새 데이터를 표시할 수 있게 함 | [부분; S9] |
| RSET | 프로그램 알람을 리셋하고 PROG 램프를 끔 | [부분; S9] |
| + / − | 십진 입력의 부호 | [확인; S4] |

### 5.4 기본 입력 형식

- 표준 입력: `VERB V1 V2 NOUN N1 N2 ENTR`. [확인; S4]
- 예: V37N31E는 Verb 37, Noun 31, Enter를 뜻한다. [부분; S15]
- 프로그램 변경(V37)은 Noun 키 없이 두 자리 프로그램 번호를 받는다(예: `V37E 40E`가 Apollo 11 TEI에서 P40 선택). [확인; S12 TEI 페이지, S18]

### 5.5 깜박임(flashing)

- Verb 실행 후 컴퓨터가 운영자 데이터를 필요로 하면 VERB와 NOUN 표시가 **약 1초에 한 번 깜박인다**. [확인; S4]
- 컴퓨터는 PROG, VERB, NOUN 표시를 깜박여 운영자와 교신한다. [확인; S3]
- 깜박임 중에 `PRO`로 진행, `ENTR`로 다른 선택, 데이터 로드 등을 하는 규칙은 프로그램마다 다르다(5.10, 5.11절 예). "깜박임 = 승무원 응답 대기"의 일반 규칙을 출처 한 줄로 확정한 문장은 읽지 못했다. [부분]
- 깜박이는 V/N 중 `V50 N25`는 "PLEASE PERFORM" + 체크리스트 코드, `V99`는 "PLEASE ENABLE ENGINE", `V04 N06`은 옵션 선택, `V06`/`V16`은 표시이다. [확인; S5, S7, S17]

### 5.6 로드 규칙

- 부호가 있으면 **십진**, 없으면 **8진**으로 읽는다. [확인; S4]
- 8진 입력은 숫자 8, 9를 쓰지 않는다. [부분; S9]
- 십진 데이터는 `+`/`−`와 **5자리 모두** 입력해야 한다. [확인; S14(Virtual AGC 요약), 인계 검증 목록] (S4에는 이 문장이 그대로 없어 한 출처만으로 확인한 것이다.)
- 로드 Verb: V21(R1), V22(R2), V23(R3), V24(R1, R2), V25(R1, R2, R3). [확인; S5]
- CLR은 현재 입력 중인 레지스터 줄을 지운다. [확인; S14, S9]
- 예: AGC 시계 설정 `V25 N36 ENTR` 후 R1 시, R2 분, R3 초x100을 로드. [확인; 인계 검증 목록, S14] 시뮬레이터에서의 입력 순서는 doc/02 5.2절에 있다.

### 5.7 램프 전부

| 램프 | 의미 | 기체 | 확인 |
|---|---|---|---|
| UPLINK ACTY | 지상에서 데이터를 받는 중 | CM/LM | [확인; S3] |
| NO ATT | 관성계가 자세 기준을 제공할 수 없음 | CM/LM | [확인; S3] |
| STBY | 컴퓨터가 대기 상태 | CM/LM | [확인; S3] |
| KEY REL | 컴퓨터가 프로그램을 완료하려면 DSKY 제어가 필요함 | CM/LM | [확인; S3] |
| OPR ERR | 컴퓨터가 키보드 입력 오류를 감지 | CM/LM | [확인; S3] |
| TEMP | 안정판(platform) 온도가 허용치를 벗어남 | CM/LM | [확인; S3] |
| GIMBAL LOCK | 가운데 짐벌 각도가 70도 초과 | CM/LM | [확인; S3] |
| PROG | 아래 충돌 표 참조 | CM/LM | [부분] |
| RESTART | 컴퓨터가 restart 프로그램에 있음 | CM/LM | [확인; S3] |
| TRACKER | 광학 커플링 유닛 중 하나가 고장 | CM/LM | [확인; S3] |
| COMP ACTY | 컴퓨터가 프로그램을 수행 중 | CM/LM | [확인; S3] |
| ALT | 고도 판독값이 한계 밖 | LM 전용 | [부분; S3] |
| VEL | 속도 판독값이 한계 밖 | LM 전용 | [부분; S3] |

- 램프는 "앞 7개와 뒤 7개(총 14칸)"로 구성되며 CM(Apollo 8 이후)에는 ALT/VEL이 없고 LM에만 있다. [확인; 인계 검증 목록, S14]
- ALT/VEL의 정확한 의미: S3는 "고도/속도 판독값이 사전 정한 한계 밖"이라고 하고, 다른 곳의 2차 자료는 "착륙 레이더 데이터 불량"이라고 한다. 1차 자료로는 확정하지 못했다. [부분]

**PROG 램프 충돌(나란히)**

| 출처 | 설명 |
|---|---|
| S3 (Computers in Spaceflight) | "컴퓨터가 승무원의 추가 정보 입력을 기다릴 때" 켜짐 |
| S9 (NASSP) / 프로젝트 구현 | **프로그램 알람(내부 오류)** 이 발생했음을 나타내고 RSET으로 끔 |
| S14 (Virtual AGC yaDSKY 요약, doc/00 인용) | 시작 시 프로그램 알람 보고에 사용되며 RSET이 끔. 단 yaDSKY에는 "입력 대기 시 점등"이라는 표현도 있다고 doc/00이 기록 |

- 이 문서는 어느 쪽도 정답으로 고르지 않는다. 시뮬레이터는 알람 해석을 쓴다. [부분]

### 5.8 PROG 숫자 창, VERB/NOUN 창

- PROG(MAJOR MODE) 창에는 현재 실행 중인 프로그램 번호가 나온다. [확인; S12(예: 전사 "mode register changed to 64"), S2]
- VERB/NOUN 창은 현재 입력 중인 명령 또는 컴퓨터가 요청한 표시를 보인다. [확인; S4]

### 5.9 알람/OPR ERR/RESTART 대응

| 상황 | 표시 | 대응 | 확인 |
|---|---|---|---|
| 프로그램 알람 | PROG 램프 점등(해석 충돌, 5.7절). 알람 코드는 `V05 N09 ENTR`으로 8진 표시 | `RSET`으로 램프 끔. 코드 의미는 7장 | [부분; S9, S2] (Apollo 11 하강 중 "Alarm code 1202 appeared on the DSKY" S2) |
| OPR ERR | 키보드 입력 오류 | `RSET`으로 끄는 것은 시뮬레이터 규칙. 실제 컴퓨터에서 RSET으로 OPR ERR를 끄는지는 [미확인] | [부분; S3, S18] |
| RESTART | restart 프로그램 진입 | 어떻게 끄는지는 [미확인] | [부분; S3] |
| 알람 후 모니터 소실 | 재시작이 보호되지 않은 표시(예: V16N68)를 떨어뜨림 | 원하는 모니터를 다시 요청. Apollo 11에서는 표시가 N63으로 돌아갔다 | [확인; S2, 인계 검증 목록] |
| KEY REL | 프로그램이 표시를 요청 | `KEY REL`로 키보드를 돌려줌 | [부분; S3, S9] |

- 알람 시 PROG와 RESTART 램프가 함께 켜진 구체적 상태는 Apollo 11 하강 기록에서 확인하지 못했다. [미확인; 조사 노트 B]

### 5.10 자주 쓰는 Verb 표 (확인된 것만)

일반(regular) Verb와 확장(extended) Verb. 정의는 Comanche 055 ASSEMBLY_AND_OPERATION_INFORMATION.agc의 표기를 그대로 번역했다. [확인; S5]

| Verb | 원문 | 뜻 |
|---|---|---|
| 05 | DISPLAY OCTAL COMP 1,2,3 IN R1,R2,R3 | 8진 3성분 표시 |
| 06 | DISPLAY DECIMAL IN R1 OR R1,R2 OR R1,R2,R3 | 십진 표시(1회) |
| 16 | MONITOR DECIMAL IN R1 OR R1,R2 OR R1,R2,R3 | 십진 모니터(갱신) |
| 21 | LOAD COMPONENT 1 INTO R1 | R1 로드 |
| 22 | LOAD COMPONENT 2 INTO R2 | R2 로드 |
| 23 | LOAD COMPONENT 3 INTO R3 | R3 로드 |
| 24 | LOAD COMPONENT 1,2 INTO R1,R2 | R1, R2 로드 |
| 25 | LOAD COMPONENT 1,2,3 INTO R1,R2,R3 | R1~R3 로드 |
| 30 | REQUEST EXECUTIVE | Executive 요청 |
| 31 | REQUEST WAITLIST | Waitlist 요청 |
| 32 | RECYCLE PROGRAM | 프로그램 재순환 |
| 33 | PROCEED WITHOUT DSKY INPUTS | 입력 없이 진행(PRO와 같은 효과로 쓰임) |
| 34 | TERMINATE FUNCTION | 기능 종료 |
| 35 | TEST LIGHTS | 램프 테스트 |
| 36 | REQUEST FRESH START | 초기화 시작 |
| 37 | CHANGE PROGRAM (MAJOR MODE) | 프로그램 변경 |

### 5.11 확장 Verb 표

| Verb | 원문(출처) | 뜻 | 확인 |
|---|---|---|---|
| 40 | ZERO CDU-S | CDU 영점 | [확인; S5] |
| 41 | COARSE ALIGN CDU-S | CDU 개략 정렬(`V41 N20`, `V41 N91` 광학) | [확인; S5; N91 광학 사용은 S7 부분] |
| 42 | (E-2448) 정밀 정렬, V21 N93 깜박임으로 토크각 로드 | 수동 fine align | [부분; S7] |
| 46 | ESTABLISH G+C CONTROL | G&C 제어 확립 (E-2448에서는 Saturn takeover에 쓰임) | [확인; S5; 부분 S7] |
| 48 | REQUEST DAP DATA LOAD ROUTINE (R03) | DAP 데이터 로드 | [확인; S5] |
| 49 | REQUEST CREW DEFINED MANEUVER ROUTINE (R62) | 승무원 지정 자세 기동 | [확인; S5] |
| 50 | PLEASE PERFORM | 승무원 조치 요청(`V50 N25` + 체크리스트 코드) | [확인; S5] |
| 69 | CAUSE RESTART | 의도적 재시작 | [확인; S5] |
| 82 | REQUEST ORBIT PARAM DISPLAY (R30) | 궤도 매개변수 표시 | [확인; S5] |
| 83 | REQUEST REND PARAM DISPLAY (R31) | 랑데부 매개변수 표시 | [확인; S5] |
| 94 | PERFORM CYSLUNAR ATTITUDE MANEUVER (P23) | P23 자세 기동 (Apollo 11 전사에서는 "flashing 51로 돌아가 자동 기동을 다시" 의미로 쓰임, S12) | [확인; S5; 부분 S12] |
| 96 | TERMINATE INTEGRATION AND GO TO P00 | 적분 종료 후 P00 | [확인; S5] |
| 99 | PLEASE ENABLE ENGINE | 엔진 점화 허가 요청 | [확인; S5] |
| 04 (N06) | (E-2448) 깜박임 `V04 N06` = IMU 방향 옵션 지정 | P52 | [부분; S7] |
| 51 | (E-2448) 깜박임 V51 "please mark" | 마크 요청 | [부분; S7] |
| 59 | AFJ 전사 "Please calibrate" | 광학 보정 | [부분; S12] |
| 66 | AFJ 해설: CSM 상태 벡터를 LM 슬롯에 복사 | | [부분; S12] |
| 75 | Comanche 055 P11 헤더: "BACKUP THRU VERB 75 ENTER" | P11 수동 시작 | [확인; S16 P11] |
| 78 | (E-2448) 발사 방위각 변경 `V78 E`, `V06 N29` | | [부분; S7] |
| 97 | (E-2448) 추력 실패 시 깜박임 `V97 N40` | | [부분; S7] |
| 57 | 전사: "to allow navigation to incorporate the landing radar measurements" | 착륙 레이더 데이터 반영 | [부분; S2] 키 순서는 [미확인] |
| 62, 64, 74, 76 | 전사에 등장 | 의미 | [미확인] |

- Verb 91 `V91E`는 "bank sums"(메모리 뱅크 합계 점검)이라는 인계 검증 목록이 있다. [부분; 인계 검증 목록(Virtual AGC)] 출처 원문은 읽지 못했다.

### 5.12 자주 쓰는 Noun 표

Luminary 099 Noun 표(S6)와 Comanche 055 표(S5)를 구분한다. **같은 번호라도 CM과 LM에서 정의가 다르다.**

| Noun | CM (Colossus/Comanche 055) | LM (Luminary 099) | 확인 |
|---|---|---|---|
| 09 | ALARM CODES (8진, 3개) | 동일 | [확인; S6, 동일 용도 S5 구조] |
| 20 | ICDU ANGLES | (LM 정의는 미조사) | CM [확인; S5] / LM [미확인] |
| 22 | NEW ICDU ANGLES (P52에서는 OGA/IGA/MGA, 진입 P62/P66도 사용) | (미조사) | CM [확인; S5, S16] / LM [미확인] |
| 33 | TIME OF IGNITION | TIME OF IGNITION (HRS,MIN,SEC) | [확인; S5, S6] |
| 34 | (미조사) | TIME OF EVENT | CM [미확인] / LM [확인; S6] |
| 36 | (미조사) | TIME OF AGC CLOCK (HRS,MIN,SEC) | LM [확인; S6] |
| 37 | (미조사) | TIG OF TPI | LM [확인; S6] |
| 40 | TIME FROM IGNITION/CUTOFF, VG, DELTA V (ACCUMULATED) | `V06N40`: TG/VG/DELTAVM 관련 표시(P40 주석) | CM [확인; S5] / LM [부분; S17] |
| 42 | APOGEE, PERIGEE, DELTA V (REQUIRED) | (P30 주석에 표시됨, 정의는 [미확인]) | CM [확인; S5] |
| 43 | (미조사) | LATITUDE / LONGITUDE / ALTITUDE (deg XXX.XX, deg XXX.XX, nmi XXXX.X) | LM [확인; S6] |
| 44 | APOGEE, PERIGEE, TFF | (미조사) | CM [확인; S5] |
| 45 | E-2448: MARKS / TFI / MGA | LM: MARKS / TTI OF NEXT BURN(분,초) / MGA | [부분; S7] / LM [확인; S6] |
| 47 | CSM 중량 (AFJ 해설) | | [부분; S12] |
| 48 | 피치/요 트림 (AFJ 해설) | | [부분; S12] |
| 54 | (미조사) | RANGE (nmi) / RANGE RATE (ft/s) / THETA (deg) | LM [확인; S6] |
| 60 | GMAX, VPRED, GAMMA EI | HORIZONTAL VELOCITY / ALTITUDE RATE / COMPUTED ALTITUDE | [확인; S5] / [확인; S6] |
| 61 | IMPACT LATITUDE, IMPACT LONGITUDE, HEADS UP/DOWN | TIME TO GO IN BRAKING PHASE / TIME TO IGNITION / CROSS RANGE DISTANCE | [확인; S5] / [확인; S6] |
| 62 | INERTIAL VEL MAG (VI), ALT RATE CHANGE (HDOT), ALT ABOVE PAD RADIUS (H) | ABSOLUTE VALUE OF VELOCITY / TIME TO IGNITION / DELTA V (ACCUMULATED) | [확인; S5] / [확인; S6] |
| 63 | RANGE 297,431 TO SPLASH (RTGO), PREDICTED INERT VEL (VIO), TIME FROM 297,431 (TFE) | ABSOLUTE VALUE OF VELOCITY / ALTITUDE RATE / COMPUTED ALTITUDE | [확인; S5] / [확인; S6] |
| 64 | DRAG ACCELERATION, INERTIAL VELOCITY (VI), RANGE TO SPLASH | TIME LEFT FOR REDESIGNATION -- LPD ANGLE / ALTITUDE RATE / COMPUTED ALTITUDE | [확인; S5] / [확인; S6] |
| 66 | (P67 표시로 코드에 쓰임, S16. 정의는 [미확인]) | LR RANGE / POSITION (R1~R3 매핑 [부분]) | CM [미확인] / LM [부분; S6] |
| 67 | RANGE TO TARGET, PRESENT LATITUDE, PRESENT LONGITUDE | (미조사) | CM [확인; S5] |
| 68 | (미조사) | SLANT RANGE TO LANDING SITE / TIME TO GO IN BRAKING PHASE / LR ALTITUDE - COMPUTED ALTITUDE | LM [확인; S6] |
| 69 | BETA, DL, VL | SPARE (Luminary 099) | [확인; S5] / [확인; S6] |
| 76 | (미조사) | DESIRED HORIZONTAL VELOCITY / DESIRED RADIAL VELOCITY / CROSS-RANGE DISTANCE | LM [확인; S6] |
| 81 | DELTA V (LV) | DELTA V (LV) x3 | [확인; S5, S6] |
| 85 | VG (BODY) | VG (BODY) x3 | [확인; S5, S6] |
| 89 | 랜드마크 위도, 경도/2, 고도 (AFJ 해설) | LANDMARK LATITUDE / LONGITUDE/2 / ALTITUDE | CM [부분; S12] / LM [확인; S6] |
| 05 | 별 각도 차이(P52, E-2448) | | [부분; S7] |
| 06 | `V04 N06`: IMU 방향 옵션(1 Preferred, 2 Nominal, 3 REFSMMAT, 4 Landing Site) | | [부분; S7] |
| 70/71 | 광학 보정 천체 코드/기록된 별 코드(E-2448) | | [부분; S7] |
| 88 | 행성 단위 벡터(E-2448) | | [부분; S7] |
| 91/92/93 | N92 광학 각도 표시, N93 자이로 토크 각도(E-2448; Apollo 11 전사 N93 확인) | | [부분; S7, S12] |
| 95 | (E-2448 P15) 점화 후 시간 TFT, VG, VI | | [부분; S7] |

- 충돌 예: N68을 ALSJ 해설(O'Brien)은 "착륙지점까지 거리, pitchover까지 시간, 속도"로 설명하고 Luminary 099 표는 "제동 구간 잔여시간, LR 고도 - 계산 고도"로 정의한다. 이 문서는 소스 표(S6)를 쓴다. [확인; 조사 노트 B]
- N66의 R1~R3 매핑과 스케일 요소는 완전 확인하지 못했다. [부분; S6]

### 5.13 대표 키 순서 (출처가 있는 것만)

| 목적 | 키 순서 | 출처/수준 |
|---|---|---|
| 램프 테스트 | `V35 E` | 모든 램프, VERB/NOUN 88 깜박임, 약 5초 후 종료, 이후 88 유지 [확인; 인계 검증 목록, S14] |
| 시계 보기 | `V16 N36 E` | 1초 갱신 [확인; 인계 검증 목록, S14] |
| 시계 설정 | `V25 N36 E` + R1/R2/R3 로드 | [확인; 인계 검증 목록, S14] |
| 알람 코드 보기 | `V05 N09 E` | [확인; S6, S16, 인계 검증 목록] |
| 초기화 | `V36 E` (fresh start) | [확인; S5, 인계 검증 목록] |
| 프로그램 변경 | `V37 E nn E` (예 `V37E 40E`, `V37E 63E`, `V37E 20E`, `V37E 00E`) | [확인; S12, S2, S17] |
| 진행 | `PRO` 또는 `V33 E` | [확인; S5, 인계 검증 목록] |
| 종료 | `V34 E` | [확인; S5] |
| 재순환/거부 | `V32 E` (E-2448 P52/P40 표) | [부분; S7] |
| 메모리 뱅크 합계 | `V91 E` | [부분; 인계 검증 목록] |
| 점화 허가 | 점화 5초 전 V99 깜박임에 `PRO` | [확인; S12 TEI, LOI-1] |
| 하강 점화 | `V37E 63E`, 점화 약 5초 전 `V06 N62` 표시 중 `PRO` | [확인; S2] |
| 착륙 하강 모니터 | `V16 N68` (알람 사건 당시), 현재는 N63 복귀 | [확인; S2] |

- 하강 전용 N62 표시: Eyles는 "Verb 06 Noun 62가 DSKY에 나타났다"고 적고(점화 카운트다운), 점화 후 N63 표시로 바뀐다. [확인; S2, 조사 노트 B]
- P40 SPS 분사 순서(E-2448, Colossus 3): `V37E 40E` → 깜박임 `V50 N18`(희망 짐벌각, `PRO`로 자동 기동) → `V50 N25`(R1=00204, SPS 짐벌 구동 시험, `PRO` 실행/`ENTR` 건너뛰기) → `V06 N40` 매초 → TIG-35 DSKY 공백 5초 → 점화 5초 전 `V99 N40` 깜박임(`V34E` 중지, `PRO` 점화) → 분사 후 `V16 N40` → `V16 N85`(잔여 VG) → `PRO` → V37 깜박임. [부분; S7] (Apollo 11 소스와 전사로 V99, V06N40, V16N40/N85, V37E40E는 확인. V50 N18는 S7만. [확인/부분; S16, S12])
- P52 IMU 정렬 순서(E-2448 7.2.2, Colossus 3): [부분; S7, Apollo 11 전사와 N05/N93/N71 일치 확인 S12, S13]

| 단계 | 표시 | 조작 |
|---|---|---|
| 1 | | `V37 E 52 E` |
| 2 | 깜박임 `V04 N06` (R1=옵션 코드 00001 Preferred, 00002 Nominal, 00003 REFSMMAT, 00004 Landing Site) | 변경은 `V22 E 코드 E`, `PRO` |
| 3 | 깜박임 `V06 N22` (제안 짐벌각) | `PRO` (가운데 짐벌 85도 초과 경고 시 `V32E` 재순환) |
| 4 | 깜박임 `V50 N25`, R1=00013 (개략 정렬) | `PRO` 개략 정렬 / `ENTR` 자이로 토크 개략 정렬 |
| 5 | 깜박임 `V50 N25`, R1=00015 (천체 획득) | `PRO` 컴퓨터 별 선택 / `ENTR` 수동 |
| 6 | 깜박임 `V01 N70` (별 코드) | `V21 E 코드 E` |
| 7 | 깜박임 `V51` "please mark" | 별을 중앙에 놓고 MARK, 이어 `V50 N25` R1=00016에서 `PRO` |
| 8 | `V01 N71` (기록된 별 코드) | `PRO`, 두 번째 별에서 6~8단계 반복 |
| 9 | 깜박임 `V06 N05` (별 각도 차이, 00001=0.01도) | `PRO` 수락, `V32E` 거부 |
| 10 | 깜박임 `V06 N93` (자이로 토크 각도) | `PRO` 토크(Apollo 11 전사: "Proceed, torque") |
| 11 | `V50 N25` R1=00014 (정렬 확인) | `PRO`로 3번째 별 확인, `ENTR` 종료 |

- Apollo 11 첫 P52(000:47:24): 별 30(Menkent), 37(Nunki), 별 각도 차이 0.01, 토크각 N93 readback +00016, +00033, +00152; 점검 별 34. [확인; S12, S13]
- 위 N93 X축 값 충돌: 전사 readback "00016" 대 미션 리포트 표 "0.018" (AFJ는 차이를 설명하지 않음). [부분; 조사 노트 A]

---

## 6. 프로그램 목록

수준 표시: "공식명"은 소스/문서에 적힌 그대로의 영문 표기가 확인된 것만 [확인]. 기능 설명만 확인된 것은 [부분].

### 6.1 CM (Colossus / Comanche 055 기준)

| 번호 | 이름/기능 | 확인 |
|---|---|---|
| P00 | CMC Idling (E-2448은 "CMC Power Down"으로도 표기) | [부분; S7] |
| P01 | Initialization (IMU 개략 정렬 후 P02로 진입) | [부분; S7] |
| P02 | Gyro Compassing (P01에서 자동 진입, 리프트오프 이산 신호가 오면 P11) | [부분; S7] |
| P03 | Optical Verification of Gyro Compassing | [부분; S7] |
| P11 | Earth Orbit Insertion Monitor (Comanche 055 소스 헤더) | [확인; S16 P11] |
| P15 | TLI Initiate/Cutoff (E-2448만; Comanche 055에는 소스 파일을 찾지 못함) | Colossus 3 [부분; S7] / Apollo 11 [미확인] |
| P20 | RENDEZVOUS NAVIGATION PROGRAM 20 (E-2448: Universal Tracking and Rendezvous Navigation) | [확인; S16 P20-P25 / S17], 이름 충돌은 나란히 표기 |
| P21 | Ground-track Determination | [부분; S7] |
| P22 | Orbital Navigation (랜드마크 추적, Apollo 11 사용) | [부분; S7, S12] |
| P23 | Cislunar Navigation (별-지평선 마크) | [부분; S7, S13] |
| P24 | Rate-aided Optics Tracking | [부분; S7] |
| P27 | CMC Update (지상 업링크 수용) | [부분; S7] |
| P30 | EXTERNAL DELTA-V TARGETTING PROGRAM (소스 설명 줄) | [확인; S16] |
| P37 | Return-to-Earth (E-2448 목차). Comanche 055 알람에서 P37 관련 00612/00613 존재 | [부분; S7, S5] |
| P40 | P40CSM: SPS 분사 | [확인; S16] |
| P41 | P41CSM: RCS 분사 | [확인; S16] |
| P47 | Thrust Monitor (E-2448) | [부분; S7] |
| P51 | IMU Orientation Determination | [부분; S7] |
| P52 | IMU Realign | [부분; S7] |
| P53 | Backup IMU Orientation Determination | [부분; S7] |
| P54 | Backup IMU Realign | [부분; S7] |
| P61 | TO CALCULATE AND DISPLAY EMS INITIALIZATION DATA (진입 준비) | [확인; S16 P61] |
| P62 | TO NOTIFY CREW WHEN GNC SYSTEM IS PREPARED FOR CM/SM SEPARATION (분리 준비, 진입 자세) | [확인; S16] |
| P63 | TO INITIALIZE THE ENTRY EQUATIONS (0.05 g 대기) | [확인; S16] |
| P64 | 0.05 g 이후 진입 유도 시작 (공식 영문 제목은 [미확인]) | [부분; S16] |
| P65 | Up control (AFJ 표기) | [부분; S12] |
| P66 | CM 자세를 상대 속도 벡터에 맞춰 유지(ballistic) | [부분; S16] |
| P67 | TO TERMINATE STEERING WHEN THE CM VELOCITY WRT EARTH = 1000 FT/SEC | [확인; S16] |
| P76 | Target dV (E-2448 목차) | [부분; S7] |

- 사령선 쪽 P32~P35(랑데부), P72~P75의 CSM 쪽 제목은 확인하지 못했다. [미확인]
- 진입 프로그램의 "P61 Entry Preparation / P62 CM/SM Separation... / P64 Post-0.05G / P66 Ballistic / P67 Final Phase" 같은 공식 영문 제목은 이번 조사에서 확인하지 못했다(소스는 기능 설명 줄만 줌). [미확인]
- 진입 순서: 일반 진입은 P64 다음 P67로 이어지고, Apollo 11 기록에는 P65에서 P67로 이어졌다는 설명도 있다("P66 will not be invoked and instead, P65 will lead directly to P67"). [확인; S12 D9a]

### 6.2 LM (Luminary 099 기준)

| 번호 | 이름/기능 | 확인 |
|---|---|---|
| P00 | LM CMC 대기 (`V50 N07` "PLEASE SELECT P00"으로 표시) | [부분; S17 P40] |
| P12 | POWERED ASCENT (ALSJ 해설 표기; 소스 파일 P12.agc) | [부분; S12, S17] |
| P20 | RENDEZVOUS NAVIGATION PROGRAM 20 (레이더 제어) | [확인; S17] |
| P22 | 소스에 "PROG22 = PROG20" 주석만 확인 | 제목 [미확인] |
| P30 | PROGRAM DESCRIPTION P30: "ACCEPT ASTRONAUT INPUTS OF TIG.DELV(LV)" | [확인; S17] |
| P32/P72 | COELLIPTIC SEQUENCE INITIATION (CSI) PROGRAMS (P32 AND P72), 능동 P32/수동 P72 | [확인; S17] |
| P33/P73 | CONSTANT DELTA HEIGHT (CDH) PROGRAMS | [확인; S17] |
| P34/P74 | TRANSFER PHASE INITITATION (TPI) PROGRAMS (원문 오탈자 그대로) | [확인; S17] |
| P35/P75 | RENDEZVOUS MID-COURSE MANEUVER PROGRAMS | [확인; S17] |
| P40 | TO COMPUTE A PREFERRED IMU ORIENTATION AND A PREFERRED VEHICLE ATTITUDE FOR A LM DPS THRUSTING MANEUVER | [확인; S17] |
| P41 | RCS 분사 | [부분; S17] |
| P42 | APS 분사 | [부분; S17] |
| P47 | 레이블 존재(승무원 "let's put P47 in") | [부분; S12] |
| P52 | ALIGNS THE IMU TO ONE OF THREE ORIENTATIONS SELECTED BY THE ASTRONAUT | [확인; S17] |
| P57 | LUNAR SURFACE IMU ALIGNMENT PROGRAM | [확인; S17] |
| P63 | THE LUNAR LANDING, BRAKING PHASE | [확인; S17] |
| P64 | 접근 단계(Visibility Phase로 표시) | 공식명 [미확인]; [부분; S2] |
| P65 | 소스 주석 "P65 GUIDANCE EQUATION" (수직 유도) | 공식명 [미확인] |
| P66 | rate-of-descent mode (Eyles 표기) | [부분; S2] |
| P67 | 수동 스로틀 이산 신호로 선택(소스 주석) | 공식명 [미확인] |
| P68 | LANDING CONFIRMATION | [확인; S17] |
| P70/P71 | 어보트 프로그램(P12.agc에 언급) | [부분; S17] |
| P76 | 파일 존재, CSI PAD 참조 | [부분; S12] |

- LM 알람 1202 사건은 P63 동안 발생하였다(3.6절). [확인; S2, 프로젝트 doc/00 4절 타임라인 기준 P63/P64 구간 S18]

---

## 7. 프로그램 알람 코드

Comanche 055 알람표 그대로이다. [확인; S5] 표의 `*`(abort-type, 별표 표시) 항목은 BAILOUT/POODOO이며 01201, 01202, 01203, 01210, 01211이 해당한다. [확인; S5]

| 코드(8진) | 원문 | 비고 |
|---|---|---|
| 00110 | NO MARK SINCE LAST MARK REJECT | SXTMARK |
| 00112 | MARK NOT BEING ACCEPTED | |
| 00113 | NO INBITS | |
| 00114 | MARK MADE BUT NOT DESIRED | |
| 00115 | OPTICS TORQUE REQUEST WITH SWITCH NOT AT CGC | |
| 00116 | OPTICS SWITCH ALTERED BEFORE 15 SEC ZERO TIME ELAPSED | |
| 00117 | OPTICS TORQUE REQUEST WITH OPTICS NOT AVAILABLE | |
| 00120 | OPTICS TORQUE REQUEST WITH OPTICS NOT ZEROED | |
| 00121 | CDUS NO GOOD AT TIME OF MARK | |
| 00122 | MARKING NOT CALLED FOR | |
| 00206 | ZERO ENCODE NOT ALLOWED WITH COARSE ALIGN + GIMBAL LOCK | |
| 00210 | IMU NOT OPERATING | |
| 00211 | COARSE ALIGN ERROR - DRIVE > 2 DEGREES | |
| 00212 | PIPA FAIL BUT PIPA IS NOT BEING USED | |
| 00213 | IMU NOT OPERATING WITH TURN-ON REQUEST | |
| 00214 | PROGRAM USING IMU WHEN TURNED OFF | |
| 00215 | PREFERRED ORIENTATION NOT SPECIFIED | |
| 00217 | BAD RETURN FROM STALL ROUTINES | |
| 00220 | IMU NOT ALIGNED - NO REFSMMAT | |
| 00401 | DESIRED GIMBAL ANGLES YIELD GIMBAL LOCK | |
| 00404 | TARGET OUT OF VIEW - TRUN ANGLE > 90 DEG | |
| 00405 | TWO STARS NOT AVAILABLE | P52, P54 |
| 00406 | REND NAVIGATION NOT OPERATING | |
| 00421 | W-MATRIX OVERFLOW | |
| 00600 | IMAGINARY ROOTS ON FIRST ITERATION | P32, P72 |
| 00605 | NUMBER OF ITERATIONS EXCEEDS LOOP MAXIMUM | P32, P72, P37 |
| 00611 | NO TIG FOR GIVEN ELEV ANGLE | |
| 00612 | STATE VECTOR IN WRONG SPHERE OF INFLUENCE | P37 |
| 00613 | REENTRY ANGLE OUT OF LIMITS | P37 |
| 00777 | PIPA FAIL CAUSED ISS WARNING | |
| 01102 | CMC SELF TEST ERROR | |
| 01105 | DOWNLINK TOO FAST | |
| 01106 | UPLINK TOO FAST | |
| 01107 | PHASE TABLE FAILURE. ASSUME ERASABLE MEMORY IS DESTROYED | |
| 01201 | EXECUTIVE OVERFLOW-NO VAC AREAS | 중단형(abort) |
| 01202 | EXECUTIVE OVERFLOW-NO CORE SETS | 중단형 |
| 01203 | WAITLIST OVERFLOW-TOO MANY TASKS | 중단형 |
| 01210 | TWO PROGRAMS USING DEVICE AT SAME TIME | 중단형 |
| 01211 | ILLEGAL INTERRUPT OF EXTENDED VERB | 중단형 |
| 01521 | P01 ILLEGALLY SELECTED | |

- 이 표는 사령선 Comanche 055 목록이다. LM Luminary 099 알람표는 이번에 읽지 못했다. [미확인] LM의 1201/1202는 Luminary 099 EXECUTIVE.agc에서 "NO CORE SETS AVAILABLE. OCT 1202", "NO VAC AREAS. OCT 1201"로 확인했다. [확인; S11]
- 00402, 31104, 21206, 20430 등 일부 코드는 Comanche 목록에 없으며 해당 표 대조는 하지 않았다. [미확인; 조사 노트 C]
- "알람 코드 앞자리(0 vs 1)의 규칙"(예: 00xxx 대 01xxx)은 읽은 출처에 설명이 없다. [미확인]
- 알람 확인 방법: `V05 N09 E`. 최대 3개(최근, 이전, 그 이전). [확인; S6, S18]

---

## 8. 이 프로젝트(시뮬레이터)가 구현한 범위와 실제와의 차이

근거: doc/00, doc/02, `src/Agc.Core/DskyNouns.cs`(S18). 구현 쪽 서술은 문서와 소스 주석에서 읽은 것이며 코드 전체를 검증한 것은 아니다.

| 기능 | 실제 | 시뮬레이터 | 비고 |
|---|---|---|---|
| 전체 구조 | 실제 AGC ROM이 AGC 명령어로 실행됨 | **시나리오 기반 시뮬레이션**: 미션 타임라인과 DSKY 로직(ROM 에뮬레이션은 M6 후보) | [확인; S18] |
| 하드웨어 | 16비트 워드, 코어 메모리, 인터럽트 | 없음. 순수 C# 로직 | [확인; S18] |
| Executive/Waitlist/재시작 | 실제 작업 스케줄링, core set, 재시작 보호 | 없음. 1201/1202는 타임라인의 **알람 이벤트**로만 발생 | [확인; S18] |
| 키 | 19개 | 19개(PC 키 매핑 제공) | [확인; S18, S9] |
| 표시 | 21자리, 7-세그먼트 | 5줄 7-세그먼트 UI | [확인; S18] |
| 십진/8진 | 십진 부호 있음, 8진 없음 | 동일(N09만 8진) | [확인; S18] |
| Verb | 수십 개 | V05(N09만), V06, V16, V21~V25, V33, V34, V35, V37만 | [확인; S18] |
| Noun | 수십 개 | N09, N36, N43, N63, N68. 정의는 Luminary 099(LM) 기준 | [확인; S18] 하나의 DSKY로 CM/LM 구분 없이 단순화 |
| V16 모니터 | 약 1초 갱신 | 1초 갱신, COMP ACTY 램프 | [확인; S18] |
| 데이터 로드 | 십진 부호 + 5자리, 많은 Noun 로드 가능 | N36(시계)만 로드 가능, 3개 레지스터 모두 로드 필요하므로 V25만 허용 | [확인; S18] |
| V35 | 모든 램프, 88, 약 5초, 종료 후 88 유지 | 구현. 단 STBY/RESTART도 점등 (doc/00은 Virtual AGC에서는 점등하지 않는다고 기록하고 검토 대상이라고 적음) | [부분; S18] |
| V37 | 프로그램 선택, 실제 프로그램이 동작 | 번호 검증 후 요청만 전달. 요청한 프로그램은 **다음 미션 단계 전환 시 덮어쓰임** | [확인; S18] |
| 프로그램 번호 | 승무원이 선택 | 미션 타임라인이 결정 | [확인; S18] |
| 프로그램의 표시 요청 | 프로그램이 V/N 깜박임으로 요청 | 기능 구현, 시나리오에서는 아직 미사용(M4) | [확인; S18] |
| KEY REL | 키보드 충돌 시 프로그램이 요청 | 구현(요청 충돌 시 램프, KEY REL 키로 해제) | [확인; S18] |
| PROG 램프 | 해석 충돌(5.7절) | 알람 해석(1201/1202, RSET으로 끔) | [부분; S18] |
| OPR ERR | 키보드 오류 | 규칙 위반 입력 시 점등, RSET으로 끔 | [확인; S18]. 실제의 RSET 동작은 [미확인] |
| RESTART 램프 | 재시작 시 점등 | 램프 테스트에서만 켜짐. 재시작 모델 없음(M4로 이월) | [확인; S18] |
| UPLINK ACTY, NO ATT, STBY, TEMP, GIMBAL LOCK, TRACKER, ALT, VEL | 해당 조건에서 켜짐 | 램프 테스트에서만 켜짐, 연동 안 됨 | [확인; S18] |
| ALT/VEL | LM에만 있음 | 한 가지 DSKY로 단순화(ALT/VEL 항상 표시), M4에서 CM/LM 전환 계획 | [확인; S18] |
| 알람 | 수십 종 | 하강 중 **5개(1202 x4, 1201 x1)**, 시각은 근사값 | [확인; S18] 시각은 S2/S12와 비교해 근사 |
| 미지원 입력 | 해당 Verb/Noun 존재 | N62, N60, N64, V82 등 미지원은 OPR ERR | [확인; S18] |
| 미션 데이터 | 실제 텔레메트리 | 공개 기록에 근거한 근사값, 선형 보간 | [확인; S18] |

---

## 9. 용어집

| 용어 | 설명 | 확인 |
|---|---|---|
| AGC | Apollo Guidance Computer | [확인; S1] |
| CMC / LGC | 사령선 컴퓨터 / 달착륙선 유도 컴퓨터(전사와 소스 용어) | [확인; S2, S16] |
| DSKY | Display and Keyboard. 승무원 인터페이스 | [확인; S9] |
| Verb / Noun | 동작 / 데이터 종류 | [확인; S4, S15] |
| Program (Pnn) / Major Mode | 주요 모드 | [확인; S5, S3] |
| Routine (Rnn) | 하위 루틴 | [확인; S5] |
| Executive | 협력형 우선순위 작업 스케줄러 | [확인; S1] |
| Waitlist | 타이머 구동 단기 작업 | [확인; S1] |
| Interpreter | 의사 명령어 가상 기계 | [확인; S1] |
| core set | Executive 작업용 레지스터 묶음 (개수는 출처 충돌) | [확인; S2, S11] |
| VAC | Vector Accumulator 영역 | [확인; S2] |
| BAILOUT | 자원 부족/오류 시 재시작을 일으키는 알람 처리 | [확인; S2] |
| restart / fresh start | 재시작(보호 지점부터) / 전체 초기화 | [확인; S5] |
| waypoint | 재시작 보호 위치 등록 | [확인; S2] |
| G&N, PGNCS | 유도/항법 시스템 | [확인; S8] |
| IMU | 관성 측정 장치 | [확인; S8] |
| PIPA | IMU 가속도계 | [부분; S8] |
| ICDU/CDU | 커플링 데이터 유닛(풀이 [미확인]) | [부분; S5] |
| SXT / SCT | 육분의 / 스캐닝 망원경 | [부분; S8] |
| AOT | LM 정렬 광학 망원경 | [부분; 이름은 S12 문맥, 풀이 [미확인]] |
| DAP | Digital Autopilot | [확인; S5, S16] |
| TVC | 추력 벡터 제어 | [확인; S16] |
| REFSMMAT | 안정판 기준 행렬(풀이 [미확인]) | [미확인] |
| SPS / DPS / APS | CSM 주엔진 / LM 하강엔진 / 상승엔진 | [확인; S2, S17] (SPS = Service Propulsion System; S7) |
| RCS | 반응 제어 시스템 | [확인; S17] |
| EMS | Entry Monitor System | [확인; S12] |
| AGS / DEDA | LM 백업 유도 / 그 키패드 | [부분; S12] |
| TIG | Time of Ignition | [확인; S6 (N33)] |
| MET / GET | 미션 경과 시간 / Ground Elapsed Time | [확인; 조사 노트 C 서두] |
| P52 | IMU Realign | [확인; S7, S17] |
| Gimbal lock | 짐벌 고착 | [확인; S3] |

---

## 10. 참고 자료

| 약호 | 자료 | URL |
|---|---|---|
| S1 | Wikipedia: Apollo Guidance Computer | https://en.wikipedia.org/wiki/Apollo_Guidance_Computer |
| S2 | Eyles, "Tales from the Lunar Module Guidance Computer" (2004) | https://klabs.org/history/apollo_11_alarms/eyles_2004/eyles_2004.htm |
| S3 | Computers in Spaceflight, Ch.2 | https://www.nasa.gov/wp-content/uploads/static/history/computers/Ch2-7.html |
| S4 | P. Parker, "The Apollo On-board Computers" (Apollo Flight Journal) | https://apollojournals.org/afj/compessay.html |
| S5 | Comanche055 ASSEMBLY_AND_OPERATION_INFORMATION.agc (Verb/Noun/알람 표) | https://raw.githubusercontent.com/chrislgarry/Apollo-11/master/Comanche055/ASSEMBLY_AND_OPERATION_INFORMATION.agc |
| S6 | Luminary099 PINBALL_NOUN_TABLES.agc | https://raw.githubusercontent.com/chrislgarry/Apollo-11/master/Luminary099/PINBALL_NOUN_TABLES.agc |
| S7 | MIT/IL E-2448 Rev 4 (Colossus 3/Luminary 1E) | https://ibiblio.org/apollo/Documents/j2-80-E-2448-REV4_text.pdf |
| S8 | Wikipedia: Apollo PGNCS | https://en.wikipedia.org/wiki/Apollo_PGNCS |
| S9 | NASSP DSKY (2차) | https://nassp.space/index.php/DSKY |
| S10 | Adler, 1201/1202 프로그램 알람 | https://www.nasa.gov/wp-content/uploads/static/history/alsj/a11/a11.1201-pa.html |
| S11 | Luminary099 EXECUTIVE.agc | https://raw.githubusercontent.com/chrislgarry/Apollo-11/master/Luminary099/EXECUTIVE.agc |
| S12 | Apollo Flight Journal / Lunar Surface Journal | https://apollojournals.org/afj/ap11fj/ ; https://apollojournals.org/alsj/a11/ (TEI: .../afj/ap11fj/21day6-tei.html, 진입: .../27day9-entry.html, 하강: .../alsj/a11/a11.landing.html) |
| S13 | Apollo 11 Mission Report | https://www.nasa.gov/wp-content/uploads/static/apollo50th/pdf/A11_MissionReport.pdf |
| S14 | Virtual AGC yaDSKY (프로젝트 doc/00 6.1절의 요약을 통해 인용) | https://www.ibiblio.org/apollo/yaDSKY.html |
| S15 | Discover Magazine (2차) | https://discovermagazine.com/the-sciences/how-verbs-and-nouns-got-apollo-to-the-moon |
| S16 | Comanche055 소스 (P30, P40-P47, P61-P67, REENTRY_CONTROL, CM_ENTRY_DIGITAL_AUTOPILOT, P11) | https://www.ibiblio.org/apollo/listings/Comanche055/ ; https://raw.githubusercontent.com/chrislgarry/Apollo-11/master/Comanche055/ |
| S17 | Luminary099 소스 (P12, P20-P25, P30_P37, P40-P47, P51-P53, THE_LUNAR_LANDING 등) | https://raw.githubusercontent.com/chrislgarry/Apollo-11/master/Luminary099/ |
| S18 | 이 프로젝트: doc/00_Development_Plan.md, doc/02_Simulator_User_Manual.md, src/Agc.Core/DskyNouns.cs | (로컬) |
| 기타 | Virtual AGC 전체 | https://www.ibiblio.org/apollo/ |

---

## 11. 미확인 항목과 주의

1. **Apollo 11 사령선의 실제 소프트웨어 이름**: 소스 헤더는 "Colossus 2A, AKA Comanche 055"이며 "Colossus 249"는 확인하지 못했다.
2. **core set 개수**: Eyles 8개 대 Adler/Luminary 주석 7개(+ 세트당 레지스터 11 대 12). 충돌 해소 못 함. VAC 영역 수 5개(Eyles) 외 다른 출처는 없다.
3. **PROG 램프**: S3(입력 대기) 대 S9/프로젝트(프로그램 알람). 1차 자료 미확인.
4. **ALT/VEL 램프의 정확한 의미**와 **램프 색상**.
5. **OPR ERR/RESTART 램프를 끄는 방법**의 실제 동작.
6. **Block II가 Apollo 11 하드웨어였다**는 직접 출처, 특수 레지스터, 인터럽트 우선순위, 메모리 뱅킹, 타이머 구성, 코어 로프 제조 방식.
7. **전원/무게/크기**는 Wikipedia 정보상자뿐이며 55 W와 "70 W에서 5~10 W 절감" 수치가 서로 다르다.
8. **Verb 62, 64, 74, 76**의 정의, V57 키 순서, V91의 정확한 정의.
9. **알람 코드 체계**(앞자리 의미)와 **LM Luminary 알람표 전체**.
10. **진입 프로그램 P61~P67의 공식 영문 제목**, P64/P65/P66/P67 LM 공식명.
11. **CM의 P20/P72~P75 제목**, Apollo 11 P15 사용 여부(증거는 사용하지 않았음 쪽).
12. **REFSMMAT, CDU, PIPA, AGS의 정식 풀이**, IMU 구성 세부(2차 자료만).
13. **Apollo 11 하강 중 PROG/RESTART 램프 표시 상태**와 알람 시각의 출처별 차이(102:38:22 / 102:38:26 / 102:39:02 등).
14. 키 배열(7열 x 3행)과 램프 색상은 프로젝트 문서가 스스로 "도면과 최종 대조가 필요한 가정"이라고 밝힌 부분이다.
15. 조사 도구가 웹 페이지를 요약하여 돌려준 경우가 있어, 인용문은 원문과의 최종 대조를 권장한다.
