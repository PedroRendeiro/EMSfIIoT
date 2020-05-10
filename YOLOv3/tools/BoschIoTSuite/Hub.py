import paho.mqtt.client as mqtt

import datetime, threading, time, sys, configparser, logging, subprocess

from .DittoSerializer import DittoSerializer
from .EMSfIIoT_Gateway import EMSfIIoT_Gateway
from ..image_acquisition import ImageAcquisition

import lite

class Hub():
    """
    MQTT Class

    Configures and starts MQTT communication

    Attributes:
     	tenantId: ID necessary for Bosch IoT Suite MQTT communication
        hub_adapter_host: MQTT broker URL
        certificatePath: MQTT TLS certificate path
        thingId: Thing ID for user and MQTT payload
        authId: Auth ID for user and MQTT payload
        device_password: Password for MQTT authentication
        ditto_topic: MQTT topic to publish
        clientId: Client ID for MQTT communication
        username: username for MQTT communication
        ImageClassification: ImageClassification Class
        infomodel: InformationModel Class
        ser: Serializer Class
        next_call: Time to next MQTT message
        timePeriod: Time between MQTT messages
        telemetryTopic: MQTT telemetry topic to publish in
        client: MQTT Client
    """

    def __init__(self, devices):
        """
        Class constructor

        Reads configuration file and sets variables.
        Starts MQTT loop.
        """
        self.log = logging.getLogger('EMSfIIoT')

        config = configparser.ConfigParser()
        config.read("cfg/emsfiiot.cfg")

        # DEVICE CONFIG GOES HERE
        self.tenantId = config['hub']['tenantId']
        self.hub_adapter_host = config['hub']['hub_adapter_host']
        self.certificatePath = config['hub']['certificatePath']
        self.thingId = config['hub']['thingId']
        self.authId = config['hub']['authId']
        self.device_password = config['hub']['device_password']
        self.ditto_topic = config['hub']['ditto_topic']
        self.clientId = self.thingId
        self.username = self.authId + "@" + self.tenantId

        self.YoloModel = lite.YOLO_lite()
        self.ImageAcquisition = ImageAcquisition()
        self.YoloModel.detect_image("example/capture.jpg")

        # Initialization of Information Model
        self.infomodel = EMSfIIoT_Gateway()

        # Create a serializer for the MQTT payload from the Information Model
        self.ser = DittoSerializer()

        # Period for publishing data to the MQTT broker in seconds
        self.timePeriod = int(config['hub']['timePeriod'])

        # Configuration of client ID and publish topic	
        self.telemetryTopic = "telemetry/" + self.tenantId + "/" + self.thingId

        # Create the MQTT client
        self.client = mqtt.Client(self.clientId)
        self.client.on_connect = self.on_connect
        self.client.on_disconnect = self.on_disconnect
        self.client.on_message = self.on_message
        self.client.on_publish = self.on_publish
        self.client.on_subscribe = self.on_subscribe
        self.client.on_log = self.on_log

        # Output relevant information for consumers of our information
        print("Connecting client:    ", self.clientId)
        print("Publishing to topic:  ", self.telemetryTopic)

        self.client.username_pw_set(self.username, self.device_password)
        self.client.tls_set(self.certificatePath)

        # Connect to the MQTT broker
        self.client.connect(self.hub_adapter_host, 8883, keepalive=30)

        self.devices = devices

    def start(self):
        # Non blocking call that processes network traffic, dispatches callbacks and
        # handles reconnecting.
        self.client.loop_start()

        # Start the periodic task for publishing MQTT messages
        self.status = True
        self.periodicAction()
    
    def stop(self):
        self.client.disconnect()
        self.client.loop_stop()
        self.timer.cancel()
    
    # The callback for when the client receives a CONNACK response from the server.
    def on_connect(self, client, userdata, flags, rc):
        """
        Function to run on when connection is established to MQTT broker

        Subscribes command topic
        Sets periodic publication
        """
        if rc != 0:
            print("Connection to MQTT broker failed: " + str(rc))
            return        

        # Subscribing in on_connect() means that if we lose the connection and
        # reconnect then subscriptions will be renewed.
        self.client.subscribe("command/+/+/req/#")

    def on_disconnect(self, client, userdata, rc):
        if rc != 0:
            print("Unexpected MQTT disconnection. Will auto-reconnect")
            self.client.reconnect()

    def on_message(self, client, userdata, msg):
        """
        Function to run on when message is received
    
        Args:
            client: MQTT client
            userdata: User who published the message on broker
            msg: MQTT message payload

        """
        
        self.log.info("Message received! Payload: " + str(msg.payload) + " from Topic: " + msg.topic)

        msgTopic = msg.topic[msg.topic.rfind("/")+1:]

        # parse Bosch IoT Hub's message ID for response
        messageId = msg.topic[msg.topic.find("req/")+4:]
        messageId = messageId[0:messageId.find("/")]

        # if this is a 2-way command, respond
        if (messageId != ""):
            print("Sender expects reply, responding to message with ID: " + messageId)

            # create MQTT response topic
            resTopic = "command///res/" + messageId + "/200"

            # parse Bosch IoT Things correlation ID for response
            reqPayload = str(msg.payload)

            # 17 is the length of 'correlation-id' and subsequent '":"'
            correlationId = reqPayload[reqPayload.find("correlation-id")+17:reqPayload.find("correlation-id")+17+36]

            print("Correlation-Id: " + correlationId)

            #self.periodicAction()

            # create Ditto compliant MQTT response payload
            resPayload = "{\"topic\":\"" + self.ditto_topic + "/things/live/messages/" + msgTopic + "\","
            resPayload += "\"headers\":{\"correlation-id\":\"" + correlationId + "\","
            resPayload += "\"version\":2,\"content-type\":\"text/plain\"},"
            resPayload += "\"path\":\"/inbox/messages/" + msgTopic +"\","

            if msgTopic == "start" and self.status == False:
                resPayload += "\"value\":\"" + "Start done" + "\","
                resPayload += "\"status\": 200 }"
                self.client.publish(resTopic, resPayload)
                self.log.info("Response published! Payload: " + str(resPayload) + " to Topic: " + resTopic)
                
                self.status = True
                self.log.info("Loop start")
                self.timer = threading.Timer(1, self.periodicAction)
                self.timer.start()
            elif msgTopic == "stop" and self.status == True:
                resPayload += "\"value\":\"" + "Stop done" + "\","
                resPayload += "\"status\": 200 }"
                self.client.publish(resTopic, resPayload)
                self.log.info("Response published! Payload: " + str(resPayload) + " to Topic: " + resTopic)

                self.status = False
                self.log.info("Loop stop")
                self.timer.cancel()
            elif msgTopic == "restart":
                resPayload += "\"value\":\"" + "Restart done" + "\","
                resPayload += "\"status\": 200 }"
                self.client.publish(resTopic, resPayload)
                self.log.info("Response published! Payload: " + str(resPayload) + " to Topic: " + resTopic)

                command = ['service', 'EMSfIIoT', 'restart']
                self.log.info('Executing: %s' % command)
                subprocess.call(command, shell=True)
            else:
                resPayload += "\"value\":\"" + "Bad Request" + "\","
                resPayload += "\"status\": 400 }"
                self.client.publish(resTopic, resPayload)
                self.log.info("Response published! Payload: " + str(resPayload) + " to Topic: " + resTopic)

    def on_publish(self, client, userdata, mid):
        pass

    def on_subscribe(self, client, userdata, mid, granted_qos):
        print("Subscribed: "+str(mid)+" "+str(granted_qos))

    def on_log(self, client, userdata, level, string):
        self.log.info(string)
    
    # The functions to publish the functionblocks data
    def publishGenericsensor(self):
        """
        Function to publish data to MQTT broker

        Generates payload from data in infomodel
        """
        payload = self.ser.serialize_functionblock("ESP32_CAM", self.infomodel, self.ditto_topic, self.thingId)
        self.client.publish(self.telemetryTopic, payload)
        self.log.info("Publish Payload: " + payload + " to Topic: " + self.telemetryTopic)
    
    # The function that will be executed periodically once the connection to the MQTT broker was established
    def periodicAction(self):
        """

        Function to run periodicaly

        Calls ImageClassification and sets current date and time.
        Publish to topic.
        Schedule next call.
        """

        print("Reading data...")
        
        L = ["181", "182", "183"]
        for device in self.devices:
            url = device['url'] + "/capture_with_flash"
            for l in L:
                screen, value = None, None
                read = 1
                n = 0
                while (read == 1):
                    try:
                        image = self.ImageAcquisition.ReadFromURL(url)

                        if image == None:
                            n = n + 1
                            if (n > 10):
                                read = 0
                            continue

                        if (device['locationId'] == 1):
                            _, value, screen = self.YoloModel.detect_image(image)
                        else:
                            _, screen, value = self.YoloModel.detect_image(image)
                            screen = screen[::-1]
                            value = value[::-1]
                        
                        self.log.info("Screen: " + screen + " | Value: " + value)

                        if (screen in [l] and len(str(value)) == 6):
                            read = 2
                            continue
                    
                    except KeyboardInterrupt:
                        print("Exiting...")
                        sys.exit(0)
                    
                    except Exception as e:
                        logging.error(e)
                        continue

                if (read == 2):
                    self.infomodel.value = int(value)
                    self.infomodel.unit = "kWh"
                    self.infomodel.measureTypeID = int(screen[-1])
                    self.infomodel.locationID = device['locationId']

                    now = datetime.datetime.now()
                    self.infomodel.timeStamp = now.strftime("%d-%m-%Y") + "T" + now.strftime("%H:%M:%S")

                    # Publish payload
                    self.publishGenericsensor()

        print("Read done!")

        if self.status:
            # Schedule next call
            self.timer = threading.Timer(self.timePeriod, self.periodicAction)
            self.timer.start()

def main():
    """
    Main function to test class
    """
    try:
        mqtt = MQTT()
        while (1):
            pass
    except KeyboardInterrupt:
        print("Exiting...")
        sys.exit(0)
    else:
        sys.exit(1)
    finally:
        pass
        
# Main body
if __name__ == '__main__':
    main()