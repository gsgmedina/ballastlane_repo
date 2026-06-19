# Multi-stage build for TaskManager.Api (build context = repo root)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution-wide files first (better layer caching on restore)
COPY global.json Directory.Build.props ./
COPY src/ ./src/

RUN dotnet restore src/TaskManager.Api/TaskManager.Api.csproj
RUN dotnet publish src/TaskManager.Api/TaskManager.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render routes traffic to this port (its default expected port is 10000)
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]
