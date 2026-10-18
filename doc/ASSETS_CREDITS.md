# 이미지 자산 출처

오른쪽 미션 화면의 지구·달 구체는 아래 NASA 공개 텍스처를 사용한다. 나머지(우주선, 별, 배경, 지형)는 코드로 직접 그린 벡터 그림이다.

| 파일 | 내용 | 출처 | 라이선스 |
|---|---|---|---|
| `src/Agc.Wpf/Assets/Textures/earth_2048.jpg` | 지구 표면 텍스처 2048×1024 (Blue Marble Next Generation, land_shallow_topo) | NASA Earth Observatory / Visible Earth — https://visibleearth.nasa.gov/images/57752/blue-marble-land-surface-shallow-water-and-shaded-topography | NASA 이미지: 별도 표시가 없는 한 저작권 제한 없음(공공 영역). 출처 표기: "NASA Earth Observatory" |
| `src/Agc.Wpf/Assets/Textures/moon_1024.jpg` | 달 표면 컬러 텍스처 1024×512 (LROC 컬러 지도) | NASA's Scientific Visualization Studio, CGI Moon Kit (SVS 4720) — https://svs.gsfc.nasa.gov/4720 | NASA 이미지: 공공 영역. 출처 표기: "NASA/GSFC/Arizona State University" |

- NASA 미디어 사용 지침: https://www.nasa.gov/nasa-brand-center/images-and-media/
- 이 텍스처는 구체 렌더링의 원본일 뿐이며, 화면의 음영(태양 방향), 대기광, 구름, 별은 프로그램이 계산해서 그린다.
- 텍스처를 교체하려면 같은 이름의 파일을 바꿔 넣고 다시 빌드한다(임베디드 리소스).
