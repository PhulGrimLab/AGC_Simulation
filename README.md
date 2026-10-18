# AGC_Simulation
아폴로우주선의 AGC (Apollo Guidance Computer)를 흉내낸 시뮬레이션 프로그램 입니다.
왼쪽은 DSKY(Block II 실제 배치), 오른쪽은 Apollo 11의 발사~착수 미션 뷰이며 두 화면이 연동됩니다.

![실행 화면](doc/screenshots/03_descent_alarm.png)

## 빌드 / 실행 / 테스트
- 빌드: `dotnet build AgcSimulation.slnx` (.NET SDK 10)
- 실행: `dotnet run --project src/Agc.Wpf`
- 테스트: `dotnet test tests/Agc.Tests` (218개: 단위/연동/UI/실제 창)
- 테스트 시나리오·결과 엑셀 갱신: `python tools/make_test_report.py`

## 문서
| 문서 | 내용 |
|---|---|
| [doc/00_Development_Plan.md](doc/00_Development_Plan.md) | 개발 계획, 아키텍처, 마일스톤, 테스트 전략·목록, 참고 자료 |
| [doc/02_Simulator_User_Manual.md](doc/02_Simulator_User_Manual.md) | 이 프로그램 사용 설명서(조작, 명령, 실습) |
| [doc/10_AGC_Manual.md](doc/10_AGC_Manual.md) | AGC 매뉴얼: 하드웨어, 소프트웨어 구조, DSKY 사용법, 프로그램/알람 |
| [doc/11_Apollo_Spacecraft_And_Operations.md](doc/11_Apollo_Spacecraft_And_Operations.md) | 아폴로 우주선(새턴 V, CSM, LM)의 기능과 운용, G&N 시스템 |
| [doc/12_Mission_Phase_AGC_Guide.md](doc/12_Mission_Phase_AGC_Guide.md) | 운용 단계별 AGC의 역할과 승무원 조작(DSKY 시퀀스), 따라 해 보기 |
| [doc/Test_Scenarios.xlsx](doc/Test_Scenarios.xlsx) | 기능별 UI 테스트 시나리오와 최신 결과, 발견된 결함 |

설명서 10~12의 모든 기술 내용에는 출처와 확인 수준([확인]/[부분]/[미확인])을 표시했다.
