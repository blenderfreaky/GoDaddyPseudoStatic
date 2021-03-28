cd GoDaddyPseudoStatic
sc stop GoDaddyIPUpdater
dotnet restore
dotnet publish -c Release -o "C:/Program Files/GoDaddyIPUpdater"
sc create GoDaddyIPUpdater binpath="C:/Program Files/GoDaddyIPUpdater/GoDaddyPseudoStatic.exe"
sc config GoDaddyIPUpdater start=auto
sc start GoDaddyIPUpdater
cd ..