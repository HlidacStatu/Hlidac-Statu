FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
COPY ./bin/dockerPublish/ .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "VolicskyPrukaz.dll"]
