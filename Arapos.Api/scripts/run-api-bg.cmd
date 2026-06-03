@echo off
cd /d "C:\Users\aya.rahall\Desktop\ARAPOS API"
set DOTNET_CLI_HOME=C:\Users\aya.rahall\Desktop\ARAPOS API\.dotnet-home
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
start "AyaPOS.Api" /B "C:\Program Files\dotnet\dotnet.exe" run --project "Arapos.Api\AyaPOS.Api\AyaPOS.Api.csproj" --launch-profile http --no-build > "Arapos.Api\api-live.out.log" 2> "Arapos.Api\api-live.err.log"
