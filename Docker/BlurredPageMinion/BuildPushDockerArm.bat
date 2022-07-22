@echo off

if '%1'=='' goto argumentError


docker build -f DockerfileFromPublishArm -t hlidacstatu/blurred_page_minion:%1-arm .

REM docker scan hlidacstatu/blurred_page_minion:%1

docker push hlidacstatu/blurred_page_minion:%1-arm
REM docker push hlidacstatu/blurred_page_minion:latest-arm

goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version>
  echo:
  echo    - build version
  echo:

  goto :EOF
