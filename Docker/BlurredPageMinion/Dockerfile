#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Using custom image based on https://github.com/shimat/opencvsharp/blob/master/docker/ubuntu20-dotnet6-opencv4.6.0/Dockerfile
FROM hlidacstatu/opencvbase AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS build
WORKDIR /src
COPY ["Hlidac-Statu/Docker/BlurredPageMinion/BlurredPageMinion.csproj", "Hlidac-Statu/Docker/BlurredPageMinion/"]
RUN dotnet restore "Hlidac-Statu/Docker/BlurredPageMinion/BlurredPageMinion.csproj"
COPY . .
WORKDIR "/src/Hlidac-Statu/Docker/BlurredPageMinion"
RUN dotnet build "BlurredPageMinion.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlurredPageMinion.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN chmod +x /app/linux-x64/AnalyzePageTextAreaCmd
ENTRYPOINT ["dotnet", "BlurredPageMinion.dll"]