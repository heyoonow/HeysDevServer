# Gmail API를 이용한 메일 전송 설정 가이드

## 1. Google Cloud Console 프로젝트 생성

1. [Google Cloud Console](https://console.cloud.google.com) 접속
2. 프로젝트 생성 또는 기존 프로젝트 선택
3. "HeyNowBot" 등으로 프로젝트명 설정

## 2. Gmail API 활성화

1. 상단 검색창에서 "Gmail API" 검색
2. "Gmail API" 선택 → "활성화" 클릭

## 3. OAuth 2.0 클라이언트 ID 생성

1. 좌측 "API 및 서비스" → "사용자 인증 정보"
2. "사용자 인증 정보 만들기" → "OAuth 클라이언트 ID"
3. 애플리케이션 유형: "데스크톱 애플리케이션"
4. 이름: "HeyNowBot"
5. "만들기" 클릭

## 4. JSON 다운로드

1. 생성된 OAuth 클라이언트 ID의 ?? 버튼 클릭
2. JSON 파일 다운로드
3. 파일명을 `credentials.json`으로 변경
4. 프로젝트 루트 디렉토리에 저장 (`HeyNowBot/credentials.json`)

## 5. .csproj에 파일 포함 설정

```xml
<ItemGroup>
    <None Update="credentials.json">
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

## 6. NuGet 패키지 설치

```powershell
dotnet add package Google.Apis.Gmail.v1 --version 1.61.0
dotnet add package Google.Apis.Auth --version 1.65.0
```

## 처음 실행 시

- 브라우저가 자동으로 열려 Google 계정으로 인증
- "HeyNowBot이 Gmail 계정 관리를 요청합니다" → 승인
- `token.json` 파일이 자동 생성되고 다음부터는 로그인 불필요

---

**보안 주의:**
- `credentials.json`과 `token.json`은 절대 GitHub에 커밋하지 마세요
- `.gitignore`에 추가:
  ```
  credentials.json
  token.json
  ```
