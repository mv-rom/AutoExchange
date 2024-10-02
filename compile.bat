@echo off

rem set name=%~n0
set ProjectDirPath=%~dp0
set ProjectName=AE
set DotnetPath=C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319
set RefAssemPath=%ProjectDirPath%\libframework

rem dotnet build --configuration Release --arch win-x64

rem https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference?view=vs-2022
rem http://docwiki.embarcadero.com/RADStudio/Alexandria/en/Building_a_Project_Using_an_MSBuild_Command


%DotnetPath%\MSBuild.exe %ProjectName%.sln /nologo /m /target:Rebuild /p:Configuration=Release;FrameworkPathOverride=%DotnetPath%

pause
