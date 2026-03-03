# 📊 Project Analysis Report

> **프로젝트**: HeyNowBot (HeysDevServer)
> **분석일**: 2026-03-03
> **분석자**: Senior Project Architect (Copilot)

---

## 1. Tech Stack Overview

### 프레임워크 / 언어
| 항목 | 내용 |
|------|------|
| 언어 | C# 12 |
| 런타임 | .NET 8.0 |
| 프로젝트 타입 | Console Application (OutputType: Exe) |
| 타겟 OS | Linux (DockerDefaultTargetOS: Linux) |
| Nullable | 활성화 |

### 주요 라이브러리 (NuGet)
| 패키지 | 버전 | 용도 |
|--------|------|------|
| `Telegram.Bot` | 22.7.6 | Telegram 메시지 전송 |
| `Supabase` | 1.1.1 | PostgreSQL 기반 BaaS (DB, Realtime) |
| `HtmlAgilityPack` | 1.11.71 | RSS HTML 파싱 |
| `Google.Apis.Gmail.v1` | 1.61.0 | Gmail API (미완성) |
| `Google.Apis.Auth` | 1.65.0 | Google OAuth 인증 |
| `MimeKit` | 4.3.0 | 이메일 파싱 (Gmail 연동용) |
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | 1.23.0 | Docker 컨테이너 빌드 지원 |

### 인프라 / 배포
| 항목 | 내용 |
|------|------|
| 컨테이너 | Docker (멀티스테이지 빌드) |
| 기본 이미지 | `mcr.microsoft.com/dotnet/runtime:8.0` |
| DB | Supabase (PostgreSQL) |
| CI/CD | GitHub Actions (`deploy.yml`) |
| 배포 방식 | SSH → `deploy.sh` 실행 → 컨테이너 재빌드 |
| 타임존 | Asia/Seoul |
| 재시작 정책 | `--restart=always` |

---

## 2. Architecture & Structure

### 적용된 패턴
- **서비스 레이어 패턴** (Service Layer Pattern)
- **인터페이스 기반 의존성 주입** (Manual DI, no container)
- **이벤트 기반 아키텍처** (Event-driven: `TimeCheckerService` → `ProcessMain`)

> Clean Architecture의 레이어 분리 의도는 있으나, DI 컨테이너 없이 수동 주입하는 Lightweight 구조.

### 폴더 구조
```
HeysDevServer/
├── .github/
│   ├── skills/                  # Copilot CLI 커스텀 스킬
│   │   ├── git-commit/
│   │   ├── git-push/
│   │   └── project_analyze/
│   ├── workflows/
│   │   └── deploy.yml           # GitHub Actions CI/CD
│   ├── AGENTS.md
│   └── copilot-instructions.md  # Copilot 커스텀 인스트럭션
├── HeyNowBot/                   # 메인 애플리케이션
│   ├── Program.cs               # 진입점 & 수동 DI 조립
│   ├── ProcessMain.cs           # 메인 루프 & 이벤트 핸들러
│   ├── Constants.cs             # 전역 상수 (Telegram, Stock, RSS, Schedule)
│   ├── Dockerfile               # 멀티스테이지 Docker 빌드
│   ├── deploy.sh                # 배포 자동화 스크립트
│   ├── Service/
│   │   ├── ServiceTelegram.cs   # ITelegramService → Telegram 메시지 전송
│   │   ├── ServiceSupabase.cs   # ISupabaseService → DB 조회/업데이트
│   │   ├── TaskRunService.cs    # ITaskRunService → 작업 조율 (RSS, 주식, 방문)
│   │   ├── TimeChekerService.cs # ITimeCheckerService → 시간 기반 이벤트 발생
│   │   ├── RssService.cs        # IRssService → RSS 피드 파싱
│   │   ├── NaverFinanceService.cs # INaverFinanceService → 주식 스크래핑
│   │   └── MessageQueue.cs      # IMessageQueue → 메시지 배치 & 속도 제한
│   └── Model/
│       └── VisitLogModel.cs     # Supabase ORM 모델
└── HeysDevServer.slnx           # 솔루션 파일
```

---

## 3. Core Workflows

### 진입점
**`Program.cs`** → 수동으로 모든 서비스 인스턴스화 → `ProcessMain` 조립 → `RunAsync()` 호출

### 주요 데이터 흐름

```
[Program.cs]
    │ 서비스 인스턴스 생성 & 주입
    ▼
[ProcessMain.RunAsync()]
    │ 1. Telegram 시작 메시지 전송
    │ 2. RSS 초기화 (기존 피드 스냅샷 수집)
    │ 3. TimeCheckerService 이벤트 등록 & Start
    │ 4. Task.Delay(Infinite) → 무한 대기
    ▼
[TimeCheckerService] ─── 1초마다 현재 시간 체크 ───►
    │ minute 변경 시 이벤트 발생:
    ├── On1MinReached  (매 분)
    ├── On10MinReached (10분마다) ──► [ProcessMain] → GetStockPriceMessageAsync() [오전 09-11시]
    ├── On30MinReached (30분마다) ──► [ProcessMain] → GetStockPriceMessageAsync() [오후 11-15:30시]
    └── OnHourReached  (매시간)  ──► [ProcessMain] → GetCountAlarmMessageAsync() + GetRssNewsMessageAsync()
                                          │
                                          ▼
                                    [MessageQueue.Enqueue()]
                                          │ 400ms 배치 플러시
                                          ▼
                                    [TelegramService.SendMessageAsync()]
                                          │
                                          ▼
                                    📱 Telegram 알림 전송

[NaverFinanceService] → HTTP 스크래핑 (finance.naver.com)
[RssService]         → HTTP 파싱 (RSS XML)
[SupabaseService]    → PostgreSQL 쿼리 (방문자 로그)
```

### 주식 모니터링 스케줄
| 시간대 | 주기 | 이벤트 |
|--------|------|--------|
| 09:00 ~ 11:00 | 10분마다 | `On10MinReached` |
| 11:00 ~ 15:30 | 30분마다 | `On30MinReached` |
| 매시간 정각 | 1회 | `OnHourReached` (RSS + 방문자) |
| 3시간마다 | 1회 | 방문자 통계 집계 |

---

## 4. Architect's Review & Recommendations

### ✅ 잘된 점

1. **인터페이스 기반 설계** - 모든 서비스가 `IXxxService` 인터페이스를 가져 테스트 가능성 확보
2. **관심사 분리** - 각 서비스가 단일 책임을 명확히 유지 (Telegram, Supabase, RSS, Stock 등)
3. **에러 격리** - 모든 이벤트 핸들러에 try-catch 적용, 한 서비스 실패가 전체를 멈추지 않음
4. **메시지 배치 처리** - `MessageQueue`로 Telegram API 속도 제한 대응
5. **Docker 멀티스테이지 빌드** - `ARG PROJECT_NAME` 변수화로 재사용 가능한 Dockerfile
6. **상수 중앙화** - `Constants.cs`에 모든 설정값 집중 관리
7. **레거시 호환성** - `[Obsolete]` 어노테이션으로 구버전 클래스명 하위 호환 유지

### ⚠️ 개선 / 보완이 필요한 점

#### 🔴 보안
| 문제 | 위치 | 권고 |
|------|------|------|
| **봇 토큰 하드코딩** | `Constants.cs:14` | 기본값에 실제 토큰 포함. 프로덕션 배포 전 반드시 환경변수 전용으로 전환 |
| **Supabase Anon Key 하드코딩** | `Constants.cs:130` | 동일. `SUPABASE_ANON_KEY` 환경변수만 허용하도록 변경 권장 |
| **Program.cs 디버그 출력** | `Program.cs:5-6` | 시작 시 토큰/채팅ID를 콘솔에 출력 — 프로덕션 로그에 노출 위험 |

#### 🟡 안정성
| 문제 | 위치 | 권고 |
|------|------|------|
| **Supabase 초기화 레이스컨디션** | `ServiceSupabase.cs:27` | 생성자에서 `async` 초기화를 `ConfigureAwait(false)` 후 fire-and-forget — 초기화 완료 전 쿼리 호출 시 `null` 반환. `EnsureInitializedAsync()`가 300ms만 기다리므로 느린 네트워크에서 실패 가능 |
| **타이머 중복 실행 방지 미흡** | `TimeChekerService.cs:60` | `FireAsync`가 fire-and-forget (`_ = FireAsync(now)`)으로 실행돼 이전 작업 완료 전 다음 분 트리거 가능 |
| **MessageQueue 중복 제거 방식** | `MessageQueue.cs:99` | `HashSet`으로 완전 동일 문자열만 제거 — 실질적으로 배치 내 중복 거의 없음, 의도 재검토 필요 |

#### 🟢 성능 / 유지보수
| 문제 | 권고 |
|------|------|
| **DI 컨테이너 부재** | `Microsoft.Extensions.DependencyInjection` 도입 시 서비스 등록/주입 자동화, 라이프사이클 관리 가능 |
| **로깅 인프라 미흡** | 각 클래스에 `Console.WriteLine` 직접 호출 — `Microsoft.Extensions.Logging` 또는 `Serilog` 도입 권장 (레벨 필터링, 파일 출력 등) |
| **단위 테스트 없음** | 인터페이스 설계가 잘 되어 있어 Mock 기반 테스트 추가 용이. `xUnit` + `Moq` 도입 권장 |
| **Gmail API 미완성** | `Google.Apis.Gmail.v1`, `MimeKit` 패키지가 등록되어 있지만 실제 서비스 미통합 — 사용 안 할 경우 패키지 제거로 빌드 크기 감소 |
| **NaverFinanceService 스크래핑 취약성** | 네이버 HTML 구조 변경 시 파싱 즉시 중단 — 공식 API 또는 별도 데이터 소스 검토 권장 |

---

*분석 완료: HeyNowBot은 소규모 모니터링 봇으로 구조가 명확하고 서비스 분리가 잘 되어 있습니다. 보안 이슈(하드코딩된 시크릿) 해결이 가장 시급한 과제입니다.*
