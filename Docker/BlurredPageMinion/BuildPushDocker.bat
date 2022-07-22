@echo off

if '%1'=='' goto argumentError

call BuildPushDockerX64.bat %1

rem call BuildPushDockerArm.bat %1


goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version>
  echo:
  echo    - build version
  echo:

  goto :EOF
