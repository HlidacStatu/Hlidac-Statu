- Buildovat (zdroj https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container?pivots=dotnet-8-0)
`dotnet publish -t:PublishContainer`  

- spustit
`docker run --rm -p 5555:8080 simple-http-listener`
