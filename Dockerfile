# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base

# Create a user and group with the specified PUID and PGID
ARG PUID=1000
ARG PGID=1000

RUN groupadd -g $PGID appgroup && \
    useradd -u $PUID -g $PGID -m appuser

# Switch to the new user
USER appuser

# Your application setup and commands
WORKDIR /app

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["account-bonus-supporter/account-bonus-supporter.csproj", "account-bonus-supporter/"]
RUN dotnet restore "./account-bonus-supporter/account-bonus-supporter.csproj"
COPY . .
WORKDIR "/src/account-bonus-supporter"
RUN dotnet build "./account-bonus-supporter.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./account-bonus-supporter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "account-bonus-supporter.dll"]