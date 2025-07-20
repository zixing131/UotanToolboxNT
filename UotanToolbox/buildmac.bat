dotnet restore -r osx-arm64
::dotnet msbuild -t:BundleApp -p:RuntimeIdentifier=osx-arm64 -p:Configuration=Release -p:UseAppHost=true -p:SelfContained=true -p:PublishTrimmed=true -p:CreatePackage=true
dotnet publish -c Release -r osx-arm64 --self-contained true /p:UseAppHost=true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:DebugType=None /p:DebugSymbols=false
::sudo codesign -f -s -  --deep /Applications/MuMuPlayer.app
pause
