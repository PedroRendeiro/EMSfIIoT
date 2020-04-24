import tools.BoschIoTSuite.MQTT as MQTT
import sys, time

def main():
    """
    Main function

    Starts MQTT connection and sends read every 30secs
    """
    try:
        mqtt = MQTT.MQTT()
        mqtt.start()

        while (1):
            pass
    except KeyboardInterrupt:
        mqtt.stop()
        print("Exiting...")
        sys.exit(0)
        time.sleep(2)
    else:
        sys.exit(1)
    finally:
        sys.exit(0)
        

# Main body
if __name__ == '__main__':
    main()