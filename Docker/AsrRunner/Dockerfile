#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Hlidac-Statu/nuget.config", "Hlidac-Statu/"]
COPY ["Hlidac-Statu/Docker/AsrRunner/AsrRunner.csproj", "Hlidac-Statu/Docker/AsrRunner/"]
RUN dotnet restore "Hlidac-Statu/Docker/AsrRunner/AsrRunner.csproj"
COPY . .
WORKDIR "/src/Hlidac-Statu/Docker/AsrRunner"
RUN dotnet build "AsrRunner.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AsrRunner.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AsrRunner.dll"]