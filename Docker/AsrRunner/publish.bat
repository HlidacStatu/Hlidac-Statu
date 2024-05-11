

if '%1'=='' goto argumentError

dotnet clean
dotnet publish -o ./bin/dockerPublish

cd ./bin/dockerPublish
call BuildPushDocker.bat %1 %2

cd ../..

goto :EOF


:argumentError
  echo:
  echo Usage: BuildPushDocker <version> [release]
  echo:
  echo    - build version
  echo:   - with release parameter create latestRelease tag		

  goto :EOF
