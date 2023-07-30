#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Console/Console.csproj", "Console/"]
COPY ["PDFtoImage/PDFtoImage.csproj", "PDFtoImage/"]
RUN dotnet restore "Console/Console.csproj"
COPY . .
WORKDIR "/src/Console"
RUN dotnet build "Console.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PDFtoImage.Console.dll"]