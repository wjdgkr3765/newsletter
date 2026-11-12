# structure.md

해당 파일은 폴더 구조와 간단한 설명을 정리한 파일이다.

---

## 규칙

1. 이 파일에서 수정할 수 있는 곳은 "변경 가능" 목록뿐이다. 이 규칙과 "변경 불가능" 목록을 포함한 나머지 부분은 수정하지 않는다.
2. "변경 가능" 목록
   - 목록의 항목(경로, 파일명, 설명)은 추가, 수정, 삭제할 수 있다.
   - 폴더나 파일을 만들거나 옮기거나 지우면, 실제 구조와 맞도록 이 목록을 바로 갱신한다. `프로젝트/backend/`와 `프로젝트/frontend/` 하위 구조가 바뀔 때도 같다.
3. "변경 불가능" 목록
   - 목록에 적힌 경로와 파일명은 수정하지 않는다.
   - 목록에 항목을 추가하거나 삭제하지 않는다.
   - 목록에 적힌 실제 파일을 옮기거나 이름을 바꾸거나 삭제하지 않는다. 파일 내용은 각 문서의 규칙에 따라 수정할 수 있다.

---

## 1. 폴더 구조

### 변경 가능
|폴더구조|설명|
|---|---|
|프로젝트/.gitignore|git에서 제외할 파일 목록. `bin/`, `obj/`, `.vs/`, `*.user`, `TestResults/`, `.env`를 제외한다 (git.md 2번)|
|프로젝트/README.md|프로젝트 소개|
|프로젝트/NewsletterApp.slnx|솔루션 파일 (웹 프로젝트 + 테스트 프로젝트)|
|프로젝트/global.json|`dotnet test`가 Microsoft Testing Platform을 쓰도록 지정 (.NET 10 + xUnit v3)|
|프로젝트/dotnet-tools.json|로컬 도구 목록 (dotnet-ef 10.0.15). `dotnet tool restore`로 설치|
|프로젝트/docs/reference.md|reference 폴더 참고자료 목록 (파일명, 설명, 관련 작업). AI가 폴더를 볼 때 갱신|
|프로젝트/docs/mockups/|화면 시안 HTML. 서버 없이 바로 열어 보는 파일 (loop.md 2번 와이어프레임). 화면마다 `{화면파일명 snake_case}.html` (11개)|
|프로젝트/docs/mockups/mockup.css|시안 공통 스타일 (실제 화면 site.css가 될 레이아웃 값 + 시안 전용 상태 막대·팝업 표시)|
|프로젝트/docs/mockups/mockup.js|시안 공통 스크립트 (상태 전환 막대, 권한별 왼쪽 메뉴 그리기). 실제 기능 없음|
|프로젝트/.claude/launch.json|시안·화면 확인용 실행 설정 (Claude 미리보기 전용, 작업 PC의 임시 폴더 스크립트를 가리킨다)|
|프로젝트/NewsletterApp/|ASP.NET Core MVC 웹 프로젝트 (.NET 10)|
|프로젝트/NewsletterApp/NewsletterApp.csproj|웹 프로젝트 파일|
|프로젝트/NewsletterApp/.env|비밀 정보 (DB 접속 문자열, Gmail 주소, Gmail 앱 비밀번호). 사용자가 직접 만들고 값을 넣는다. git 제외|
|프로젝트/NewsletterApp/.env.example|`.env`에 필요한 변수 이름만 적은 파일 (값 비움). git에 올린다|
|프로젝트/NewsletterApp/Program.cs|서비스 등록(DB, 쿠키 인증, Antiforgery, Data Protection, IMemoryCache, MailKit, BackgroundService)과 미들웨어 설정|
|프로젝트/NewsletterApp/appsettings.json|비밀이 아닌 설정 (`App:BaseUrl`, 로그 수준). 비밀 정보는 넣지 않는다|
|프로젝트/NewsletterApp/appsettings.Development.json|개발 환경 설정|
|프로젝트/NewsletterApp/Properties/launchSettings.json|로컬 실행 주소 (HTTPS)|
|프로젝트/NewsletterApp/Controllers/AccountController.cs|화면: `/login`, `/register`, `/password-reset` (로그인했으면 `/newsletters`로)|
|프로젝트/NewsletterApp/Controllers/UnsubscribeController.cs|화면: `/unsubscribe?token=` (토큰을 풀어 종류 이름 표시)|
|프로젝트/NewsletterApp/Controllers/NewsletterController.cs|화면: `/`, `/newsletters`|
|프로젝트/NewsletterApp/Controllers/MyController.cs|화면: `/my/subscriptions`, `/my/profile`|
|프로젝트/NewsletterApp/Controllers/AdminController.cs|화면: `/admin/users`, `/admin/subscriptions`, `/admin/gubuns`, `/admin/gubuns/{id}`|
|프로젝트/NewsletterApp/Controllers/ClaimsPrincipalExtensions.cs|로그인 세션 클레임(회원 id, 아이디, 이름, 관리자 Role) 만들기·읽기|
|프로젝트/NewsletterApp/Controllers/Api/ApiControllerBase.cs|API 컨트롤러 공통: 서비스 결과를 공통 응답 형식으로 바꾼다|
|프로젝트/NewsletterApp/Controllers/Api/AntiforgeryJsonResultFilter.cs|CSRF 토큰 실패를 400 INVALID_INPUT 공통 응답으로 바꾸는 필터|
|프로젝트/NewsletterApp/Controllers/Api/AuthApiController.cs|API: `/api/auth/*` (api.md 6.1)|
|프로젝트/NewsletterApp/Controllers/Api/MeApiController.cs|API: `/api/me/*` (api.md 6.2)|
|프로젝트/NewsletterApp/Controllers/Api/NewsletterApiController.cs|API: `/api/newsletters/*` (api.md 6.3)|
|프로젝트/NewsletterApp/Controllers/Api/UnsubscribeApiController.cs|API: `/api/unsubscribe` (api.md 6.4)|
|프로젝트/NewsletterApp/Controllers/Api/AdminUserApiController.cs|API: `/api/admin/users/*` (api.md 6.5)|
|프로젝트/NewsletterApp/Controllers/Api/AdminSubscriptionApiController.cs|API: `/api/admin/subscriptions` (api.md 6.6)|
|프로젝트/NewsletterApp/Controllers/Api/AdminGubunApiController.cs|API: `/api/admin/gubuns/*` (api.md 6.7, 6.8)|
|프로젝트/NewsletterApp/Data/AppDbContext.cs|EF Core DbContext, 테이블·컬럼 매핑 (database.md 4번)|
|프로젝트/NewsletterApp/Data/Entities/User.cs|`users` 엔티티|
|프로젝트/NewsletterApp/Data/Entities/Gubun.cs|`gubuns` 엔티티|
|프로젝트/NewsletterApp/Data/Entities/NewsletterConfig.cs|`newsletter_configs` 엔티티|
|프로젝트/NewsletterApp/Data/Entities/Subscription.cs|`subscriptions` 엔티티|
|프로젝트/NewsletterApp/Data/Entities/Newsletter.cs|`newsletters` 엔티티|
|프로젝트/NewsletterApp/Data/Entities/History.cs|`historys` 엔티티|
|프로젝트/NewsletterApp/Migrations/|EF Core 마이그레이션 (자동 생성)|
|프로젝트/NewsletterApp/Models/ApiResponse.cs|공통 응답 형식 `{ data }`, `{ error: { code, message } }`, 목록 응답 형식, 서비스 결과(`ServiceResult`)|
|프로젝트/NewsletterApp/Models/ErrorCodes.cs|오류 코드와 화면 메시지 (api.md 4번)|
|프로젝트/NewsletterApp/Models/Requests/ApiRequests.cs|API 요청 DTO|
|프로젝트/NewsletterApp/Models/Requests/InputRules.cs|입력값 규칙 검증 (api.md 6번 "입력값 규칙")|
|프로젝트/NewsletterApp/Models/Responses/ApiResponses.cs|API 응답 DTO|
|프로젝트/NewsletterApp/Services/AuthService.cs|회원가입, 로그인(실패 횟수·잠금), 비밀번호 찾기|
|프로젝트/NewsletterApp/Services/VerificationCodeService.cs|인증 코드 발급·확인·인증 완료 상태 (IMemoryCache)|
|프로젝트/NewsletterApp/Services/LoginAttemptService.cs|로그인 5번 연속 실패 시 10분 잠금 (IMemoryCache)|
|프로젝트/NewsletterApp/Services/SendLockService.cs|종류별 발송 잠금 (동시 발송 방지)|
|프로젝트/NewsletterApp/Services/PasswordHasher.cs|BCrypt 해시와 확인|
|프로젝트/NewsletterApp/Services/MemoryTicketStore.cs|쿠키 인증 세션 저장소 (`ITicketStore`, IMemoryCache, 60분 Sliding)|
|프로젝트/NewsletterApp/Services/MeService.cs|내 정보 수정, 비밀번호 변경, 탈퇴, 내 구독|
|프로젝트/NewsletterApp/Services/AdminUserService.cs|회원 목록, 정지·정지 해제|
|프로젝트/NewsletterApp/Services/SubscriptionService.cs|구독·해지 (내 구독, 구독 관리, 수신 거부 공통)|
|프로젝트/NewsletterApp/Services/GubunService.cs|종류 목록·추가, 발송 설정 저장|
|프로젝트/NewsletterApp/Services/NewsletterQueryService.cs|수신 내역 목록, 본문 조회 (권한별)|
|프로젝트/NewsletterApp/Services/NewsletterSendService.cs|뉴스레터 만들어 보내기, 다시 보내기, 종류별 발송 잠금 (api.md 7번)|
|프로젝트/NewsletterApp/Services/NewsletterSchedulerService.cs|자동 발송 BackgroundService (매 분 확인)|
|프로젝트/NewsletterApp/Services/MailService.cs|MailKit Gmail SMTP 발송 (뉴스레터, 인증 코드)|
|프로젝트/NewsletterApp/Services/UnsubscribeTokenService.cs|수신 거부 토큰 만들기·풀기 (Data Protection)|
|프로젝트/NewsletterApp/Services/KstClock.cs|현재 UTC·한국 시각·한국 날짜 (테스트에서 바꿔 끼울 수 있게 인터페이스 `IClock`과 함께)|
|프로젝트/NewsletterApp/Services/Contents/INewsletterContentBuilder.cs|종류별 본문 생성기 인터페이스 (외부 데이터 실패 시 실패 결과 반환)|
|프로젝트/NewsletterApp/Services/Contents/DefaultContentBuilder.cs|기본 본문 생성기 (종류 이름과 날짜)|
|프로젝트/NewsletterApp/Services/Contents/ContentBuilderResolver.cs|종류 이름으로 본문 생성기를 고른다. 없으면 기본 본문 생성기|
|프로젝트/NewsletterApp/Views/_ViewImports.cshtml|Razor 공통 using, 태그 헬퍼|
|프로젝트/NewsletterApp/Views/_ViewStart.cshtml|기본 레이아웃 지정|
|프로젝트/NewsletterApp/Views/Shared/_Layout.cshtml|로그인 후 공통 레이아웃: 왼쪽 메뉴(권한별), 본문 최대 너비 960px, CSRF 메타 태그|
|프로젝트/NewsletterApp/Views/Shared/_GuestLayout.cshtml|비회원 레이아웃: 메뉴 없음, 가운데 카드, CSRF 메타 태그|
|프로젝트/NewsletterApp/Views/Shared/_ConfirmModal.cshtml|공통 확인 팝업|
|프로젝트/NewsletterApp/Views/Account/Login.cshtml|로그인 화면|
|프로젝트/NewsletterApp/Views/Account/Login_screen.md|로그인 화면 설계|
|프로젝트/NewsletterApp/Views/Account/Register.cshtml|회원가입 화면|
|프로젝트/NewsletterApp/Views/Account/Register_screen.md|회원가입 화면 설계|
|프로젝트/NewsletterApp/Views/Account/PasswordReset.cshtml|비밀번호 찾기 화면|
|프로젝트/NewsletterApp/Views/Account/PasswordReset_screen.md|비밀번호 찾기 화면 설계|
|프로젝트/NewsletterApp/Views/Unsubscribe/Unsubscribe.cshtml|수신 거부 화면|
|프로젝트/NewsletterApp/Views/Unsubscribe/Unsubscribe_screen.md|수신 거부 화면 설계|
|프로젝트/NewsletterApp/Views/Newsletter/History.cshtml|뉴스레터 수신 내역 화면|
|프로젝트/NewsletterApp/Views/Newsletter/History_screen.md|뉴스레터 수신 내역 화면 설계|
|프로젝트/NewsletterApp/Views/My/MySubscriptions.cshtml|내 구독 화면|
|프로젝트/NewsletterApp/Views/My/MySubscriptions_screen.md|내 구독 화면 설계|
|프로젝트/NewsletterApp/Views/My/Profile.cshtml|내 정보 화면|
|프로젝트/NewsletterApp/Views/My/Profile_screen.md|내 정보 화면 설계|
|프로젝트/NewsletterApp/Views/Admin/Users.cshtml|회원 관리 화면|
|프로젝트/NewsletterApp/Views/Admin/Users_screen.md|회원 관리 화면 설계|
|프로젝트/NewsletterApp/Views/Admin/AdminSubscriptions.cshtml|구독 화면|
|프로젝트/NewsletterApp/Views/Admin/AdminSubscriptions_screen.md|구독 화면 설계|
|프로젝트/NewsletterApp/Views/Admin/Gubuns.cshtml|뉴스레터 설정 화면|
|프로젝트/NewsletterApp/Views/Admin/Gubuns_screen.md|뉴스레터 설정 화면 설계|
|프로젝트/NewsletterApp/Views/Admin/GubunDetail.cshtml|발송 설정 상세 화면|
|프로젝트/NewsletterApp/Views/Admin/GubunDetail_screen.md|발송 설정 상세 화면 설계|
|프로젝트/NewsletterApp/wwwroot/css/site.css|공통 스타일 (레이아웃, 메뉴)|
|프로젝트/NewsletterApp/wwwroot/js/common.js|공통 JS: fetch 래퍼(CSRF 헤더, 401이면 `/login`), 한국 시간 표시, 페이지 버튼, 확인 팝업|
|프로젝트/NewsletterApp/wwwroot/js/validation.js|공통 입력값 검사 (아이디, 비밀번호, 이메일, 이름, 코드)|
|프로젝트/NewsletterApp/wwwroot/js/login.js|로그인 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/register.js|회원가입 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/password_reset.js|비밀번호 찾기 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/unsubscribe.js|수신 거부 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/history.js|뉴스레터 수신 내역 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/my_subscriptions.js|내 구독 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/profile.js|내 정보 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/users.js|회원 관리 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/admin_subscriptions.js|구독 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/gubuns.js|뉴스레터 설정 화면 JS|
|프로젝트/NewsletterApp/wwwroot/js/gubun_detail.js|발송 설정 상세 화면 JS|
|프로젝트/NewsletterApp/wwwroot/lib/bootstrap/|부트스트랩 5.3.3 (MVC 템플릿 포함)|
|프로젝트/NewsletterApp/wwwroot/favicon.ico|사이트 아이콘 (MVC 템플릿 기본)|
|프로젝트/NewsletterApp.Tests/|xUnit v3 테스트 프로젝트|
|프로젝트/NewsletterApp.Tests/NewsletterApp.Tests.csproj|테스트 프로젝트 파일|
|프로젝트/NewsletterApp.Tests/TestSupport.cs|테스트 공통: SQLite 메모리 DB, 가짜 시계·메일·해시, 테스트 데이터 만들기|
|프로젝트/NewsletterApp.Tests/Services/SecurityServiceTests.cs|인증 코드, 로그인 잠금, BCrypt, 수신 거부 토큰 테스트|
|프로젝트/NewsletterApp.Tests/Services/AccountServiceTests.cs|회원가입, 로그인, 비밀번호 찾기, 내 정보, 탈퇴 테스트|
|프로젝트/NewsletterApp.Tests/Services/NewsletterServiceTests.cs|발송, 다시 보내기, 수신 내역, 구독, 수신 거부, 종류 설정, 회원 관리 테스트|
|프로젝트/NewsletterApp.Tests/Validation/InputRulesTests.cs|입력값 규칙 동등 분할·경계값 테스트|

### 변경 불가능
- 프로젝트/docs/coding_rule.md
- 프로젝트/docs/database.md
- 프로젝트/docs/screen.md
- 프로젝트/docs/guide.md
- 프로젝트/docs/loop.md
- 프로젝트/docs/spec.md
- 프로젝트/docs/structure.md
- 프로젝트/docs/test.md
- 프로젝트/docs/todo.md
- 프로젝트/docs/overview.md
- 프로젝트/docs/api.md
- 프로젝트/docs/decision.md
- 프로젝트/docs/security.md
- 프로젝트/docs/git.md
- 프로젝트/docs/reference/
- 프로젝트/CLAUDE.md
