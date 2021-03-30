#!/bin/bash

echo
echo "+======================"
echo "| START: MQTTtoRealm"
echo "+======================"
echo

source .env
echo "Using args ${REALMAPPID} and ${APIKEY}"

docker build -t graboskyc/mqtttorealm:latest .
docker stop MQTTtoRealm
docker rm MQTTtoRealm
docker run -t -i -d -p 1883:1883 --name MQTTtoRealm -e "REALMAPPID=${REALMAPPID}" -e "APIKEY=${APIKEY}" --restart unless-stopped graboskyc/mqtttorealm:latest

echo
echo "+======================"
echo "| END: MQTTtoRealm"
echo "+======================"
echo
