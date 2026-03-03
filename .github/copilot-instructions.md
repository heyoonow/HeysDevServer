# HeysDevServer Copilot 가이드

## 프로젝트 개요

**HeyNowBot**은 RSS 피드, 주식 가격을 모니터링하고 Telegram을 통해 알림을 보내는 .NET 8.0 콘솔 애플리케이션입니다. Linux 서버에서 Docker 컨테이너로 실행됩니다.

**아키텍처:**
- **Program.cs** - 진입점; 모든 서비스 초기화
- **ProcessMain.cs** - 메인 애플리케이션 루프; 시간 기반 이벤트 핸들러 조정
- **서비스 레이어** - 특정 기능을 담당하는 독립적 서비스들:
  - `TelegramService` - Telegram 메시지 전송
  - `SupabaseService` - 데이터베이스 작업
  - `TaskRunService` - RSS, 주식, 방문 기록 작업 조율
  - `TimeCheckerService` - 특정 시간(10분 마크, 매시간)에 이벤트 발생
  - `RssService` - 설정된 URL의 RSS 피드 파싱
  - `NaverFinanceService` - 주식 가격 데이터 조회
  - `MessageQueue` - 속도 제한 회피를 위해 메시지 일괄 처리
- **모델 레이어** - `VisitLogModel` - 데이터베이스 모델
- **Constants.cs** - 설정 (Telegram 토큰, 채팅 ID, 주식 코드, RSS 피드, 일정)

## 빌드 및 배포

### 필수 요구사항
- .NET 8.0 SDK (로컬 개발용)
- Docker (컨테이너화된 배포용)
- dotnet CLI

### 빌드 명령어
```bash
# 의존성 복원
dotnet restore HeyNowBot/HeyNowBot.csproj

# 로컬 빌드
cd HeyNowBot
dotnet build -c Release

# 로컬 실행
dotnet run
```

### Docker 배포
모든 Docker 작업은 `deploy.sh`로 자동화됩니다:

```bash
cd HeyNowBot
chmod +x deploy.sh
./deploy.sh
```

이 스크립트는:
1. 기존 컨테이너 중지/제거
2. `ARG PROJECT_NAME=HeyNowBot`으로 Docker 이미지 빌드
3. 타임존과 Telegram 환경변수와 함께 새 컨테이너 실행

**Docker 주요 사항:**
- 기본 이미지: `mcr.microsoft.com/dotnet/runtime:8.0` (최종 스테이지)
- 빌드 이미지: `mcr.microsoft.com/dotnet/sdk:8.0`
- Dockerfile은 `ARG PROJECT_NAME` 변수를 사용한 멀티 스테이지 빌드 사용 - 실제 프로젝트 이름과 일치해야 함
- 컨테이너 자동 재시작 (`--restart=always`)
- 타임존: `Asia/Seoul`

### 자동 배포 (CI/CD)
- GitHub Actions 워크플로우: `.github/workflows/deploy.yml`
- `master` 브랜치로 푸시 시 트리거
- 서버에 SSH 연결해서 `./deploy.sh` 실행
- 필요한 secrets: `SERVER_HOST`, `SERVER_USER`, `SERVER_SSH_KEY`

## 핵심 규칙 및 패턴

### 서비스 아키텍처
- 모든 서비스는 인터페이스 기반 의존성 주입 패턴을 따름
- 서비스는 `Program.cs`에서 인스턴스화되고 `ProcessMain`으로 전달
- 각 서비스는 공개 인터페이스(예: `ITelegramService`)와 구체 구현을 가짐

### 시간 기반 이벤트
- `TimeCheckerService`는 백그라운드 루프에서 실행
- 두 가지 이벤트 발생: `OnHourReached` (매시간), `On10MinReached` (10분마다)
- `ProcessMain`의 이벤트 핸들러가 적절한 작업 서비스 호출

### 설정
- Constants는 `Constants.cs`에 중앙화되어 있으며 관심사별 정리 (Telegram, Stock, RSS, Schedule)
- 환경변수로 기본값 오버라이드 가능:
  - `TELEGRAM_BOT_TOKEN` - Telegram 봇 토큰
  - `TELEGRAM_CHAT_ID` - 메시지 전송할 Telegram 채팅 ID
  - 로컬 테스트용 기본값은 코드에 있음
- RSS 피드 URL은 `Constants.Rss.FeedUrls`에 하드코딩

### 로깅
- 각 서비스 클래스의 커스텀 정적 `Log()` 메서드 (의존성 주입 아님)
- 간단한 문자열 포맷팅으로 콘솔에 로깅
- 예: `Log("메시지 전송: {preview}...")`

### 에러 처리
- 대부분의 작업은 예외를 캐치하고 로그 기록; 우아한 성능 저하
- 예외가 전파되지 않음; 한 작업이 실패해도 서비스는 계속 실행

### Gmail API
- 부분적으로 설정되었으나 완전히 통합되지 않음
- 설정 지침은 `GMAIL_API_SETUP.md`에 있음
- `credentials.json`과 `token.json`은 gitignored

## 중요 사항

### 민감한 데이터
- 봇 토큰과 채팅 ID는 코드에 기본값이 있음 (프로덕션에서는 환경변수만 사용하도록 변경 권장)
- `credentials.json`은 gitignored이며 Gmail API 사용을 위해 수동으로 추가해야 함

### 테스트
- 코드베이스에 단위 테스트 없음
- 기능 테스트는 수동 (메시지 전송, 알림 검증)

### 일반적인 수정 사항
- **RSS 피드 추가**: `Constants.Rss.FeedUrls` 목록 수정
- **모니터링 주식 변경**: `Constants.Stock.MonitoringStockCode` 업데이트
- **예약된 시간 조정**: `Constants.Schedule`의 시간 상수 수정
- **새 서비스 추가**: 인터페이스 생성, 구현 작성, `Program.cs`에서 인스턴스화, `ProcessMain`으로 주입

### 배포 워크플로우
1. `master` 브랜치로 변경 사항 커밋
2. GitHub Actions이 자동으로 SSH를 통해 배포
3. 서버에서 `deploy.sh`를 실행하여 컨테이너 재빌드 및 재시작
4. 서버에서 `docker logs heynowbot-service`로 로그 확인
