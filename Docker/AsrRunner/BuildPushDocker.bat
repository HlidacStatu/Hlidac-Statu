@echo off

if '%1'=='' goto argumentError

dotnet publish -o ./bin/dockerPublish
docker build -f dockerfile.publish -t hlidacstatu/asr-runner:%1 .
docker push hlidacstatu/asr-runner:%1

goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version>
  echo:
  echo    - build version
  echo:

  goto :EOF
