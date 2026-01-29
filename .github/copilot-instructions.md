# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction

## Code Style
- Use specific formatting rules
- Follow naming conventions
- When outputting console logs, always include the current time in the format: yyyy-MM-dd HH:mm:ss. (사용자 승인: 콘솔 로그를 출력할 때는 항상 yyyy-MM-dd HH:mm:ss 타임스탬프를 포함한다.)

## Project-Specific Rules
- ProcessMain에서는 스케줄(10/30/60분) 트리거가 동시에 발생할 수 있어도, 텔레그램 메시지는 반드시 한 번으로 합쳐서 보내고(배치 전송), 트리거 자체를 억제하지 않는다.