# AutoExchange

### Add net4.0 ReferenceAssemblies like install package:
> https://www.nuget.org/packages/microsoft.netframework.referenceassemblies.net40
```
dotnet add package Microsoft.NETFramework.ReferenceAssemblies.net40 --version 1.0.3
```
> or using NuGet:
```
NuGet\Install-Package Microsoft.NETFramework.ReferenceAssemblies.net40 -Version 1.0.3
```

### or Add net4.0 ReferenceAssemblies (net4.0_targeting_pack) through install VS 2019:
> https://www.techspot.com/downloads/7241-visual-studio-2019.html


### Case to use V77.Application:
```
regsvr32 "C:\Program Files (x86)\1Cv77\BIN\v7plus.dll"
regsvr32 "C:\Program Files (x86)\1Cv77\BIN\v7chart.dll"
regsvr32 "C:\Program Files (x86)\1Cv77\BIN\zlibeng.dll"
```

### Follow these instructions to compile the code:
```
git clone https://github.com/mv-rom/AutoExchange.git
cd AutoExchange/src
start compile.bat
```

### Follow these instructions to run the code:
```
cd AutoExchange/Run
ae_start.exe
```
