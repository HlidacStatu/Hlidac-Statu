@echo off

if '%1'=='' goto argumentError

docker build -f Dockerfile.publish -t hlidacstatu/asr-runner:%1 .
docker push hlidacstatu/asr-runner:%1

goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version>
  echo:
  echo    - build version
  echo:

  goto :EOF
