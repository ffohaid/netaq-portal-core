FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Install Arabic fonts for PDF export support (Sprint 6 - AC-5)
RUN apt-get update && apt-get install -y --no-install-recommends \
    fonts-hosny-amiri \
    fonts-kacst \
    fontconfig \
    && fc-cache -fv \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Netaq.sln", "."]
COPY ["src/Netaq.Domain/Netaq.Domain.csproj", "src/Netaq.Domain/"]
COPY ["src/Netaq.Application/Netaq.Application.csproj", "src/Netaq.Application/"]
COPY ["src/Netaq.Infrastructure/Netaq.Infrastructure.csproj", "src/Netaq.Infrastructure/"]
COPY ["src/Netaq.Api/Netaq.Api.csproj", "src/Netaq.Api/"]

# Restore dependencies
RUN dotnet restore "src/Netaq.Api/Netaq.Api.csproj"

# Copy all source code
COPY src/ src/

# Build
WORKDIR "/src/src/Netaq.Api"
RUN dotnet build "Netaq.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "Netaq.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Netaq.Api.dll"]
