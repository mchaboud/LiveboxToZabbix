#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM amd64/alpine:latest
RUN apk upgrade --no-cache && apk add --no-cache  openssl libgcc libstdc++ ncurses-libs libc6-compat gcompat icu-libs
WORKDIR /app
COPY bin/Publish/net7.0/LiveboxToZabbix .
ENTRYPOINT ["/app/LiveboxToZabbix"]

#Reminder how to generate the package and export it for dsm
#cd LiveboxToZabbix
#docker build -t livebox-to-zabbix .
#docker save livebox-to-zabbix > livebox-to-zabbix.tar
#docker run livebox-to-zabbix
#sudo cp /home/morgan/LiveboxToZabbix/livebox-to-zabbix.tar /mnt/hgfs/Vrac/livebox-to-zabbix.tar