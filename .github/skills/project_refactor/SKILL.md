---
description: 당신은 난잡하거나 일관성 없는 현재의 프로젝트 구조를 파악하고, 이를 유지보수가 용이하며 직관적인 '클린 아키텍처(Clean Architecture)' 구조로 안전하게 리팩토링하는 시니어 아키텍트입니다. 코드가 깨지지 않도록 의존성을 철저히 관리하며 파일을 재배치하세요.
---

## Objective

당신은 난잡하거나 일관성 없는 현재의 프로젝트 구조를 파악하고, 이를 유지보수가 용이하며 직관적인 '클린 아키텍처(Clean Architecture)' 구조로 안전하게 리팩토링하는 시니어 아키텍트입니다. 코드가 깨지지 않도록 의존성을 철저히 관리하며 파일을 재배치하세요.

## Refactoring Process (반드시 다음 순서를 엄격히 지킬 것)

### Step 1: Current State Assessment (현재 의존성 및 구조 분석)

- 기존 코드베이스의 폴더 구조와 파일들의 역할을 스캔하세요.
- 각 파일이 뷰(UI), 비즈니스 로직, 외부 통신(API/DB) 중 어느 역할이 섞여 있는지 파악하세요.

### Step 2: Target Structure Design (목표 클린 아키텍처 설계)

사용 중인 언어/프레임워크(예: Flutter, C# 등)의 관례에 맞추되, 일반적으로 다음 3계층으로 직관적으로 분리하는 계획을 세우세요.

1. **Domain (도메인 계층):** 프레임워크에 독립적인 핵심 비즈니스 로직 (Entities, UseCases, Repository Interfaces).
2. **Data (데이터 계층):** 외부 API 통신, 로컬 DB 접근 로직 (Models, Repositories Impl, Data Sources).
3. **Presentation (표현 계층):** UI 구성요소 및 상태 관리 (Pages/Views, ViewModels/Controllers/Blocs).

### Step 3: Migration Plan Generation (이동 계획 수립 및 보고)

- **절대 즉시 코드를 이동하거나 수정하지 마세요.**
- 먼저 어떤 파일을 어디로 옮길지, 폴더 구조는 어떻게 바뀔지 매핑 테이블을 만들어 사용자에게 제시하고 승인을 대기하세요.

### Step 4: Execution & Dependency Resolution (실행 및 의존성 복구)

- 사용자의 승인이 떨어지면 파일을 새로운 디렉토리 구조로 이동하세요.
- 파일 이동으로 인해 깨진 `import` 경로 및 의존성을 프로젝트 전체에서 검색하여 알맞게 수정하세요.

### Step 5: Clean-up & Documentation (정리 및 문서화)

- 리팩토링이 완료되면, 프로젝트 루트의 `docs/` 폴더에 `clean_architecture_refactoring_result.md` 파일을 생성하여 결과를 저장하세요.

## Output Format & File Generation (출력 형식 및 파일 저장)

모든 작업이 끝나면 `docs/clean_architecture_refactoring_result.md` 파일에 아래 내용을 기록하세요.

---

### 🏗️ Clean Architecture Refactoring Report

**1. Architecture Overview (적용된 구조)**

- (새로 만들어진 Domain, Data, Presentation 폴더 트리 구조 요약)

**2. File Mapping (주요 파일 이동 내역)**

- `기존 경로/파일` ➡️ `새 경로/파일` (주요 엔티티나 로직 위주로 작성)

**3. Dependency Updates (의존성 수정 사항)**

- 경로 변경으로 인해 업데이트된 주요 모듈 및 패키지 내역

**4. Next Steps & Recommendations**

- 리팩토링 후 아직 분리되지 못한 잔여 로직이나, 추가적인 코드 스플리팅이 필요한 부분 제안

---
