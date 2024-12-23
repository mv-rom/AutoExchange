@echo off

echo "Post build deployment .."
echo %1
echo %2

if exist %1\..\..\ae.jsonconfig (
copy /V /Y %1\..\..\ae.jsonconfig %1
)
rem copy /V /Y %2\libframework\*.* %1\libs

rem copy 1c reports
set reports1cDir="%1\..\Services\EDI\Reports1C\"
mkdir %reports1cDir%
del %reports1cDir%\*.ert
copy /V /Y %2\ae\services\EDI\reports1c\*.ert %reports1cDir%
echo ".. is complete."
