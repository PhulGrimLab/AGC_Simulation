"""개발 계획 문서(00)의 테스트 표를 현재 개수/새 클래스에 맞게 갱신하는 일회성 보조 스크립트."""
import pathlib
import re

p = pathlib.Path(__file__).resolve().parent.parent / "doc" / "00_Development_Plan.md"
s = p.read_text(encoding="utf-8")

s = s.replace("(M1+M2+코드리뷰 수정+UI 확장: 총 218개, 모두 통과)", "(M1~M4 일부, 코드리뷰 수정, UI·장면·가이드 확장: 총 300개, 모두 통과)")
for cls, n in {
    "DskyCommandTests": 22, "DskyNounFormatTests": 16, "DskyProgramAndDisplayRequestTests": 22,
    "Apollo11MissionDataTests": 12, "MissionTimelineTests": 17,
}.items():
    s, count = re.subn(rf"(\| `{cls}` \| )\d+( \|)", rf"\g<1>{n}\g<2>", s)
    assert count == 1, cls

new_rows = """| 키 표기법 | `DskyKeyNotationTests` | 5 | 가이드의 키 문자열(V37E63E …) 파싱·검증·설명("VERB 1 6 NOUN 6 3 ENTR"), 컨트롤러 구동 |
| 미션↔DSKY 연결(자동 표시) | `AgcMissionLinkTests` | 8 | 발사 P11이 V16 N62를 스스로 표시, PDI에서 N63, 같은 단계 안의 Seek는 재표시 없음, 재생으로 점화를 지날 때 표시/KEY REL, 알람 전달, 도킹 후 사령선 노운 |
| 단계 가이드 데이터 | `PhaseGuideTests` | 7 | 27개 키프레임마다 가이드 1개, 설명·역할·조작 순서·참조, 단계마다 조작/목적/영향, 키 표기 유효성, **실행 가능(Runnable) 단계가 해당 단계에서 실제로 오류 없이 실행됨**, 하강 가이드의 N63/N68 |
| 우주선 전환 UI | `VehicleUiTests` | 10 | 사령선 DSKY의 ALT/VEL 칸 숨김, LM 분리/도킹 경계의 캡션·램프 전환, 모든 단계의 컴퓨터, 착륙선 램프 테스트 13개, 프로그램 자동 표시와 KEY REL 보호, N62 정의 차이(속도·고도 vs 점화 카운트다운), N63/N68은 착륙선 전용, 재생 중 컴퓨터 전환 2회 |
| 천체 렌더러 | `PlanetRendererTests` | 10 | NASA 텍스처 로드, 프레임/동결, 태양 쪽 밝기·바다 색, 지구 대기 글로/모서리 투명, 달 무채색·대기 없음, 면 전환, 구름 이동, 결정성, 렌더 속도, PNG 저장 |
| 미션 장면 | `SceneRenderingTests` | 14 | 키프레임별 장면 종류(11가지), 연소 구간의 엔진, 여정 비율, 발사 캡션(S-IC/S-II/S-IVB), **27개 단계 렌더링 + 스크린샷**, 발사 불꽃·푸른 하늘, 푸른 지구, 지구·달 동시 표시, 하강 지표·금색 착륙선, EVA 이후 성조기, 재진입 플라스마 강도, 낙하산·바다, 결정성, 장면당 렌더 시간 |
| 단계 가이드 패널 UI | `GuidePanelUiTests` | 9 | 모든 단계의 미션 설명·AGC 역할·조작 단계, 조작/목적/영향 표시, [실행] 버튼 수와 미지원 표시, **모든 [실행] 버튼이 DSKY에서 오류 없이 동작**, 단계가 바뀔 때만 다시 그림, 하강 가이드의 알람 설명, 레이아웃(1600×980/1100×700/1920×1080), 스크롤 |
"""
marker = "| 메인 창 UI | `MainWindowUiTests` | 3 |"
assert marker in s
s = s.replace(marker, new_rows + marker, 1)
p.write_text(s, encoding="utf-8")
print("plan tests updated")
