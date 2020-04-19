import paho.mqtt.client as mqtt

import datetime, threading, time, sys, configparser, logging

from .DittoSerializer import DittoSerializer
from .GenericSensor import GenericSensor
from ..image_acquisition import ImageAcquisition

import yolo

class MQTT():
    """
    MQTT Class

    Configures and starts MQTT communication

    Attributes:
     	tenantId: ID necessary for Bosch IoT Suite MQTT communication
        hub_adapter_host: MQTT broker URL
        certificatePath: MQTT TLS certificate path
        deviceId: Device ID for user and MQTT payload
        authId: Auth ID for user and MQTT payload
        device_password: Password for MQTT authentication
        ditto_topic: MQTT topic to publish
        clientId: Client ID for MQTT communication
        username: username for MQTT communication
        readDone: Check if current read is done
        ImageClassification: ImageClassification Class
        infomodel: InformationModel Class
        ser: Serializer Class
        next_call: Time to next MQTT message
        timePeriod: Time between MQTT messages
        publishTopic: MQTT topic to publish in
        client: MQTT Client
    """

    def __init__(self):
        """
        Class constructor

        Reads configuration file and sets variables.
        Starts MQTT loop.
        """
        
        logging.basicConfig(filename='logs/EMSfIIoT.log', level=logging.DEBUG, format='%(asctime)s %(message)s')
        
        config = configparser.ConfigParser()
        config.read("cfg/emsfiiot.cfg")

        # DEVICE CONFIG GOES HERE
        self.tenantId = config['mqtt']['tenantId']
        self.hub_adapter_host = config['mqtt']['hub_adapter_host']
        self.certificatePath = config['mqtt']['certificatePath']
        self.deviceId = config['mqtt']['deviceId']
        self.authId = config['mqtt']['authId']
        self.device_password = config['mqtt']['device_password']
        self.ditto_topic = config['mqtt']['ditto_topic']
        self.clientId = self.deviceId
        self.username = self.authId + "@" + self.tenantId

        self.cameraURL = config['source']['ip']

        self.readDone = False

        self.YoloModel = yolo.YOLO_np()
        self.ImageAcquisition = ImageAcquisition()

        # Initialization of Information Model
        self.infomodel = GenericSensor()

        # Create a serializer for the MQTT payload from the Information Model
        self.ser = DittoSerializer()

        # Timer variable for periodic function
        self.next_call = 0
        # Period for publishing data to the MQTT broker in seconds
        self.timePeriod = 10
        #
        self.thread = None

        # Configuration of client ID and publish topic	
        self.publishTopic = "telemetry/" + self.tenantId + "/" + self.deviceId

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
        print("Publishing to topic:  ", self.publishTopic)

        self.client.username_pw_set(self.username, self.device_password)
        self.client.tls_set(self.certificatePath)

        # Connect to the MQTT broker
        self.client.connect_async(self.hub_adapter_host, 8883, 60)

    def start(self):
        # Blocking call that processes network traffic, dispatches callbacks and
        # handles reconnecting.
        # Other loop*() functions are available that give a threaded interface and a
        # manual interface.
        self.client.loop_start()
    
    def stop(self):
        self.client.disconnect()
        self.client.loop_stop()
    
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

        #self.client.subscribe("commands/" + self.tenantId + "/")
        self.client.subscribe("control/+/+/req/#")

        # Time stamp when the periodAction function shall be called again
        self.next_call = time.time()
        
        # Start the periodic task for publishing MQTT messages
        self.periodicAction()

    def on_disconnect(self, client, userdata, rc):
        if rc != 0:
            print("Unexpected MQTT disconnection. Will auto-reconnect")
            self.client.reconnect()

    # The callback for when a PUBLISH message is received from the server.
    def on_message(self, client, userdata, msg):
        """
        Function to run on when message is received
    
        Args:
            client: MQTT client
            userdata: User who published the message on broker
            msg: MQTT message payload

        """
        
        print("Message received:")
        print(msg.topic + " " + str(msg.payload))

        # parse Bosch IoT Hub's message ID for response
        messageId = msg.topic[msg.topic.find("req/")+4:]
        messageId = messageId[0:messageId.find("/")]

        # if this is a 2-way command, respond
        if (messageId != ""):
            print("Sender expects reply, responding to message with ID: " + messageId)

            # create MQTT response topic
            resTopic = "control///res/" + messageId + "/200"

            # parse Bosch IoT Things correlation ID for response
            reqPayload = str(msg.payload)

            # 17 is the length of 'correlation-id' and subsequent '":"'
            correlationId = reqPayload[reqPayload.find("correlation-id")+17:reqPayload.find("correlation-id")+17+36]

            print("Sender expects reply, responding to message with Correlation-Id: " + correlationId)

            self.periodicAction()

            # create Ditto compliant MQTT response payload
            resPayload = "{\"topic\":\"" + self.ditto_topic + "/things/live/messages/switch\","
            resPayload += "\"headers\":{\"correlation-id\":\"" + correlationId + "\","
            resPayload += "\"version\":2,\"content-type\":\"text/plain\"},"
            resPayload += "\"path\":\"/inbox/messages/switch\","
            resPayload += "\"value\":\"" + str(self.infomodel.sensorValue) + "\","
            resPayload += "\"status\": 200 }"

            self.client.publish(resTopic, resPayload)
            print("Response published!")

    def on_publish(self, client, userdata, mid):
        print("mid: "+str(mid))

    def on_subscribe(self, client, userdata, mid, granted_qos):
        print("Subscribed: "+str(mid)+" "+str(granted_qos))

    def on_log(self, client, userdata, level, string):
        logging.info(string)
    
    # The functions to publish the functionblocks data
    def publishGenericsensor(self):
        """
        Function to publish data to MQTT broker

        Generates payload from data in infomodel
        """
        payload = self.ser.serialize_functionblock("genericsensor", self.infomodel, self.ditto_topic, self.deviceId)
        print("Publish Payload: ", payload, " to Topic: ", self.publishTopic)
        self.client.publish(self.publishTopic, payload)
    
    # The function that will be executed periodically once the connection to the MQTT broker was established
    def periodicAction(self):
        """

        Function to run periodicaly

        Calls ImageClassification and sets current date and time.
        Publish to topic.
        Schedule next call.
        """

        self.readDone = False

        print("Reading data...")

        now = datetime.datetime.now()
        
        screen = ""
        L = ["181"]
        while (screen not in L):
            self.infomodel.sensorValue, screen = yolo.process_img(self.YoloModel, self.ImageAcquisition.ReadFromURL(self.cameraURL))
            print(self.infomodel.sensorValue, screen)
        
        #self.infomodel.sensorValue = 5000
        self.infomodel.sensorUnits = "kWh"
        self.infomodel.lastValueDate = now.strftime("%d-%m-%Y")
        self.infomodel.lastValueTime = now.strftime("%H:%M:%S")

        print("Read done!")

        # Publish payload
        self.publishGenericsensor()

        self.readDone = True

        # Schedule next call
        self.next_call = self.next_call + self.timePeriod
        threading.Timer(self.timePeriod, self.periodicAction).start()

def main():
    """
    Main function to test class
    """
    try:
        mqtt = MQTT()
        while (not(mqtt.readDone)):
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