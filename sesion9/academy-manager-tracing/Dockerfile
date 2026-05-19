# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/AcademyManager.Domain/AcademyManager.Domain.csproj", "AcademyManager.Domain/"]
COPY ["src/AcademyManager.Application/AcademyManager.Application.csproj", "AcademyManager.Application/"]
COPY ["src/AcademyManager.Infrastructure/AcademyManager.Infrastructure.csproj", "AcademyManager.Infrastructure/"]
COPY ["src/AcademyManager.API/AcademyManager.API.csproj", "AcademyManager.API/"]

RUN dotnet restore "AcademyManager.API/AcademyManager.API.csproj"

COPY src/ .

RUN dotnet build "AcademyManager.API/AcademyManager.API.csproj" -c Release -o /app/build

# ── Stage 2: Publish ──────────────────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish "AcademyManager.API/AcademyManager.API.csproj" \
    -c Release -o /app/publish \
    /p:UseAppHost=false

# ── Stage 3: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app


# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AcademyManager.API.dll"]
