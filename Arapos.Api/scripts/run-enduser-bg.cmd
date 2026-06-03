@echo off
cd /d "C:\Users\aya.rahall\Desktop\ARAPOS API"
set DOTNET_CLI_HOME=C:\Users\aya.rahall\Desktop\ARAPOS API\.dotnet-home
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
start "AyaPOS.EndUser" /B "C:\Program Files\dotnet\dotnet.exe" run --project "AyaPOS.EndUser\AyaPOS.EndUser.csproj" --launch-profile http --no-build > "Arapos.Api\enduser-live.out.log" 2> "Arapos.Api\enduser-live.err.log"
