FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
# solves SQL server 2012 handshake problem
RUN sed -i 's/TLSv1.2/TLSv1.0/g' /etc/ssl/openssl.cnf 
RUN apt-get update && apt-get install -y curl

#folder path from caches
COPY bin/dockerPublish/ .

ENTRYPOINT ["dotnet", "HlidacStatu.AutocompleteApi.dll"]