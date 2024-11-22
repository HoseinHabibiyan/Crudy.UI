FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY . .
WORKDIR /app/Crudy.UI

RUN dotnet restore
RUN dotnet publish -c Release -o out /p:UseAppHost=false

FROM nginx:1.26.2-alpine-slim
WORKDIR /usr/share/nginx/html
COPY --from=build /app/Crudy.UI/out/wwwroot .
COPY nginx.conf /etc/nginx/nginx.conf

EXPOSE 3000
