cd GoDaddyPseudoStatic
dotnet restore
dotnet publish -c Release -o "C:/Program Files/GoDaddyIPUpdater"
sc.exe create GoDaddyIPUpdater binpath="C:/Program Files/GoDaddyIPUpdater/GoDaddyPseudoStatic.exe"
sc.exe config GoDaddyIPUpdater start=auto
sc.exe start GoDaddyIPUpdater
cd ..