FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY SilkHat.sln ./
COPY src/SilkHat.Api/SilkHat.Api.csproj src/SilkHat.Api/
COPY src/SilkHat.Analysis/SilkHat.Analysis.csproj src/SilkHat.Analysis/
COPY src/SilkHat.Code.Analysis/SilkHat.Code.Analysis.csproj src/SilkHat.Code.Analysis/
COPY src/SilkHat.Code.Core/SilkHat.Code.Core.csproj src/SilkHat.Code.Core/
COPY src/SilkHat.Core/SilkHat.Core.csproj src/SilkHat.Core/
COPY src/SilkHat.Git.Analysis/SilkHat.Git.Analysis.csproj src/SilkHat.Git.Analysis/
COPY src/SilkHat.Git.Core/SilkHat.Git.Core.csproj src/SilkHat.Git.Core/
COPY src/SilkHat.Infrastructure/SilkHat.Infrastructure.csproj src/SilkHat.Infrastructure/
RUN dotnet restore src/SilkHat.Api/SilkHat.Api.csproj

COPY . .
WORKDIR /src/src/SilkHat.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends git \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .
COPY docker/api-entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh
ENTRYPOINT ["/app/entrypoint.sh"]
