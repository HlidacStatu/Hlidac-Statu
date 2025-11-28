@echo off

if '%1'=='' goto argumentError


docker build -f Dockerfilepublish -t hlidacstatu/webgenerator:%1 .


docker push hlidacstatu/webgenerator:%1

if '%2' =='release' (
	docker tag hlidacstatu/webgenerator:%1 hlidacstatu/webgenerator:latestRelease
	docker push hlidacstatu/webgenerator:latestRelease
)

goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version>
  echo:
  echo    - build version
  echo:

  goto :EOF
