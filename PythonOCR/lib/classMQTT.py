import paho.mqtt.client as mqtt

import datetime, threading, time, sys, configparser

import classDittoSerializer as DittoSerializer
import GenericSensor as GenericSensor

import classImageClassification

class MQTT():
    def __init__(self):
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

        self.ImageClassification = classImageClassification.ImageClassification()

        # Initialization of Information Model
        self.infomodel = GenericSensor.GenericSensor()

        # Create a serializer for the MQTT payload from the Information Model
        self.ser = DittoSerializer.DittoSerializer()

        # Timer variable for periodic function
        self.next_call = 0
        # Period for publishing data to the MQTT broker in seconds
        self.timePeriod = 10

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
        
        ### BEGIN SAMPLE CODE
        
        print(msg.topic + " " + str(msg.payload))

        ### END SAMPLE CODE

    # The functions to publish the functionblocks data
    def publishGenericsensor(self):
        payload = self.ser.serialize_functionblock("genericsensor", self.infomodel, self.ditto_topic, self.deviceId)
        print("Publish Payload: ", payload, " to Topic: ", self.publishTopic)
        self.client.publish(self.publishTopic, payload)
    
    # The function that will be executed periodically once the connection to the MQTT broker was established
    def periodicAction(self):

        self.readDone = False
        
        ### BEGIN READING SENSOR DATA

        print("Reading data...")

        now = datetime.datetime.now()
        
        self.infomodel.sensorValue = self.ImageClassification.readImage()
        self.infomodel.sensorUnits = "kWh"
        self.infomodel.lastValueDate = now.strftime("%d-%m-%Y")
        self.infomodel.lastValueTime = now.strftime("%H:%M:%S")

        print("Read done!")

        ### END READING SENSOR DATA

        # Publish payload
        self.publishGenericsensor()

        self.readDone = True

        # Schedule next call
        self.next_call = self.next_call + self.timePeriod
        threading.Timer(self.timePeriod, self.periodicAction).start()

def main():
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