# 🏗️ Clean Architecture Refactoring Report

> 리팩토링 일자: 2026-03-03

---

## 1. Architecture Overview (적용된 구조)

```
HeyNowBot/
├── Domain/                          ← 프레임워크 독립적 핵심 계층
│   ├── Entities/
│   │   ├── RssItem.cs               ← RSS 피드 아이템 엔티티
│   │   ├── StockInfo.cs             ← 주식 정보 엔티티
│   │   └── VisitLog.cs              ← 방문 기록 DB 모델
│   └── Interfaces/
│       ├── INaverFinanceService.cs
│       ├── IRssService.cs
│       ├── ISupabaseService.cs
│       ├── ITelegramService.cs
│       ├── ITaskRunService.cs
│       ├── ITimeCheckerService.cs
│       └── IMessageQueue.cs
│
├── Application/                     ← 비즈니스 유즈케이스 계층
│   └── TaskRunService.cs            ← RSS, 주식, 방문자 통계 조율
│
├── Infrastructure/                  ← 외부 의존성 구현 계층
│   ├── External/
│   │   ├── NaverFinanceService.cs   ← 네이버 금융 HTML 파싱
│   │   ├── RssService.cs            ← RSS 피드 HTTP 요청 및 파싱
│   │   └── SupabaseService.cs       ← Supabase DB 접근
│   └── Messaging/
│       ├── TelegramService.cs       ← Telegram Bot 메시지 전송
│       ├── MessageQueue.cs          ← 속도 제한용 메시지 배치 큐
│       └── TimeCheckerService.cs    ← 분/시간 주기 이벤트 스케줄러
│
├── Constants.cs                     ← 전역 설정 상수 (유지)
├── ProcessMain.cs                   ← 앱 메인 루프 / 이벤트 핸들러 (유지)
└── Program.cs                       ← 진입점 / DI Composition Root (유지)
```

---

## 2. File Mapping (주요 파일 이동 내역)

| 기존 경로 | 새 경로 | 비고 |
|---|---|---|
| `Model/VisitLogModel.cs` | `Domain/Entities/VisitLog.cs` | 클래스명 VisitLog로 변경, 네임스페이스 변경 |
| `Service/RssService.cs` (RssItem) | `Domain/Entities/RssItem.cs` | 엔티티 분리 |
| `Service/RssService.cs` (IRssService) | `Domain/Interfaces/IRssService.cs` | 인터페이스 분리 |
| `Service/RssService.cs` (RssService 구현) | `Infrastructure/External/RssService.cs` | 구현부 분리 |
| `Service/NaverFinanceService.cs` (StockInfo) | `Domain/Entities/StockInfo.cs` | 엔티티 분리 |
| `Service/NaverFinanceService.cs` (INaverFinanceService) | `Domain/Interfaces/INaverFinanceService.cs` | 인터페이스 분리 |
| `Service/NaverFinanceService.cs` (구현) | `Infrastructure/External/NaverFinanceService.cs` | 구현부 분리 |
| `Service/ServiceSupabase.cs` (ISupabaseService) | `Domain/Interfaces/ISupabaseService.cs` | 인터페이스 분리, `VisitLogModel` → `VisitLog` |
| `Service/ServiceSupabase.cs` (구현) | `Infrastructure/External/SupabaseService.cs` | 오타 파일명 제거, 레거시 별칭 제거 |
| `Service/ServiceTelegram.cs` (ITelegramService) | `Domain/Interfaces/ITelegramService.cs` | 인터페이스 분리 |
| `Service/ServiceTelegram.cs` (구현) | `Infrastructure/Messaging/TelegramService.cs` | 구현부 분리 |
| `Service/MessageQueue.cs` (IMessageQueue) | `Domain/Interfaces/IMessageQueue.cs` | 인터페이스 분리 |
| `Service/MessageQueue.cs` (구현) | `Infrastructure/Messaging/MessageQueue.cs` | 구현부 분리 |
| `Service/TimeChekerService.cs` (ITimeCheckerService) | `Domain/Interfaces/ITimeCheckerService.cs` | 인터페이스 분리 |
| `Service/TimeChekerService.cs` (구현) | `Infrastructure/Messaging/TimeCheckerService.cs` | **오타 수정** (Cheker→Checker), 레거시 별칭 제거 |
| `Service/TaskRunService.cs` (ITaskRunService) | `Domain/Interfaces/ITaskRunService.cs` | 인터페이스 분리 |
| `Service/TaskRunService.cs` (구현) | `Application/TaskRunService.cs` | 유즈케이스 계층으로 이동 |

**삭제된 폴더:**
- `Service/` (내용 모두 이전)
- `Model/` (내용 모두 이전)

---

## 3. Dependency Updates (의존성 수정 사항)

| 파일 | 변경 내용 |
|---|---|
| `Program.cs` | `using HeyNowBot.Service` → `using HeyNowBot.Application`, `HeyNowBot.Infrastructure.External`, `HeyNowBot.Infrastructure.Messaging` |
| `ProcessMain.cs` | `using HeyNowBot.Service` → `using HeyNowBot.Domain.Interfaces` |
| `Application/TaskRunService.cs` | `using HeyNowBot.Model` → `using HeyNowBot.Domain.Entities`, `HeyNowBot.Domain.Interfaces` |
| `Infrastructure/External/SupabaseService.cs` | `VisitLogModel` → `VisitLog` (Domain.Entities) |
| `Domain/Interfaces/ISupabaseService.cs` | `VisitLogModel` → `VisitLog` |
| `Domain/Interfaces/ITaskRunService.cs` | `VisitLogModel` → `VisitLog` |

**네임스페이스 변경 요약:**

| 이전 | 이후 |
|---|---|
| `HeyNowBot.Model` | `HeyNowBot.Domain.Entities` |
| `HeyNowBot.Service` (인터페이스) | `HeyNowBot.Domain.Interfaces` |
| `HeyNowBot.Service` (데이터 구현) | `HeyNowBot.Infrastructure.External` |
| `HeyNowBot.Service` (메시징/스케줄) | `HeyNowBot.Infrastructure.Messaging` |
| `HeyNowBot.Service` (유즈케이스) | `HeyNowBot.Application` |

---

## 4. Next Steps & Recommendations

1. **DI 컨테이너 도입**: 현재 `Program.cs`에서 수동으로 인스턴스를 생성함. `Microsoft.Extensions.DependencyInjection`을 도입하면 더 확장성 높은 구성이 가능.

2. **Nullable 정리**: 빌드 경고(CS8618)에서 `RssItem`, `StockInfo`, `VisitLog`의 string 프로퍼티들이 nullable로 선언되지 않음. 프로젝트 전체에 `<Nullable>enable</Nullable>` 적용 후 프로퍼티를 `string?`으로 정리 권장.

3. **레거시 별칭 제거 완료**: 기존의 `[Obsolete] ServiceSupabase`, `[Obsolete] TimeChekerService` 별칭 클래스 완전 제거됨.

4. **Gmail 서비스 통합**: `GMAIL_API_SETUP.md`에 설명된 Gmail API가 아직 미통합 상태. 추후 `Infrastructure/External/GmailService.cs` + `Domain/Interfaces/IGmailService.cs`로 동일한 패턴 적용 권장.

5. **단위 테스트 추가**: Clean Architecture 구조가 완성되어 Domain/Interfaces 기반의 Mock 테스트 작성이 용이해짐. `xUnit` + `Moq`를 이용한 `TaskRunService` 유닛 테스트 추가 권장.
