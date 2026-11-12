# spec.md

기술 스택, 의존성을 정리한다.

---

## 1. 스택
|구분|선택|버전|
|---|---|---|
|프레임워크|ASP.NET Core MVC|.NET 10 (LTS)|
|언어|C#, HTML, JavaScript|C# 14, ES2022|
|화면|Razor 뷰 + JS `fetch`로 JSON API 호출| |
|스타일링|부트스트랩|5.3.3 (MVC 템플릿의 `wwwroot/lib/bootstrap`)|
|DB|SQL Server Developer 에디션|2022 이상|
|ORM|EF Core (SQL Server 공급자)|10.0.15|
|인증|ASP.NET Core 쿠키 인증 (Identity 미사용), 세션은 서버 메모리(`ITicketStore` + IMemoryCache)| .NET 10 기본 포함|
|비밀번호 해시|BCrypt (BCrypt.Net-Next)|4.2.0 (work factor 11)|
|메일 발송|Gmail SMTP (MailKit)|5.0.0|
|자동 발송|ASP.NET Core BackgroundService| .NET 10 기본 포함|
|인증 코드·로그인 실패 횟수 저장|ASP.NET Core IMemoryCache (앱 메모리)| .NET 10 기본 포함|
|수신 거부 링크 토큰|ASP.NET Core Data Protection| .NET 10 기본 포함|
|CSRF 방지|ASP.NET Core Antiforgery (요청 헤더 토큰)| .NET 10 기본 포함|
|비밀 정보 관리|`.env` 파일 (DB 접속 정보, Gmail 주소, Gmail 앱 비밀번호), DotNetEnv로 읽어 환경 변수로 등록| |
|테스트 도구|xUnit v3 (Microsoft Testing Platform, `global.json`)|4.0.3|
|개발 도구|Visual Studio|2026|
|실행 환경|로컬 PC (Windows)| |

- 2026-11-12 설치하면서 `dotnet list package --vulnerable --include-transitive`로 확인했고 취약한 패키지는 없었다. 버전을 바꾸면 다시 확인하고 아래 표를 고친다.
- 한국 시간 변환은 `TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul")`을 쓴다. (.NET 6 이상은 Windows에서도 IANA ID를 지원한다)

### 설정 값
| 설정 키 (코드) | `.env` 변수 이름 | 저장 위치 | 설명 |
|---|---|---|---|
| `ConnectionStrings:Default` | `ConnectionStrings__Default` | `NewsletterApp/.env` | SQL Server 접속 문자열 |
| `Gmail:Address` | `Gmail__Address` | `NewsletterApp/.env` | 보내는 Gmail 주소 (보내는 사람 이메일) |
| `Gmail:AppPassword` | `Gmail__AppPassword` | `NewsletterApp/.env` | Gmail 앱 비밀번호 |
| `App:BaseUrl` | 없음 | appsettings.json | 메일의 수신 거부 링크에 쓰는 사이트 주소 (`https://localhost:7105`) |
| `Kestrel:Endpoints` | 없음 | appsettings.json | 앱이 여는 주소 `https://localhost:7105`, `http://localhost:5115`. 실행 방법(VS, dotnet run)과 관계없이 이 주소로 뜬다 |

- `Program.cs` 맨 앞에서 `DotNetEnv.Env.TraversePath().Load()`로 `.env`를 읽어 환경 변수로 등록한다. ASP.NET Core는 환경 변수 이름의 `__`를 설정 키의 `:`로 읽는다.
- `.env`는 `.gitignore`로 커밋하지 않는다. 변수 이름만 적고 값은 비운 `.env.example`을 커밋한다. (security.md 1번)
- `.env` 값은 사용자가 직접 넣는다. AI는 값을 쓰거나 출력하지 않고, 키 이름만 확인한다.

---

## 2. 의존성
|이름|버전|용도|
|---|---|---|
|Microsoft.EntityFrameworkCore.SqlServer|10.0.15|EF Core SQL Server 공급자|
|Microsoft.EntityFrameworkCore.Design|10.0.15|EF Core 마이그레이션 도구|
|BCrypt.Net-Next|4.2.0|비밀번호 해시|
|DotNetEnv|3.2.0|`.env` 파일을 읽어 환경 변수로 등록|
|MailKit|5.0.0|Gmail SMTP 발송|
|xunit.v3|4.0.3|테스트|
|xunit.runner.visualstudio|3.1.4|Visual Studio 테스트 실행|
|Microsoft.NET.Test.Sdk|17.14.1|테스트 실행|
|Microsoft.EntityFrameworkCore.Sqlite|10.0.15|테스트용 DB (SQLite 메모리 모드, UNIQUE 제약까지 확인)|
|bootstrap|5.3.3 (.NET 10 MVC 템플릿 포함 버전)|화면 스타일|
|dotnet-ef (로컬 도구, `dotnet-tools.json`)|10.0.15|마이그레이션 만들기 (`dotnet ef migrations add`)|

---
