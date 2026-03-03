---
name: git-push
description: 현재 브랜치를 원격 서버(origin)에 푸시. "푸시", "서버에 올려", "git push", "배포" 등의 요청 시 사용.
---

# Git Push 스킬

현재 브랜치의 커밋을 원격 서버(origin)에 푸시한다.

## 절차

1. 현재 상태 확인
   ```bash
   git status
   git log --oneline -5
   ```

2. 미커밋 변경사항이 있으면 먼저 커밋 (git-commit 스킬 활용)

3. 원격 서버에 푸시
   ```bash
   git push origin master
   ```

4. 결과 확인 후 성공/실패 알림

## 주의사항

- push 전 반드시 uncommitted changes 없는지 확인
- 강제 푸시(`--force`)는 절대 사용 금지
- 푸시 성공 시 GitHub Actions CI/CD가 자동으로 서버 배포 시작
