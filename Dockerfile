FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RedisMemoryUsage/RedisMemoryUsage.csproj", "RedisMemoryUsage/"]
RUN dotnet restore "RedisMemoryUsage/RedisMemoryUsage.csproj"
COPY . .
WORKDIR "/src/RedisMemoryUsage"
RUN dotnet build "RedisMemoryUsage.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RedisMemoryUsage.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedisMemoryUsage.dll"]
