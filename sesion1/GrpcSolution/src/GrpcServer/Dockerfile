# ── Etapa 1: Build ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias (capa de caché)
COPY ["src/GrpcServer/GrpcServer.csproj", "src/GrpcServer/"]
RUN dotnet restore "src/GrpcServer/GrpcServer.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar en modo Release
WORKDIR "/src/src/GrpcServer"
RUN dotnet publish "GrpcServer.csproj" -c Release -o /app/publish \
    --no-restore

# ── Etapa 2: Runtime ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar los artefactos publicados
COPY --from=build /app/publish .

# gRPC usa HTTP/2 en el puerto 5200
EXPOSE 5200

# Variables de entorno
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "GrpcServer.dll"]
