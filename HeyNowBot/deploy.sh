#!/bin/bash

# =================================================================
# 1. 설정 변수
# ** 이 값만 수정하면 됩니다. Dockerfile과 일치해야 합니다. **
# =================================================================
IMAGE_NAME="heynowbot-app"
CONTAINER_NAME="heynowbot-service"
PROJECT_NAME="HeyNowBot" 

# -----------------------------------------------------------------
# ** [중요 수정] 줄 끝 문자 문제 해결 **
# (이전에 sed나 dos2unix를 사용했다면 이 코드는 없어도 되지만, 혹시 모를 재발 방지)
sed -i 's/\r//g' "$0" 2> /dev/null
# -----------------------------------------------------------------

# 현재 디렉토리(.csproj와 Dockerfile이 있는 위치)로 이동
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$SCRIPT_DIR"

echo "--- 1. 기존 컨테이너 중지 및 삭제 시작 ---"

# 실행 중인 컨테이너가 있으면 중지하고 삭제
docker stop $CONTAINER_NAME 2> /dev/null
docker rm $CONTAINER_NAME 2> /dev/null

echo "✅ 1. 기존 컨테이너 정리 완료"

# =================================================================
# 2. 도커 이미지 재빌드
# =================================================================
echo "--- 2. 도커 이미지 빌드 시작 ($IMAGE_NAME) ---"

# ** [핵심 수정] PROJECT_NAME 변수를 ARG로 전달하여 빌드 **
# 이 부분은 필수! Dockerfile의 ARG PROJECT_NAME을 사용합니다.
docker build --build-arg PROJECT_NAME=$PROJECT_NAME -t $IMAGE_NAME .

# 빌드 성공 여부 확인
if [ $? -ne 0 ]; then
    echo "❌ 2. 도커 이미지 빌드 실패. 스크립트를 종료합니다."
    exit 1
fi

echo "✅ 2. 도커 이미지 빌드 성공"

# =================================================================
# 3. 새로운 컨테이너 실행
# =================================================================
echo "--- 3. 새로운 컨테이너 실행 시작 ($CONTAINER_NAME) ---"

# 기존과 동일하게 백그라운드 및 자동 재시작 설정
docker run -d \
--restart=always \
-v /etc/localtime:/etc/localtime:ro \
-v /etc/timezone:/etc/timezone:ro \
--name $CONTAINER_NAME \
$IMAGE_NAME

echo "✅ 3. 새로운 컨테이너 실행 성공"

# =================================================================
# 4. 상태 확인
# =================================================================
echo "--- 4. 컨테이너 상태 확인 ---"
docker ps -f name=$CONTAINER_NAME
echo "--- 5. 최신 로그 확인 (5초 후 Ctrl+C로 종료 가능) ---"
sleep 5
docker logs --tail 20 $CONTAINER_NAME

echo "🎉 배포 및 재실행 완료."