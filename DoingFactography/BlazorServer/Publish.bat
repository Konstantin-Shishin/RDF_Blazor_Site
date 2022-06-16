ssh kamronis@fact.iis.nsk.su -p 3322  -i id_rsa "kill -9 `pidof BlazorServer` ; cp /var/www/blazorPub/wwwroot/config.xml /var/www/blazorPub/wwwroot/config.xml.bk"
dotnet publish-ssh --os linux --ssh-host fact.iis.nsk.su --ssh-port 3322 --ssh-user kamronis --ssh-keyfile id_rsa --ssh-path /var/www/blazorPub
ssh kamronis@fact.iis.nsk.su -p 3322  -i id_rsa "cd /var/www/blazorPub ; cp wwwroot/config.xml.bk wwwroot/config.xml ; chmod 775 BlazorServer ; nohup ./BlazorServer &"
echo "Публикация завершена!"
pause
