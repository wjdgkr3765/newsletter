# api.md

API의 기준 문서다. API를 추가하거나 바꾸는 작업에서는 이 문서도 함께 갱신한다.

이 문서와 실제 API가 다르면 사용자에게 알린다.

---

## 1. 개요

- **기본 경로**: /api
- **데이터 형식**: JSON (UTF-8)
- **인증 방식**: ASP.NET Core 쿠키 인증. 세션 내용은 서버 메모리에 두고 쿠키에는 세션 키만 둔다(`ITicketStore`). 60분 유지, 사용하면 연장(Sliding). 쿠키는 `HttpOnly`, `SameSite=Lax`, HTTPS에서 `Secure`.
- **CSRF**: 모든 POST 요청은 요청 헤더 `RequestVerificationToken`에 Antiforgery 토큰을 넣는다. 토큰은 레이아웃의 `<meta name="csrf-token">`에서 읽는다. 토큰이 없거나 틀리면 400 `INVALID_INPUT`.
- **날짜 형식**: 시각은 ISO 8601, UTC (`2026-01-01T00:00:00Z`). 화면에서 한국 시간으로 바꿔 보여준다. 발송 시간은 한국 시간 `"HH:mm"`, 발송일은 한국 날짜 `"YYYY-MM-DD"`.
- **로그인이 필요한 API**에 로그인하지 않고 요청하면 로그인 화면으로 보내지 않고 401 `UNAUTHORIZED`를 JSON으로 응답한다. 관리자 API에 일반 사용자가 요청하면 403 `FORBIDDEN`.
- **문자열 입력**은 서버에서 앞뒤 공백을 빼고 검증한다. 이메일은 소문자로 바꾼다. 비밀번호와 인증 코드는 공백을 빼지 않는다.
- **목록 응답**은 아래 형식이다. `page`는 1부터, 범위를 벗어나면 400 `INVALID_INPUT`.
  ```json
  { "data": { "items": [], "page": 1, "pageSize": 10, "totalCount": 0, "totalPages": 0 } }
  ```

---

## 2. 네이밍 규칙

| 대상 | 규칙 | 예 |
|---|---|---|
| 경로 | 소문자 kebab-case, 복수형 명사 | `/api/users` |
| 경로 변수 | `{id}` | `/api/users/{id}` |
| 요청/응답 필드 | camelCase | `userName`, `createdAt` |
| 쿼리 파라미터 | camelCase | `?pageSize=20` |

### 메서드
| 메서드 | 용도 |
|---|---|
| GET | 조회 |
| POST | 생성/수정/삭제 |

---

## 3. 공통 응답 형식

### 성공
```json
{
  "data": {}
}
```

### 실패
```json
{
  "error": {
    "code": "USER_NOT_FOUND",
    "message": "사용자를 찾을 수 없습니다."
  }
}
```

---

## 4. 오류 코드

| HTTP 상태 | 코드 | 설명 |
|---|---|---|
| 400 | INVALID_INPUT | 입력값이 올바르지 않음 |
| 401 | UNAUTHORIZED | 로그인이 필요함 |
| 403 | FORBIDDEN | 권한이 없음 |
| 404 | NOT_FOUND | 대상이 없음 |
| 500 | INTERNAL_ERROR | 서버 오류 |

<!-- 프로젝트 전용 오류 코드는 이 아래에 추가한다 -->
| HTTP 상태 | 코드 | 화면 메시지 |
|---|---|---|
| 400 | LOGIN_FAILED | 아이디 또는 비밀번호가 맞지 않습니다. |
| 429 | LOGIN_LOCKED | 로그인 시도가 많아 10분 뒤에 다시 시도해 주세요. |
| 409 | DUPLICATE_LOGIN_ID | 이미 사용 중인 아이디입니다. |
| 409 | DUPLICATE_EMAIL | 이미 사용 중인 이메일입니다. |
| 400 | SAME_EMAIL | 지금 쓰고 있는 이메일입니다. |
| 400 | CODE_INVALID | 인증 코드가 맞지 않습니다. (남은 횟수 n번) |
| 400 | CODE_EXPIRED | 인증 코드가 만료되었거나 더 이상 쓸 수 없습니다. 다시 받아 주세요. |
| 400 | EMAIL_NOT_VERIFIED | 이메일 인증을 먼저 완료해 주세요. |
| 400 | MEMBER_NOT_MATCHED | 아이디와 이메일이 일치하는 회원이 없습니다. |
| 400 | WRONG_PASSWORD | 현재 비밀번호가 맞지 않습니다. |
| 403 | ADMIN_CANNOT_WITHDRAW | 관리자는 탈퇴할 수 없습니다. |
| 400 | CANNOT_SUSPEND_SELF | 자기 자신은 정지할 수 없습니다. |
| 409 | DUPLICATE_GUBUN_NAME | 이미 있는 종류 이름입니다. |
| 409 | NEWSLETTER_EXISTS | 오늘 뉴스레터가 이미 있습니다. |
| 400 | NO_SUBSCRIBER | 구독한 회원이 없어 만들지 않았습니다. |
| 400 | NO_PENDING_SUBSCRIBER | 받지 않은 구독자가 없습니다. |
| 409 | SENDING_IN_PROGRESS | 이 종류의 뉴스레터를 보내는 중입니다. 잠시 뒤에 다시 시도해 주세요. |
| 502 | CONTENT_FETCH_FAILED | 본문 데이터를 받지 못해 만들지 않았습니다. |
| 502 | SEND_FAILED | 아무에게도 보내지 못했습니다. |
| 502 | MAIL_SEND_FAILED | 메일을 보내지 못했습니다. 잠시 뒤에 다시 시도해 주세요. |
| 400 | INVALID_LINK | 링크가 올바르지 않거나 더 이상 쓸 수 없습니다. |

---

## 5. API 정의

<!-- API마다 아래 형식을 복사해서 쓴다 -->
### {메서드} {경로}
- **설명**: 
- **권한**: (overview.md "사용자와 권한" 기준)
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
- **오류**: (4. 오류 코드 중 발생할 수 있는 것)
- **사용 테이블**: (database.md 기준)

---

## 6. 실제 사용할 API
<!-- 실제 API 정의를 이 아래에 작성 -->

### 입력값 규칙 (여러 API 공통)
| 이름 | 타입 | 규칙 |
|---|---|---|
| loginId | string | 영문 소문자+숫자 6~20자 (`^[a-z0-9]{6,20}$`) |
| password, newPassword | string | 8~20자, 영문·숫자·특수문자 각 1자 이상 (특수문자 = 영문·숫자·공백이 아닌 출력 가능한 ASCII 문자) |
| passwordConfirm, newPasswordConfirm | string | password(newPassword)와 같아야 함 |
| currentPassword, password(탈퇴) | string | 1~20자 (형식은 검사하지 않고 일치만 확인) |
| email | string | 이메일 형식(`이름@도메인.최상위도메인`), 최대 50자 |
| name (회원) | string | 2~30자 |
| code | string | 숫자 6자리 (`^[0-9]{6}$`) |
| name (종류) | string | 1~20자 |
| sendTime | string | `"HH:mm"`, 00:00~23:59 |
| isUse, isSubscribed, isSuspended | boolean | true/false |
| id, gubunId, userId | number | 1 이상 정수 (BIGINT) |
| page | number | 1 이상 정수 |
| token | string | 1~1000자 |
| keyword | string | 앞뒤 공백을 뺀 뒤 0~50자. 빈 값·없음 = 검색하지 않음. 50자를 넘으면 INVALID_INPUT (D-012) |

### 6.1 인증 (비회원)

#### GET /api/auth/availability
- **설명**: 회원가입 화면에서 아이디 또는 이메일이 사용 가능한지 즉시 확인한다. 둘 중 하나만 보낸다.
- **권한**: 로그인 불필요
- **요청(쿼리)**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | loginId | string | 둘 중 하나 | 아이디 규칙 |
  | email | string | 둘 중 하나 | 이메일 규칙 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | available | boolean | 사용 가능하면 true (탈퇴한 회원의 아이디·이메일은 사용 가능) |
- **오류**: INVALID_INPUT
- **사용 테이블**: users

#### POST /api/auth/email-codes
- **설명**: 회원가입용 인증 코드를 이메일로 보낸다. 다시 보내면 이전 코드는 쓸 수 없다.
- **권한**: 로그인 불필요
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | email | string | O | 이메일 규칙, 다른 회원이 쓰지 않는 이메일 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | expiresInSeconds | number | 코드 유효 시간 (600) |
- **오류**: INVALID_INPUT, DUPLICATE_EMAIL, MAIL_SEND_FAILED
- **사용 테이블**: users (이메일 중복 확인). 코드는 IMemoryCache (키: 회원가입 + 이메일)

#### POST /api/auth/email-codes/verify
- **설명**: 회원가입용 인증 코드를 확인한다. 맞으면 그 이메일이 30분 동안 인증 완료 상태가 된다. 틀리면 횟수를 센다(5번 틀리면 그 코드는 쓸 수 없음).
- **권한**: 로그인 불필요
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | email | string | O | 이메일 규칙 |
  | code | string | O | 코드 규칙 |
- **응답 data**: 없음 (`{}`)
- **오류**: INVALID_INPUT, CODE_INVALID, CODE_EXPIRED
- **사용 테이블**: 없음 (IMemoryCache)

#### POST /api/auth/register
- **설명**: 회원가입. 이메일이 인증 완료 상태여야 한다. 정상 상태의 일반 사용자로 만든다. 가입하면 인증 완료 상태를 지운다. 로그인은 하지 않는다.
- **권한**: 로그인 불필요
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | loginId | string | O | 아이디 규칙, 중복 불가 |
  | password | string | O | 비밀번호 규칙 |
  | passwordConfirm | string | O | password와 같음 |
  | email | string | O | 이메일 규칙, 중복 불가, 인증 완료 |
  | name | string | O | 회원 이름 규칙 |
- **응답 data**: 없음 (`{}`)
- **오류**: INVALID_INPUT, EMAIL_NOT_VERIFIED, DUPLICATE_LOGIN_ID, DUPLICATE_EMAIL
- **사용 테이블**: users

#### POST /api/auth/login
- **설명**: 로그인. 아이디가 없거나, 비밀번호가 틀리거나, 정지 회원이면 모두 LOGIN_FAILED. 같은 아이디 문자열로 5번 연속 실패하면 10분 동안 LOGIN_LOCKED(맞는 비밀번호여도). 성공하면 실패 횟수를 지우고 세션을 만든다(클레임: 회원 id, 아이디, 이름, 관리자 여부).
- **권한**: 로그인 불필요
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | loginId | string | O | 1~20자 (형식이 틀려도 LOGIN_FAILED로 응답) |
  | password | string | O | 1~20자 (형식이 틀려도 LOGIN_FAILED로 응답) |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | redirectUrl | string | `/newsletters` |
- **오류**: INVALID_INPUT(빈 값, 20자 초과), LOGIN_FAILED, LOGIN_LOCKED
- **사용 테이블**: users. 실패 횟수는 IMemoryCache (키: 로그인 + 아이디 문자열)

#### POST /api/auth/logout
- **설명**: 로그아웃. 서버의 세션을 지우고 쿠키를 지운다.
- **권한**: 로그인 필요 (일반 사용자, 관리자)
- **요청**: 없음
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | redirectUrl | string | `/login` |
- **오류**: UNAUTHORIZED
- **사용 테이블**: 없음 (세션 저장소)

#### POST /api/auth/password-reset/codes
- **설명**: 비밀번호 찾기용 인증 코드를 그 회원의 이메일로 보낸다. 아이디와 이메일이 같은 정상 회원의 것이 아니면 메일을 보내지 않는다.
- **권한**: 로그인 불필요
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | loginId | string | O | 아이디 규칙 |
  | email | string | O | 이메일 규칙 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | expiresInSeconds | number | 600 |
- **오류**: INVALID_INPUT, MEMBER_NOT_MATCHED, MAIL_SEND_FAILED
- **사용 테이블**: users. 코드는 IMemoryCache (키: 비밀번호 찾기 + 회원 id)

#### POST /api/auth/password-reset/codes/verify
- **설명**: 비밀번호 찾기용 인증 코드를 확인한다. 맞으면 그 회원이 30분 동안 인증 완료 상태가 된다.
- **권한**: 로그인 불필요
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | loginId | string | O | 아이디 규칙 |
  | email | string | O | 이메일 규칙 |
  | code | string | O | 코드 규칙 |
- **응답 data**: 없음 (`{}`)
- **오류**: INVALID_INPUT, MEMBER_NOT_MATCHED, CODE_INVALID, CODE_EXPIRED
- **사용 테이블**: users

#### POST /api/auth/password-reset
- **설명**: 인증 완료 상태인 회원의 비밀번호를 바꾼다. 지금 비밀번호와 같아도 된다. 바꾸면 인증 완료 상태를 지운다.
- **권한**: 로그인 불필요
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | loginId | string | O | 아이디 규칙 |
  | email | string | O | 이메일 규칙 |
  | newPassword | string | O | 비밀번호 규칙 |
  | newPasswordConfirm | string | O | newPassword와 같음 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | redirectUrl | string | `/login` |
- **오류**: INVALID_INPUT, MEMBER_NOT_MATCHED, EMAIL_NOT_VERIFIED
- **사용 테이블**: users

### 6.2 내 정보 (일반 사용자, 관리자)

모든 API는 로그인 필요. 대상은 항상 세션의 회원 id이며, 요청으로 회원 id를 받지 않는다(자기 데이터만).

#### GET /api/me
- **설명**: 내 정보 조회
- **권한**: 로그인 필요, 자기 데이터
- **요청**: 없음
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | loginId | string | 아이디 |
  | name | string | 이름 |
  | email | string | 이메일 |
  | isAdmin | boolean | 관리자 여부 (탈퇴 영역 표시에 사용) |
  | createDate | string | 가입 시각 (UTC) |
- **오류**: UNAUTHORIZED, NOT_FOUND(세션의 회원이 없어짐)
- **사용 테이블**: users

#### POST /api/me/email-codes
- **설명**: 이메일 변경용 인증 코드를 새 이메일로 보낸다.
- **권한**: 로그인 필요, 자기 데이터
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | email | string | O | 이메일 규칙, 지금 이메일과 다르고 다른 회원이 쓰지 않는 이메일 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | expiresInSeconds | number | 600 |
- **오류**: UNAUTHORIZED, INVALID_INPUT, SAME_EMAIL, DUPLICATE_EMAIL, MAIL_SEND_FAILED
- **사용 테이블**: users. 코드는 IMemoryCache (키: 이메일 변경 + 회원 id + 이메일)

#### POST /api/me/email-codes/verify
- **설명**: 이메일 변경용 인증 코드를 확인한다. 맞으면 (회원, 새 이메일)이 30분 동안 인증 완료 상태가 된다.
- **권한**: 로그인 필요, 자기 데이터
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | email | string | O | 이메일 규칙 |
  | code | string | O | 코드 규칙 |
- **응답 data**: 없음 (`{}`)
- **오류**: UNAUTHORIZED, INVALID_INPUT, CODE_INVALID, CODE_EXPIRED
- **사용 테이블**: 없음 (IMemoryCache)

#### POST /api/me/profile
- **설명**: 이름과 이메일을 저장한다. 이메일이 지금과 같으면 인증 없이 이름만 저장한다. 다르면 인증 완료 상태여야 하고, 저장하면 인증 완료 상태를 지운다. 세션의 이름 클레임도 바꾼다.
- **권한**: 로그인 필요, 자기 데이터
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | name | string | O | 회원 이름 규칙 |
  | email | string | O | 이메일 규칙 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | name | string | 저장된 이름 |
  | email | string | 저장된 이메일 |
- **오류**: UNAUTHORIZED, INVALID_INPUT, EMAIL_NOT_VERIFIED, DUPLICATE_EMAIL, NOT_FOUND
- **사용 테이블**: users

#### POST /api/me/password
- **설명**: 현재 비밀번호를 확인하고 새 비밀번호로 바꾼다. 로그인은 유지된다.
- **권한**: 로그인 필요, 자기 데이터
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | currentPassword | string | O | 1~20자 |
  | newPassword | string | O | 비밀번호 규칙 |
  | newPasswordConfirm | string | O | newPassword와 같음 |
- **응답 data**: 없음 (`{}`)
- **오류**: UNAUTHORIZED, INVALID_INPUT, WRONG_PASSWORD, NOT_FOUND
- **사용 테이블**: users

#### POST /api/me/withdraw
- **설명**: 회원 탈퇴. 현재 비밀번호를 확인하고 그 회원의 historys, subscriptions, users 행을 한 트랜잭션으로 지운 뒤 로그아웃한다. newsletters는 남는다.
- **권한**: 로그인 필요, 자기 데이터, 관리자는 불가(DB의 `is_admin`으로 확인)
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | password | string | O | 1~20자 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | redirectUrl | string | `/login` |
- **오류**: UNAUTHORIZED, INVALID_INPUT, WRONG_PASSWORD, ADMIN_CANNOT_WITHDRAW, NOT_FOUND
- **사용 테이블**: users, subscriptions, historys

#### GET /api/me/subscriptions
- **설명**: 모든 종류(종류 이름 오름차순)와 발송 설정, 내 구독 여부
- **권한**: 로그인 필요, 자기 데이터
- **요청(쿼리)**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | keyword | string | X | 검색어. 앞뒤 공백을 빼고 0~50자. 빈 값이면 전체. 종류 이름 중 하나라도 검색어를 포함하면 나온다(부분 일치, 영문 대소문자 구분 없음, `%`·`_`도 글자 그대로) |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | items[].gubunId | number | 종류 id |
  | items[].name | string | 종류 이름 |
  | items[].sendTime | string \| null | 발송 시간 `"HH:mm"`. 발송 설정이 없으면 null |
  | items[].isUse | boolean \| null | 사용 여부. 발송 설정이 없으면 null |
  | items[].isSubscribed | boolean | 내 구독 여부 |
- **오류**: UNAUTHORIZED, INVALID_INPUT
- **사용 테이블**: gubuns, newsletter_configs, subscriptions

#### POST /api/me/subscriptions
- **설명**: 내 구독을 켜거나 끈다. 이미 원하는 상태면 성공으로 처리한다. 사용 안 함인 종류도 구독할 수 있다.
- **권한**: 로그인 필요, 자기 데이터
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | gubunId | number | O | 있는 종류 |
  | isSubscribed | boolean | O | true: 구독, false: 해지 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | isSubscribed | boolean | 저장된 상태 |
- **오류**: UNAUTHORIZED, INVALID_INPUT, NOT_FOUND(종류 없음)
- **사용 테이블**: gubuns, subscriptions

### 6.3 뉴스레터 수신 내역 (일반 사용자, 관리자)

#### GET /api/newsletters
- **설명**: 발송된 뉴스레터 목록. 최근 발송 순(`newsletters.create_date` 내림차순, 같으면 id 내림차순), 30개씩. 일반 사용자는 자기 `historys`가 있는 뉴스레터만, 관리자는 `historys`가 1건 이상인 모든 뉴스레터(아무도 받지 못한 뉴스레터는 빼고).
- **권한**: 로그인 필요. 일반 사용자는 자기가 받은 것만, 관리자는 전부
- **요청(쿼리)**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | page | number | X | 기본 1 |
  | keyword | string | X | 검색어. 앞뒤 공백을 빼고 0~50자. 빈 값이면 전체. 제목, 종류 이름 중 하나라도 검색어를 포함하면 나온다(부분 일치, 영문 대소문자 구분 없음, `%`·`_`도 글자 그대로) |
- **응답 data**: 목록 응답 형식 (pageSize 30)
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | items[].id | number | 뉴스레터 id |
  | items[].title | string | 제목 |
  | items[].gubunName | string | 종류 이름 |
  | items[].sendDate | string | 발송 시각 (UTC) |
  | items[].subscriberCount | number \| null | 받은 회원 수. 관리자만 값이 있고 일반 사용자는 null |
  | isAdmin | boolean | 관리자 여부 ("구독자" 열 표시에 사용) |
- **오류**: UNAUTHORIZED, INVALID_INPUT
- **사용 테이블**: newsletters, gubuns, historys

#### GET /api/newsletters/{id}
- **설명**: 뉴스레터 본문 (수신 거부 링크 없음). 일반 사용자는 자기 `historys`가 없으면 NOT_FOUND(있는지도 알리지 않는다).
- **권한**: 로그인 필요. 일반 사용자는 자기가 받은 것만, 관리자는 전부
- **요청(경로)**: id (number)
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | id | number | 뉴스레터 id |
  | title | string | 제목 |
  | gubunName | string | 종류 이름 |
  | sendDate | string | 발송 시각 (UTC) |
  | content | string | HTML 본문. 화면에서 `sandbox` iframe의 `srcdoc`으로만 보여준다 |
- **오류**: UNAUTHORIZED, INVALID_INPUT, NOT_FOUND
- **사용 테이블**: newsletters, gubuns, historys

### 6.4 수신 거부 (로그인 불필요)

#### POST /api/unsubscribe
- **설명**: 메일의 수신 거부 링크 토큰으로 그 회원의 그 종류 구독을 해지한다. 토큰은 Data Protection(목적 문자열 `Newsletter.Unsubscribe.v1`)으로 `회원 id:종류 id`를 보호한 값이고 만료가 없다. 이미 해지되어 있으면 성공으로 처리한다.
- **권한**: 로그인 불필요. 토큰을 풀어 나온 회원·종류만 대상
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | token | string | O | 토큰 규칙 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | gubunName | string | 해지한 종류 이름 |
- **오류**: INVALID_INPUT, INVALID_LINK(토큰을 풀 수 없음, 회원 또는 종류가 없어짐)
- **사용 테이블**: users, gubuns, subscriptions

### 6.5 회원 관리 (관리자)

관리자 API(`/api/admin/*`)는 모두 로그인 필요, 관리자만(세션의 관리자 클레임). 일반 사용자는 403 FORBIDDEN.

#### GET /api/admin/users
- **설명**: 회원 목록. 이름 오름차순(같으면 아이디 오름차순), 10명씩. 정지 회원 포함.
- **권한**: 관리자
- **요청(쿼리)**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | page | number | X | 기본 1 |
  | keyword | string | X | 검색어. 앞뒤 공백을 빼고 0~50자. 빈 값이면 전체. 아이디, 이름 중 하나라도 검색어를 포함하면 나온다(부분 일치, 영문 대소문자 구분 없음, `%`·`_`도 글자 그대로) |
- **응답 data**: 목록 응답 형식 (pageSize 10)
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | items[].id | number | 회원 id |
  | items[].loginId | string | 아이디 |
  | items[].name | string | 이름 |
  | items[].isAdmin | boolean | 권한 ("관리자"/"일반") |
  | items[].isSuspended | boolean | 상태 ("정지"/"정상") |
  | items[].isMe | boolean | 로그인한 관리자 자신이면 true (정지 버튼 비활성화) |
  | items[].createDate | string | 가입 시각 (UTC) |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT
- **사용 테이블**: users

#### POST /api/admin/users/{id}/suspension
- **설명**: 회원을 정지하거나 정지를 푼다. 자기 자신은 정지할 수 없다. 다른 관리자는 정지할 수 있다. 정지는 그 회원의 다음 로그인부터 적용된다. 이미 원하는 상태면 성공으로 처리한다.
- **권한**: 관리자
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | id (경로) | number | O | 회원 id |
  | isSuspended | boolean | O | true: 정지, false: 정지 해제 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | isSuspended | boolean | 저장된 상태 |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, CANNOT_SUSPEND_SELF, NOT_FOUND
- **사용 테이블**: users

### 6.6 구독 관리 (관리자)

#### GET /api/admin/subscriptions
- **설명**: 정지 회원을 뺀 모든 회원(관리자 포함). 이름 오름차순(같으면 아이디 오름차순), 10명씩. gubunId가 있으면 그 종류의 구독 여부를 함께 준다.
- **권한**: 관리자
- **요청(쿼리)**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | gubunId | number | X | 없으면 '전체' |
  | page | number | X | 기본 1 |
  | keyword | string | X | 검색어. 앞뒤 공백을 빼고 0~50자. 빈 값이면 전체. 이름, 이메일 중 하나라도 검색어를 포함하면 나온다(부분 일치, 영문 대소문자 구분 없음, `%`·`_`도 글자 그대로) |
- **응답 data**: 목록 응답 형식 (pageSize 10)
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | items[].userId | number | 회원 id |
  | items[].name | string | 이름 |
  | items[].email | string | 이메일 |
  | items[].isSubscribed | boolean \| null | gubunId가 있으면 구독 여부, 없으면 null |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, NOT_FOUND(종류 없음)
- **사용 테이블**: users, subscriptions, gubuns

#### POST /api/admin/subscriptions
- **설명**: 회원의 종류 구독을 켜거나 끈다. 이미 원하는 상태면 성공으로 처리한다. 정지 회원은 대상이 아니다.
- **권한**: 관리자
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | userId | number | O | 정지되지 않은 회원 |
  | gubunId | number | O | 있는 종류 |
  | isSubscribed | boolean | O | true: 구독, false: 해지 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | isSubscribed | boolean | 저장된 상태 |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, NOT_FOUND(회원 없음·정지 회원, 종류 없음)
- **사용 테이블**: users, gubuns, subscriptions

### 6.7 뉴스레터 설정 (관리자)

#### GET /api/admin/gubuns
- **설명**: 모든 종류와 발송 설정. 종류 이름 오름차순. 페이지를 나누지 않는다. 구독 관리 화면의 드롭다운에도 쓴다(드롭다운은 keyword 없이 부른다).
- **권한**: 관리자
- **요청(쿼리)**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | keyword | string | X | 검색어. 앞뒤 공백을 빼고 0~50자. 빈 값이면 전체. 종류 이름 중 하나라도 검색어를 포함하면 나온다(부분 일치, 영문 대소문자 구분 없음, `%`·`_`도 글자 그대로) |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | items[].id | number | 종류 id |
  | items[].name | string | 종류 이름 |
  | items[].sendTime | string \| null | 발송 시간. 설정 없으면 null ("설정 없음") |
  | items[].isUse | boolean \| null | 사용 여부. 설정 없으면 null |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT
- **사용 테이블**: gubuns, newsletter_configs

#### POST /api/admin/gubuns
- **설명**: 종류와 발송 설정을 한 트랜잭션으로 함께 만든다.
- **권한**: 관리자
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | name | string | O | 종류 이름 규칙, 중복 불가 |
  | sendTime | string | O | `"HH:mm"` (화면 기본값 16:00) |
  | isUse | boolean | O | 화면 기본값 false |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | id | number | 만든 종류 id |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, DUPLICATE_GUBUN_NAME
- **사용 테이블**: gubuns, newsletter_configs

#### GET /api/admin/gubuns/{id}
- **설명**: 종류 하나와 발송 설정
- **권한**: 관리자
- **요청(경로)**: id (number)
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | id | number | 종류 id |
  | name | string | 종류 이름 |
  | sendTime | string \| null | 발송 시간. 설정 없으면 null |
  | isUse | boolean \| null | 사용 여부. 설정 없으면 null |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, NOT_FOUND
- **사용 테이블**: gubuns, newsletter_configs

#### POST /api/admin/gubuns/{id}/config
- **설명**: 발송 설정을 저장한다. 설정이 없으면 만든다.
- **권한**: 관리자
- **요청**:
  | 필드 | 타입 | 필수 | 설명 |
  |---|---|---|---|
  | id (경로) | number | O | 종류 id |
  | sendTime | string | O | `"HH:mm"` |
  | isUse | boolean | O | 사용 여부 |
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | sendTime | string | 저장된 발송 시간 |
  | isUse | boolean | 저장된 사용 여부 |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, NOT_FOUND
- **사용 테이블**: gubuns, newsletter_configs

### 6.8 오늘 뉴스레터 (관리자)

#### GET /api/admin/gubuns/{id}/today
- **설명**: 오늘(한국 날짜) 그 종류의 뉴스레터 상태
- **권한**: 관리자
- **요청(경로)**: id (number)
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | exists | boolean | 오늘 뉴스레터가 있는지 |
  | sendDay | string | 오늘 한국 날짜 `"YYYY-MM-DD"` |
  | newsletterId | number \| null | 오늘 뉴스레터 id |
  | title | string \| null | 오늘 뉴스레터 제목 |
  | sendDate | string \| null | 발송 시각 (UTC) |
  | receivedCount | number | 받은 회원 수 (historys 수, 탈퇴한 회원 제외). 없으면 0 |
  | pendingCount | number | 받지 않은 구독자 수. 없으면 지금 구독한 정상 회원 수 |
  | isSending | boolean | 이 종류를 보내는 중인지 |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, NOT_FOUND
- **사용 테이블**: gubuns, newsletters, historys, subscriptions, users

#### POST /api/admin/gubuns/{id}/today/send
- **설명**: 직접 발송. 오늘 뉴스레터가 없을 때만, 사용 여부·발송 시간과 관계없이 지금 만들어 구독한 정상 회원에게 보낸다. 자동 발송과 같은 발송 서비스를 쓴다(아래 "7. 발송 처리").
- **권한**: 관리자
- **요청(경로)**: id (number). 본문 없음
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | newsletterId | number | 만든 뉴스레터 id |
  | receivedCount | number | 이번에 받는 데 성공한 회원 수 |
  | failedCount | number | 이번에 실패한 회원 수 |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, NOT_FOUND, NEWSLETTER_EXISTS, NO_SUBSCRIBER, CONTENT_FETCH_FAILED, SEND_FAILED(모두 실패, 뉴스레터는 남음), SENDING_IN_PROGRESS
- **사용 테이블**: gubuns, newsletters, historys, subscriptions, users

#### POST /api/admin/gubuns/{id}/today/resend
- **설명**: 다시 보내기. 오늘 뉴스레터를 받지 않은 구독자(지금 구독한 정상 회원 중 historys가 없는 회원)에게 보낸다. 지난 날짜 뉴스레터는 대상이 아니다.
- **권한**: 관리자
- **요청(경로)**: id (number). 본문 없음
- **응답 data**:
  | 필드 | 타입 | 설명 |
  |---|---|---|
  | receivedCount | number | 이번에 받는 데 성공한 회원 수 |
  | failedCount | number | 이번에 실패한 회원 수 |
- **오류**: UNAUTHORIZED, FORBIDDEN, INVALID_INPUT, NOT_FOUND(종류 없음, 오늘 뉴스레터 없음), NO_PENDING_SUBSCRIBER, SEND_FAILED(이번에 모두 실패, 뉴스레터는 지우지 않음), SENDING_IN_PROGRESS
- **사용 테이블**: gubuns, newsletters, historys, subscriptions, users

---

## 7. 발송 처리 (API가 아닌 서버 내부 동작)

자동 발송(BackgroundService), 직접 발송, 다시 보내기가 같은 발송 서비스를 쓴다.

### 자동 발송 (NewsletterSchedulerService)
1. 매 분이 바뀔 때(초 0) 깨어나 그 분의 한국 시각 `HH:mm`을 구한다. 깨어난 시각이 그 분을 넘겼으면 그 분은 건너뛴다(밀린 분을 따라잡지 않는다).
2. `is_use = 1`이고 `send_time`이 그 `HH:mm`인 발송 설정을 찾는다.
3. 종류마다 "뉴스레터 만들어 보내기"를 한다. 실패는 로그만 남기고 다음 종류로 넘어간다.

### 뉴스레터 만들어 보내기 (자동 발송, 직접 발송)
1. 종류별 발송 잠금(앱 메모리)을 잡는다. 이미 잡혀 있으면 SENDING_IN_PROGRESS (자동 발송은 건너뜀).
2. 오늘(한국 날짜) 그 종류의 뉴스레터가 있으면 NEWSLETTER_EXISTS (자동 발송은 건너뜀).
3. 구독한 정상 회원을 구한다. 없으면 NO_SUBSCRIBER (자동 발송은 건너뜀).
4. 그 종류의 본문 생성기로 본문을 만든다. 본문 생성기가 없으면 기본 본문(종류 이름과 날짜). 외부 데이터를 받지 못하면 CONTENT_FETCH_FAILED (자동 발송은 건너뜀).
5. `newsletters` 행을 넣는다. `UNIQUE (gubun_id, send_day)`에 걸리면 NEWSLETTER_EXISTS.
6. 회원마다 메일을 한 통씩 보낸다. 본문 맨 아래에 수신 거부 문구와 회원·종류별 링크(`{App:BaseUrl}/unsubscribe?token=...`)를 붙인다. 성공하면 바로 `historys` 행을 넣는다.
7. 아무도 받지 못했으면 SEND_FAILED. `newsletters` 행은 지우지 않고 그대로 둔다(그날은 다시 보내기로만 보낼 수 있다).
8. 잠금을 푼다.

### 다시 보내기
1. 종류별 발송 잠금을 잡는다.
2. 오늘 뉴스레터가 없으면 NOT_FOUND. 받지 않은 구독자가 없으면 NO_PENDING_SUBSCRIBER.
3. 저장된 본문으로 위 6번과 같이 보낸다. 아무도 받지 못해도 뉴스레터는 지우지 않는다(SEND_FAILED).
4. 잠금을 푼다.

### 메일 형식
- 보내는 사람: `.env`의 `Gmail__Address` (설정 키 `Gmail:Address`)
- 제목: `newsletters.title`
- 본문: HTML. `newsletters.content` + 수신 거부 영역("더 이상 받지 않으려면 수신 거부를 눌러 주세요." + "수신 거부" 링크)
- 본문 생성기는 외부 데이터를 HTML 인코딩해서 넣는다.
- 본문에 `<img src="data:image/png;base64,...">`가 있으면 MailService가 보낼 때 그 이미지를 메일 안 첨부 이미지(Content-ID)로 바꾸고 `src`를 `cid:...`로 바꾼다(Gmail은 `data:` 이미지를 보여주지 않음). 저장된 `newsletters.content`는 바꾸지 않으므로 수신 내역 팝업에서는 `data:` 이미지 그대로 보인다.

### 본문 생성기: 날씨 정보 (WeatherContentBuilder)
종류 이름이 "날씨 정보(지역)"이고 지역이 `Weather:Regions`에 있을 때 쓴다. 지역마다 본문 생성기를 하나씩 등록한다(같은 클래스, `GubunName` = "날씨 정보({Name})"). (overview.md "종류별 본문: 날씨 정보")

#### 예보 받기 (WeatherForecastClient, 지역 하나)
1. 예보 발표를 고른다. 지금 한국 시각이 02:10 이후면 `base_date` = 오늘, `base_time` = `0200`. 그 전이면 `base_date` = 어제, `base_time` = `2300`.
2. 같은 발표·같은 격자의 결과가 앱 메모리(IMemoryCache)에 있으면 그것을 쓴다. 성공한 결과만 그날 끝(한국 시간 다음 날 00:00)까지 보관한다. (D-011)
3. `GET {Weather:BaseUrl}?serviceKey={인증키 URL 인코딩}&pageNo=1&numOfRows=1000&dataType=JSON&base_date=YYYYMMDD&base_time=HHmm&nx={Nx}&ny={Ny}`. 타임아웃 10초.
4. 응답 `resultCode`가 `00`이 아니면 실패. (`03` 데이터없음, `22` 요청제한횟수 초과, `30` 등록되지 않은 서비스키 등. 로그에는 코드만 남긴다)
5. 항목(`fcstDate`, `fcstTime`, `category`, `fcstValue`) 중 `fcstDate`가 오늘(한국 날짜)인 것만 쓴다.
   | category | 뜻 | 쓰는 시각 |
   |---|---|---|
   | TMP | 1시간 기온(°C) | 0600, 0900, 1200, 1500, 1800, 2100 |
   | SKY | 하늘 상태: 1 맑음, 3 구름많음, 4 흐림 | 위와 같음 |
   | PTY | 강수 형태: 0 없음, 1 비, 2 비/눈, 3 눈, 4 소나기 | 위와 같음 |
   | POP | 강수 확률(%) | 위와 같음 |
   | TMN | 일 최저기온(°C) | 아무 시각 (하루 한 값) |
   | TMX | 일 최고기온(°C) | 아무 시각 (하루 한 값) |
6. 날씨 = PTY가 0이 아니면 PTY 뜻, 0이면 SKY 뜻. 표에 없는 코드값이면 실패.
7. 위 값 중 하나라도 없거나 숫자로 읽을 수 없으면 실패.

#### 본문 만들기
1. 그 종류의 지역 예보를 받는다. 실패하면 본문 생성기는 null을 돌려주고 발송 처리는 CONTENT_FETCH_FAILED가 된다.
2. 17곳 전부의 예보를 받는다(위 캐시를 함께 쓴다). 다른 지역이 실패하면 그 지역은 지도에 "-"로 그린다.
3. 지도 이미지를 그린다 (WeatherMapRenderer). 실패하면 null(CONTENT_FETCH_FAILED).
   - 크기 가로 600px. 배경과 바다는 단색, 대한민국 윤곽은 `Resources/korea_outline.json`의 경도·위도를 화면 좌표로 바꿔 채운다(경도 124.5~132.2, 위도 33.0~38.7 범위).
   - 지역마다 `Lat`, `Lon` 위치에 점을 찍고, 점에서 (`LabelDx`, `LabelDy`)px 옮긴 곳에 이름표(가로 104px, 세로 24px)를 그린다. 이름표 안에는 12:00 날씨 아이콘(7가지, 코드로 그림), "최저/최고°". 그 종류의 지역은 이름표 테두리를 강조색으로 그린다.
   - 지도 좌표: x = (경도 - 124.5) × 77.9, y = 50 + (38.7 - 위도) × 96.2 (가로 600px, 세로 600px. 위 50px는 제목 자리).
   - 이름표 위치 (`LabelDx`, `LabelDy`, 시안과 같음): 서울 (8, -30), 인천 (-112, -4), 춘천 (8, -30), 강릉 (8, -12), 청주 (8, -26), 세종 (-112, -12), 대전 (8, 2), 안동 (8, -12), 대구 (8, -6), 전주 (-112, -6), 광주 (-112, -6), 창원 (-112, 6), 울산 (8, -8), 부산 (8, 2), 제주 (10, -12), 울릉도 (-40, -32), 독도 (-80, 10)
   - 위쪽에 "오늘 날씨 YYYY-MM-DD". 글꼴은 "맑은 고딕". PNG가 150KB를 넘지 않게 단색 위주로 그린다.
4. 본문 HTML = 지도 `<img src="data:image/png;base64,..." alt="오늘 전국 날씨 지도" width="600">` + "오늘(YYYY-MM-DD) {지역} 날씨" + 최저/최고 + 시간별 표 + 출처 문구. 인라인 스타일만 쓴다(메일 프로그램이 `<style>`을 지울 수 있음). 지역 이름과 API 값은 HTML 인코딩한다.
5. 인증키는 로그·오류 메시지에 남기지 않는다. 실패 로그에는 실패 이유(지역, HTTP 상태, `resultCode`, 빠진 값)만 남긴다.

#### 확인한 근거
- 위 발표시각(하루 8회, 발표 10분 뒤 제공), 카테고리 코드값, `resultCode`는 `docs/reference/단기예보조회서비스_API활용가이드_261019.docx`로 확인했다. 최고/최저기온은 02시 발표에 오늘 값이, 23시 발표에 다음 날 값이 들어 있다(가이드 "최고/최저기온의 발표시간별 저장되는 예보자료 시간").
- 지역 격자와 위경도는 `docs/reference/기상청41_단기예보 조회서비스_오픈API활용가이드_격자_위경도(2610).xlsx`로 확인했다.
- 가이드의 서비스 URL은 기상청 API허브(`apihub.kma.go.kr`, `authKey`)로 적혀 있지만, 인증키를 공공데이터포털에서 받으므로 공공데이터포털 주소(`apis.data.go.kr`, `serviceKey`)를 쓴다. 2026-11-20 인증키로 실제 호출해 확인했다: `apis.data.go.kr` + `serviceKey`로 HTTP 200, `resultCode` `00`, 02시 발표 서울 `totalCount` 944(numOfRows 1000 안), 오늘 TMP·SKY·PTY·POP은 03~23시 매시, TMN은 0600, TMX는 1500 항목에 있음. 응답 항목은 baseDate, baseTime, category, fcstDate, fcstTime, fcstValue, nx, ny.

### 인증 코드 메일
- 제목: "[뉴스레터] 인증 코드"
- 본문: 6자리 코드와 "10분 동안 유효합니다." 문구

---

## 8. 화면 경로 (Razor 페이지, GET)

| 경로 | 화면 | 권한 | 다른 경우 |
|---|---|---|---|
| `/` | 없음 | 모두 | 로그인했으면 `/newsletters`, 아니면 `/login`으로 이동 |
| `/login` | 로그인 | 비회원 | 로그인했으면 `/newsletters`로 이동 |
| `/register` | 회원가입 | 비회원 | 로그인했으면 `/newsletters`로 이동 |
| `/password-reset` | 비밀번호 찾기 | 비회원 | 로그인했으면 `/newsletters`로 이동 |
| `/unsubscribe?token=` | 수신 거부 | 로그인 불필요 | 토큰이 올바르지 않으면 같은 화면에 INVALID_LINK 문구 |
| `/newsletters` | 뉴스레터 수신 내역 | 일반 사용자, 관리자 | 로그인 안 했으면 `/login` |
| `/my/subscriptions` | 내 구독 | 일반 사용자, 관리자 | 로그인 안 했으면 `/login` |
| `/my/profile` | 내 정보 | 일반 사용자, 관리자 | 로그인 안 했으면 `/login` |
| `/admin/users` | 회원 관리 | 관리자 | 로그인 안 했으면 `/login`, 일반 사용자는 `/newsletters` |
| `/admin/subscriptions` | 구독 | 관리자 | 위와 같음 |
| `/admin/gubuns` | 뉴스레터 설정 | 관리자 | 위와 같음 |
| `/admin/gubuns/{id}` | 발송 설정 상세 | 관리자 | 위와 같음. 종류가 없으면 `/admin/gubuns` |
