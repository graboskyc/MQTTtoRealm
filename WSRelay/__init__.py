import tornado.ioloop
import tornado.web 
import tornado.websocket
from paho.mqtt import client as mqtt_client
import random
import time

broker = '192.168.2.251'
port = 1883
topic = "/python/mqtt"
client_id = f'python-mqtt-{random.randint(0, 1000)}'

def connect_mqtt():
    def on_connect(client, userdata, flags, rc):
        if rc == 0:
            print("Connected to MQTT Broker!")
        else:
            print("Failed to connect, return code %d\n", rc)
    # Set Connecting Client ID
    client = mqtt_client.Client(client_id)
    client.on_connect = on_connect
    client.connect(broker, port)
    return client

class WebSockHandler(tornado.websocket.WebSocketHandler):
    def open(self):
        print("New client connected")
        self.write_message("You are connected")

    def on_message(self, msg):
        print(msg)
        #self.write_message(msg)
        # oh man this is bad practice
        m = connect_mqtt()
        result = m.publish(topic, msg)
        print(result)

    def on_close(self):
        print("Client disconnected")

    def check_origin(self, origin):
        # who cares about security
        return True

if __name__ == "__main__":
    appsoc = tornado.web.Application([(r"/", WebSockHandler),],)
    appsoc.listen(5000)
    tornado.ioloop.IOLoop.current().start()