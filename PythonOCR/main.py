import lib.classMQTT
import lib.classImageClassification
import sys

def main():
    """
    Main function

    Starts MQTT connection and sends read every 30secs
    """
    try:
        #image = lib.classImageClassification.ImageClassification()
        #image.readImage()

        lib.classMQTT.MQTT()

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