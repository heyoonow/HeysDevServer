# Copilot Skills & Agents

이 파일은 GitHub Copilot CLI가 사용할 수 있는 커스텀 스킬과 에이전트를 정의합니다.

## 스킬 목록

### git-commit 스킬
커밋 메시지 작성 가이드 및 정책

- **파일**: `.github/skills/git-commit.md`
- **목적**: 논리적으로 변경사항을 그룹화하여 정갈한 커밋 생성
- **특징**:
  - [tag] 형식의 의미있는 커밋 메시지
  - 한글 기반 작성
  - 자동 Co-authored-by 트레일러 추가
