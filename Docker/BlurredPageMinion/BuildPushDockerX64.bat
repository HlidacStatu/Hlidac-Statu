@echo off

if '%1'=='' goto argumentError


docker build -f DockerfileFromPublish -t hlidacstatu/blurred_page_minion:%1 .

REM docker scan hlidacstatu/blurred_page_minion:%1

docker push hlidacstatu/blurred_page_minion:%1
REM docker push hlidacstatu/blurred_page_minion:latest

goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version>
  echo:
  echo    - build version
  echo:

  goto :EOF
