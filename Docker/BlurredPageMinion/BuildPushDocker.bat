@echo off

if '%1'=='' goto argumentError

call BuildPushDockerX64.bat %1 %2

rem call BuildPushDockerArm.bat %1


goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version> [release]
  echo:
  echo    - build version
  echo:   - with release parameter create latestRelease tag		

  goto :EOF
