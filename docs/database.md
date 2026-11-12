# database.md

데이터 모델의 기준 문서다. 스키마를 바꾸는 작업에서는 이 문서도 함께 갱신한다.

이 문서와 실제 스키마가 다르면 사용자에게 알린다.

---

## 1. 개요

- **DB**: SQL Server 2022 이상 (Developer 에디션), 데이터베이스 이름 `Newsletter`
- **스키마 관리**: EF Core 마이그레이션으로 만든다. 마이그레이션 결과는 "4. 실제 사용할 테이블 생성 코드"와 같아야 한다.
  - 앱을 시작하면 아직 적용하지 않은 마이그레이션을 자동으로 적용한다(`Database.Migrate()`). DB가 없으면 만든다.
  - EF Core는 `UNIQUE` 제약을 고유 인덱스(`IX_테이블_컬럼`)로 만들고, PK·FK 이름도 EF가 정한다(`PK_users`, `FK_...`). 중복 방지 효과는 같다.
  - 마이그레이션 SQL 확인: `dotnet ef migrations script --project NewsletterApp`

---

## 2. 네이밍 규칙

| 대상 | 규칙 | 예 |
|---|---|---|
| 테이블 | 소문자 snake_case, 단수형 + s (영어 불규칙 복수형은 쓰지 않음) | `users`, `historys`, `newsletter_configs` |
| 컬럼 | 소문자 snake_case | `send_time` |
| 기본 키 | `id BIGINT IDENTITY(1,1) PRIMARY KEY` | |
| 외래 키 컬럼 | `{단수 테이블명}_id` | `user_id`, `gubun_id` |
| 인덱스 | `idx_{테이블}_{컬럼}` | `idx_users_email` |
| 여부(불리언) | `is_` 접두사 + `BIT` (0/1) | `is_use BIT` |
| 값 제한 | 들어갈 값이 정해진 컬럼(불리언 제외)에 `CHECK (컬럼 IN (...))`를 붙인다 | `CHECK (status IN ('A','B'))` |
| 시각 | `DATETIME`, UTC로 저장, 기본값 `GETUTCDATE()` | |
| 시각 컬럼명 | `_date` 접미사 | `send_date` |
| 문자열 | `NVARCHAR` | |
| 중복 방지 | 단일 컬럼은 인라인 `UNIQUE`, 연결 테이블은 두 FK 조합에 복합 `UNIQUE` | `UNIQUE (user_id, gubun_id)` |
| 외래 키 선언 | 컬럼 정의 뒤에 모아서 쓴다 | `FOREIGN KEY (gubun_id) REFERENCES gubuns(id)` |
| 제약 이름 | DEFAULT, FK, UNIQUE, CHECK에 이름을 붙이지 않는다 (DB가 자동으로 정함) | |
| 컬럼 순서 | id → 업무 컬럼 → FK → 여부 → 날짜 → create_date | |
| 표기 | SQL 키워드는 대문자, 타입·제약 열을 맞춰 정렬 | |
| 설명 | 컬럼마다 줄 끝에 `-- 설명`을 단다 | `name NVARCHAR(20) NOT NULL, -- 이름` |

### 공통 컬럼
- `id`: 모든 테이블에 둔다
- `create_date`: 생성 시각 (UTC). 마지막 컬럼에 둔다. 설정 정보 테이블에는 두지 않는다
- 수정 시각: 기본으로 만들지 않고, 필요한 테이블에만 만든다

### 테이블 종류
| 종류 | 규칙 | 예 |
|---|---|---|
| 연결 테이블 | 양쪽 FK와 `create_date`만 두고, 두 FK 조합에 `UNIQUE` | `subscriptions` |
| 설정 테이블 | 분류·설정 값을 저장하고, 생성·수정 시각을 두지 않는다 | `gubuns`, `newsletter_configs` |

### 예
<!-- 아래는 SQL Server 예다. 프로젝트 DB의 문법에 맞게 바꾼다 -->
```sql
CREATE TABLE subscriptions (
    id          BIGINT IDENTITY(1,1) PRIMARY KEY,           -- 기본키
    user_id     BIGINT   NOT NULL,                          -- 구독한 회원
    gubun_id    BIGINT   NOT NULL,                          -- 구독한 구분
    create_date DATETIME NOT NULL DEFAULT GETUTCDATE(),     -- 생성 시각 (UTC)

    FOREIGN KEY (user_id)  REFERENCES users(id),
    FOREIGN KEY (gubun_id) REFERENCES gubuns(id),
    UNIQUE (user_id, gubun_id)
);
```

---

## 3. 테이블 정의

<!-- 테이블마다 아래 형식을 복사해서 쓴다. 2. 네이밍 규칙을 따른다 -->
```sql
CREATE TABLE 테이블명 (
    컬럼 타입 제약 -- 설명
);
```

<!-- 예시 (SQL Server). 프로젝트 DB의 문법에 맞게 바꾼다 -->
```sql
CREATE TABLE users (
    id          BIGINT IDENTITY(1,1) PRIMARY KEY,           -- 기본키
    name        NVARCHAR(20) NOT NULL,                      -- 이름
    create_date DATETIME     NOT NULL DEFAULT GETUTCDATE()  -- 생성 시각 (UTC)
);
```

### 테이블 목록
| 테이블 | 종류 | 설명 | 관련 기능 |
|---|---|---|---|
| `users` | 일반 | 회원 | 회원가입, 로그인, 비밀번호 찾기, 내 정보, 회원 관리 |
| `gubuns` | 설정 | 뉴스레터 종류 | 뉴스레터 설정, 내 구독, 구독 관리 |
| `newsletter_configs` | 설정 | 종류별 발송 설정 (종류당 하나) | 뉴스레터 설정, 내 구독, 자동 발송 |
| `subscriptions` | 연결 | 회원의 종류 구독 | 내 구독, 구독 관리, 수신 거부, 발송 |
| `newsletters` | 일반 | 종류별 하루 한 번 만든 뉴스레터 | 자동 발송, 직접 발송, 수신 내역 |
| `historys` | 연결 | 회원이 뉴스레터를 받은 기록 (받는 데 성공한 회원만) | 발송, 다시 보내기, 수신 내역 |

### 값 규칙
| 컬럼 | 규칙 |
|---|---|
| `users.login_id` | 영문 소문자+숫자 6~20자 (`^[a-z0-9]{6,20}$`) |
| `users.password_hash` | BCrypt 해시 60자. 원래 비밀번호는 8~20자, 영문·숫자·특수문자 각 1자 이상 |
| `users.email` | 이메일 형식, 최대 50자, 앞뒤 공백 제거 후 소문자로 저장 |
| `users.name` | 앞뒤 공백 제거 후 2~30자 |
| `gubuns.name` | 앞뒤 공백 제거 후 1~20자 |
| `newsletter_configs.send_time` | 한국 시간 시·분 (초는 0). 00:00~23:59 |
| `newsletters.title` | "[종류 이름] YYYY-MM-DD", 최대 33자 (1 + 20 + 2 + 10) |
| `newsletters.content` | HTML 본문. 수신 거부 문구와 링크는 넣지 않는다(메일을 보낼 때 붙인다) |
| `newsletters.send_day` | 한국 날짜. 종류별 하루 한 번 규칙을 `UNIQUE (gubun_id, send_day)`로 지킨다 |
| `newsletters.create_date` | 만든 시각(UTC) = 화면의 "발송 시각" |
| `historys.create_date` | 그 회원이 받는 데 성공한 시각(UTC) |

### 삭제 규칙
- FK에 `ON DELETE CASCADE`를 쓰지 않는다. 지울 때는 코드에서 자식부터 한 트랜잭션으로 지운다.
- 회원 탈퇴: `historys`(그 회원) → `subscriptions`(그 회원) → `users` 순서로 지운다. `newsletters`는 남긴다.
- 발송에서 아무도 받지 못한 뉴스레터: 지우지 않고 그대로 둔다(`historys`가 0건인 `newsletters` 행). 하루 한 번 규칙(`UNIQUE (gubun_id, send_day)`)에 따라 그날 같은 종류는 새로 만들지 않는다.
- `newsletters`는 지우지 않는다.
- `gubuns`, `newsletter_configs`는 지우지 않는다.

---

## 4. 실제 사용할 테이블 생성 코드
<!-- 실제 테이블 생성 코드를 이 아래에 작성 -->
```sql
CREATE TABLE users (
    id            BIGINT IDENTITY(1,1) PRIMARY KEY,           -- 기본키
    login_id      NVARCHAR(20) NOT NULL UNIQUE,               -- 아이디 (영문 소문자+숫자 6~20자)
    password_hash NVARCHAR(60) NOT NULL,                      -- 비밀번호 BCrypt 해시
    email         NVARCHAR(50) NOT NULL UNIQUE,               -- 이메일 (소문자)
    name          NVARCHAR(30) NOT NULL,                      -- 이름
    is_admin      BIT          NOT NULL DEFAULT 0,            -- 관리자 여부 (DB에서 직접 지정)
    is_suspended  BIT          NOT NULL DEFAULT 0,            -- 정지 여부
    create_date   DATETIME     NOT NULL DEFAULT GETUTCDATE()  -- 가입 시각 (UTC)
);

CREATE TABLE gubuns (
    id   BIGINT IDENTITY(1,1) PRIMARY KEY,                    -- 기본키
    name NVARCHAR(20) NOT NULL UNIQUE                         -- 종류 이름
);

CREATE TABLE newsletter_configs (
    id        BIGINT IDENTITY(1,1) PRIMARY KEY,               -- 기본키
    send_time TIME(0) NOT NULL,                               -- 발송 시간 (한국 시간 시·분)
    gubun_id  BIGINT  NOT NULL UNIQUE,                        -- 종류 (종류당 설정 하나)
    is_use    BIT     NOT NULL DEFAULT 0,                     -- 사용 여부 (1: 사용, 0: 사용 안 함)

    FOREIGN KEY (gubun_id) REFERENCES gubuns(id)
);

CREATE TABLE subscriptions (
    id          BIGINT IDENTITY(1,1) PRIMARY KEY,             -- 기본키
    user_id     BIGINT   NOT NULL,                            -- 구독한 회원
    gubun_id    BIGINT   NOT NULL,                            -- 구독한 종류
    create_date DATETIME NOT NULL DEFAULT GETUTCDATE(),       -- 구독 시각 (UTC)

    FOREIGN KEY (user_id)  REFERENCES users(id),
    FOREIGN KEY (gubun_id) REFERENCES gubuns(id),
    UNIQUE (user_id, gubun_id)
);

CREATE INDEX idx_subscriptions_gubun_id ON subscriptions (gubun_id);

CREATE TABLE newsletters (
    id          BIGINT IDENTITY(1,1) PRIMARY KEY,             -- 기본키
    title       NVARCHAR(50)  NOT NULL,                       -- 제목 "[종류 이름] YYYY-MM-DD"
    content     NVARCHAR(MAX) NOT NULL,                       -- HTML 본문 (수신 거부 링크 제외)
    gubun_id    BIGINT        NOT NULL,                       -- 종류
    send_day    DATE          NOT NULL,                       -- 발송일 (한국 날짜)
    create_date DATETIME      NOT NULL DEFAULT GETUTCDATE(),  -- 만든 시각 = 발송 시각 (UTC)

    FOREIGN KEY (gubun_id) REFERENCES gubuns(id),
    UNIQUE (gubun_id, send_day)
);

CREATE INDEX idx_newsletters_create_date ON newsletters (create_date);

CREATE TABLE historys (
    id            BIGINT IDENTITY(1,1) PRIMARY KEY,           -- 기본키
    newsletter_id BIGINT   NOT NULL,                          -- 받은 뉴스레터
    user_id       BIGINT   NOT NULL,                          -- 받은 회원
    create_date   DATETIME NOT NULL DEFAULT GETUTCDATE(),     -- 받는 데 성공한 시각 (UTC)

    FOREIGN KEY (newsletter_id) REFERENCES newsletters(id),
    FOREIGN KEY (user_id)       REFERENCES users(id),
    UNIQUE (newsletter_id, user_id)
);

CREATE INDEX idx_historys_user_id ON historys (user_id);
```
