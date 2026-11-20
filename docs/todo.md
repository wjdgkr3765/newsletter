# todo.md

진행 상황을 기록하는 문서다. 세션이 바뀌어도 이 파일만 읽으면 이어서 작업할 수 있어야 한다.

## 운영 규칙
- 세션을 시작하면 이 파일부터 읽는다.
- "진행 중"에는 한 번에 하나의 작업만 둔다.
- 작업 상태가 바뀔 때마다 즉시 갱신한다. 세션 끝에 몰아서 쓰지 않는다.
- 세션을 마치기 전에 "세션 인계"를 갱신한다.
- loop.md 4단계가 모두 승인되어야 "완료"로 옮긴다.
- 완료된 항목은 날짜와 함께 "완료"로 옮긴다. 20개가 넘으면 오래된 것부터 지운다.
- 사용자의 결정은 "메모"가 아니라 decision.md에 기록한다.

---

## 진행 중

<!-- 형식:
### [작업명]
- 목표: 
- 범위: (프로젝트 전체 / 추가 기능: overview.md 핵심 기능 중 해당 기능)
- 현재 단계: (loop.md 단계 번호. 예: 2. 와이어프레임)
- 단계 승인: 설계(승인 전 / 승인 완료, YYYY-MM-DD), 와이어프레임( ), 구현( ), 보안 체크( )
- 진행 상황: (현재 단계에서 끝낸 것과 남은 것. 예: 화면 5개 중 3개 시안 완료)
- 관련 파일: 
- 재시도 기록: (실패한 체크리스트 항목과 연속 실패 횟수)
- 메모: 
-->

### 날씨 정보 본문 (기상청 단기예보)
- 목표: 종류 "날씨 정보(지역)"(17곳)의 뉴스레터 본문을 기상청 단기예보 API로 만든다. 메일 위 전국 날씨 지도 이미지, 아래 그 지역의 오늘 최저·최고 기온과 06~21시 3시간 간격 날씨·기온·강수 확률 표.
- 범위: 추가 기능: overview.md 핵심 기능 "뉴스레터 자동 생성·발송"의 종류별 본문
- 현재 단계: 3. 구현
- 단계 승인: 설계(승인 완료 2026-11-20, D-009 변경도 승인 완료 2026-11-20), 와이어프레임(승인 완료, 2026-11-20), 구현(승인 전), 보안 체크( )
- 진행 상황: 구현 완료, 사용자 승인 전. 끝낸 것 — 윤곽 파일 NewsletterApp/Resources/korea_outline.json(22KB, 원본은 프로젝트 밖 C:\zzz\newsletter_data\ne_10m_admin_0_countries.geojson), Services/Weather(WeatherRegion, WeatherForecastClient, WeatherMapRenderer), Services/Contents/WeatherContentBuilder, MailService의 data: 이미지 → cid 변환, Program.cs 등록, appsettings.json Weather(BaseUrl, 지역 17곳, HttpClient 로그 Warning), .env.example에 Weather__ServiceKey, SkiaSharp 4.157.0. 빌드 경고 0, 테스트 136개 통과(신규 50, NewsletterApp.Tests/Services/WeatherContentBuilderTests.cs). 실제 기상청 데이터로 지도 PNG 확인(62KB, 17곳 3.8초). 남은 것 — (1) 실제 메일 1통 발송 확인(사용자 허락 필요, 아직 허락 안 받음): 관리자 뉴스레터 설정에서 종류 "날씨 정보(서울)" 추가 → 내 구독에서 켜기 → 발송 설정 상세 "지금 만들어 보내기" → Gmail에서 지도 이미지 보이는지, (2) 수신 내역 팝업에 지도·표가 보이는지(test.md 14·15), (3) test.md "결과 보고" 후 구현 승인 요청, (4) 승인되면 4. 보안 체크
- 관련 파일: docs/overview.md, docs/spec.md, docs/api.md(7번), docs/structure.md, docs/decision.md(D-007~D-010), docs/reference.md, docs/mockups/weather_mail.html, NewsletterApp/Views/Newsletter/History_screen.md, 위 진행 상황의 코드 파일
- 재시도 기록: 없음
- 메모: 공공데이터포털 페이지에서 확인한 것: 무료, 공공누리 제1유형(출처표시), 호출 주소 `https://apis.data.go.kr/1360000/VilageFcstInfoService_2.0`. 발표시각·카테고리 코드·최고/최저기온 제공 시간·서울 격자(60, 127)는 docs/reference의 활용가이드와 격자 엑셀로 확인함(2026-11-20). 가이드 서비스 URL은 apihub.kma.go.kr라 공공데이터포털 주소와 다름 → 인증키를 받은 뒤 구현 첫 단계에서 실제 응답으로 확인한다.

---

## 백로그

우선순위가 높은 순서로 적는다. overview.md 핵심 기능의 우선순위를 기준으로 적는다.

"진행 중"에서 옮긴 작업은 "진행 중" 형식의 내용을 그대로 유지한 채 옮긴다.

- (없음)

---

## 막힌 일

사용자의 결정, 외부 조건, 또는 반복 실패(loop.md "종료 조건") 때문에 진행할 수 없는 항목이다.

- (없음)

---

## 세션 인계

- **마지막 갱신**: 2026-11-20
- **현재 상태**: "날씨 정보 본문 (기상청 단기예보)" 작업이 loop.md 3. 구현 단계다. 설계·와이어프레임은 승인됨, 구현은 끝났고 사용자 승인 전. 자세한 내용은 위 "진행 중".
  - 방식: 종류 "날씨 정보(지역)"마다(지역 17곳) 메일 위 전국 날씨 지도 PNG + 아래 그 지역 06~21시 3시간 간격 표 + 공공누리 제1유형 출처 문구 (D-007~D-010)
  - 기존 프로젝트(2026-11-12 완료): 기능 12개, 화면 11개, API 31개, 보안 체크 통과. 테스트는 이번 작업으로 86 → 136개
- **다음에 할 일**: 사용자에게 실제 메일 1통 발송 확인을 할지 묻는다(허락 필요. 사용자가 직접 할 수도 있음) → 수신 내역 팝업 확인 → test.md "결과 보고"로 구현 승인 요청 → 4. 보안 체크(security.md 11번, 이번에 바뀐 외부 호출·인증키·HTML 인코딩 중심).
- **아직 커밋하지 않은 것**: 2026-11-17 커밋(fcf1ffc, launch.json app 설정) 이후 변경 전부 — 문서, 시안, 코드, 테스트, Resources, docs/reference의 활용가이드 docx·격자 xlsx, .claude/launch.json(mockups 설정을 python http.server로 바꿈).
- **주의 사항**:
  - 이 PC 환경(2026-11-17 새 PC): .NET SDK 10.0.404, SQL Server 2025 Express(localhost\SQLEXPRESS, Windows 인증, DB `Newsletter`), HTTPS 개발 인증서는 사용자가 신뢰 등록하지 않기로 함(브라우저 경고에서 계속 진행). 회사 보안 프로그램이 새 exe 실행을 검사한다.
  - .env 키 4개(ConnectionStrings__Default, Gmail__Address, Gmail__AppPassword, Weather__ServiceKey) 모두 값 있음. 값은 보지 않는다.
  - 기상청 API: 공공데이터포털 주소 apis.data.go.kr + serviceKey로 동작 확인(2026-11-20). 활용가이드에는 apihub.kma.go.kr로 적혀 있지만 쓰지 않는다. 일일 트래픽 10,000회.
  - 지도 글꼴은 Windows "맑은 고딕". 지도 이미지는 newsletters.content에 base64로 저장되어 날씨 뉴스레터 하나가 약 85KB.
  - 사용자는 파일 내려받기·삭제를 직접 하길 원한다. 외부 파일이 필요하면 주소·크기·둘 곳을 안내한다.
  - .claude/launch.json의 app-check 설정은 이전 PC 임시 폴더를 가리켜 동작하지 않는다(app, mockups는 동작).
  - 이전부터: 로그인 세션·인증 코드는 서버 메모리라 앱을 다시 시작하면 없어진다. 인증 코드 메일 발송 횟수 제한은 없다.

---

## 완료

- 2026-11-12 뉴스레터 서비스 (프로젝트 전체: 기능 12개, 화면 11개, API 31개)
