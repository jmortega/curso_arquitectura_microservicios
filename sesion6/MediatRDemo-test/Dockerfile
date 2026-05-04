# ── Stage 1: Build ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar primero (capa de caché)
COPY ["src/MediatRDemo.API/MediatRDemo.API.csproj", "src/MediatRDemo.API/"]
RUN dotnet restore "src/MediatRDemo.API/MediatRDemo.API.csproj"

# Copiar el resto del código
COPY . .

# Publicar en modo Release
WORKDIR "/src/src/MediatRDemo.API"
RUN dotnet publish "MediatRDemo.API.csproj" -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Instalar curl para el healthcheck
RUN apt-get update && apt-get install -y curl --no-install-recommends && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "MediatRDemo.API.dll"]
