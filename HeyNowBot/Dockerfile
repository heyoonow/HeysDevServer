# --------------------------------------------------------------------------------------
# 🌟 프로젝트 이름 변수 선언 (최상단) 🌟
ARG PROJECT_NAME="HeyNowBot"
# --------------------------------------------------------------------------------------

# 이 스테이지는 VS에서 빠른 모드로 실행할 때 사용됩니다(디버그 구성의 기본값).
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app


# 이 스테이지는 서비스 프로젝트를 빌드하는 데 사용됩니다.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG PROJECT_NAME
WORKDIR /src

COPY ["${PROJECT_NAME}.csproj", "."]
RUN dotnet restore "${PROJECT_NAME}.csproj"

COPY . .
RUN dotnet build "${PROJECT_NAME}.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG PROJECT_NAME
RUN dotnet publish "${PROJECT_NAME}.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# --------------------- ❗ 여기부터 수정됨 ---------------------------------
FROM base AS final
ARG PROJECT_NAME
WORKDIR /app

# 🔥 final 스테이지에서 반드시 다시 선언해야 함
ENV DLL_NAME="${PROJECT_NAME}.dll"

COPY --from=publish /app/publish .

# 🔥 쉘에서 확장되도록 함
ENTRYPOINT ["sh", "-c", "dotnet \"$DLL_NAME\""]
# -------------------------------------------------------------------------------
