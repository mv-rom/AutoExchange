@echo off

echo %1
echo %2
copy /V /Y %1\..\..\ae.jsonconfig %1
copy /V /Y %2\libframework\*.* %1\libs
