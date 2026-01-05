FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY SilkHat.sln ./
COPY src/SilkHat.Ui/SilkHat.Ui.csproj src/SilkHat.Ui/
RUN dotnet restore src/SilkHat.Ui/SilkHat.Ui.csproj

COPY . .
WORKDIR /src/src/SilkHat.Ui
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM nginx:alpine
WORKDIR /usr/share/nginx/html
COPY src/SilkHat.Ui/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot .
EXPOSE 80
