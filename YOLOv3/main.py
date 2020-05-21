from tools.BoschIoTSuite.Hub import Hub
from tools.BoschIoTSuite.Things import Things
import sys, time, logging
from logging.handlers import RotatingFileHandler

def main():
    """
    Main function

    Starts MQTT connection and sends read every 30secs
    """

    # create logger with 'spam_application'
    log = logging.getLogger('EMSfIIoT')
    log.setLevel(logging.INFO)

    # create formatter and add it to the handlers
    formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')

    # create file handler which logs even debug messages
    fh = RotatingFileHandler('logs/EMSfIIoT.log', mode='a', maxBytes=5*1024*1024, backupCount=2, encoding=None, delay=0)
    fh.setLevel(logging.DEBUG)
    fh.setFormatter(formatter)
    log.addHandler(fh)

    # create console handler with a higher log level
    ch = logging.StreamHandler()
    ch.setLevel(logging.DEBUG)
    ch.setFormatter(formatter)
    log.addHandler(ch)

    try:

        configuration = Things().get()

        hub = Hub(configuration)
        hub.start()

        while (1):
            pass
    except KeyboardInterrupt:
        hub.stop()
        print("Exiting...")
        sys.exit(0)
    except Exception as e:
        logging.error(e)
        sys.exit(1)
    finally:
        sys.exit(0)
        

# Main body
if __name__ == '__main__':
    main()