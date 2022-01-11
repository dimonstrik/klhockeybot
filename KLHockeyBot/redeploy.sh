service klhockeybot stop
systemctl daemon-reload
dotnet publish -o /klhockeybot/bin/
service klhockeybot start
journalctl --unit klhockeybot --follow
