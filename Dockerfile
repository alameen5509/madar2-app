FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

WORKDIR /src/Mdar.API

RUN dotnet restore

RUN dotnet publish -c Release -o /app/publish



FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

ENV PORT=8080

WORKDIR /app

ENTRYPOINT ["dotnet", "Mdar.API.dll"]
