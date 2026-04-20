# ── Etapa 1: Build ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar ambos proyectos (el cliente referencia el proto del servidor)
COPY ["src/GrpcServer/GrpcServer.csproj", "src/GrpcServer/"]
COPY ["src/GrpcClient/GrpcClient.csproj", "src/GrpcClient/"]
RUN dotnet restore "src/GrpcClient/GrpcClient.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar en modo Release
WORKDIR "/src/src/GrpcClient"
RUN dotnet publish "GrpcClient.csproj" -c Release -o /app/publish \
    --no-restore

# ── Etapa 2: Runtime ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# API REST en el puerto 5100
EXPOSE 5100

ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "GrpcClient.dll"]
