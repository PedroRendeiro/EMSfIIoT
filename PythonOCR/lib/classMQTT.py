import paho.mqtt.client as mqtt

import datetime, threading, time, sys, configparser

import lib.classDittoSerializer as DittoSerializer
import lib.GenericSensor as GenericSensor

import lib.classImageClassification as ImageClassification

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
        config = configparser.ConfigParser()
        config.read("./config/config.ini")

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

        self.readDone = False

        self.ImageClassification = ImageClassification.ImageClassification()

        # Initialization of Information Model
        self.infomodel = GenericSensor.GenericSensor()

        # Create a serializer for the MQTT payload from the Information Model
        self.ser = DittoSerializer.DittoSerializer()

        # Timer variable for periodic function
        self.next_call = 0
        # Period for publishing data to the MQTT broker in seconds
        self.timePeriod = 25

        # Configuration of client ID and publish topic	
        self.publishTopic = "telemetry/" + self.tenantId + "/" + self.deviceId

        # Create the MQTT client
        self.client = mqtt.Client(self.clientId)
        self.client.on_connect = self.on_connect
        self.client.on_message = self.on_message

        # Output relevant information for consumers of our information
        print("Connecting client:    ", self.clientId)
        print("Publishing to topic:  ", self.publishTopic)

        self.client.username_pw_set(self.username, self.device_password)
        self.client.tls_set(self.certificatePath)

        # Connect to the MQTT broker
        self.client.connect(self.hub_adapter_host, 8883, 60)

        # Blocking call that processes network traffic, dispatches callbacks and
        # handles reconnecting.
        # Other loop*() functions are available that give a threaded interface and a
        # manual interface.
        self.client.loop_start()

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

        # BEGIN SAMPLE CODE
        self.client.subscribe("commands/" + self.tenantId + "/")
        # END SAMPLE CODE

        # Time stamp when the periodAction function shall be called again
        self.next_call = time.time()
        
        # Start the periodic task for publishing MQTT messages
        self.periodicAction()

    # The callback for when a PUBLISH message is received from the server.
    def on_message(self, client, userdata, msg):
        """
        Function to run on when message is received
    
        Args:
            client: MQTT client
            userdata: User who published the message on broker
            msg: MQTT message payload

        """
        print(msg.topic + " " + str(msg.payload))

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
        
        self.infomodel.sensorValue = self.ImageClassification.readImage()
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