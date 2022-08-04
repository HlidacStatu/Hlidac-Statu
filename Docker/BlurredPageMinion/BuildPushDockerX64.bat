@echo off

if '%1'=='' goto argumentError


docker build -f DockerfileFromPublish -t hlidacstatu/blurred_page_minion:%1 .

REM docker scan hlidacstatu/blurred_page_minion:%1

docker push hlidacstatu/blurred_page_minion:%1
REM docker push hlidacstatu/blurred_page_minion:latest

if '%2' =='release' (
	docker tag hlidacstatu/blurred_page_minion:%1 hlidacstatu/blurred_page_minion:latestRelease
	docker push hlidacstatu/blurred_page_minion:latestRelease
)

goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version>
  echo:
  echo    - build version
  echo:

  goto :EOF
