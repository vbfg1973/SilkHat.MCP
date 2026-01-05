FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY SilkHat.sln ./
COPY src/SilkHat.Api/SilkHat.Api.csproj src/SilkHat.Api/
RUN dotnet restore src/SilkHat.Api/SilkHat.Api.csproj

COPY . .
WORKDIR /src/src/SilkHat.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SilkHat.Api.dll"]
