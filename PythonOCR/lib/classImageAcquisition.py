import configparser
import urllib.request
from multiprocessing import Process, Event
from PIL import Image
import os, time

class ImageAcquisition:
    """
    ImageAcquisition Class

    Retrives image from HTTP request

    Attributes:
        TimeoutLoadImage: HTTP request timeout
        URLImageSource: URL to get image from
        logImage: Where to save the image
        logOnlyFalsePictures: Save only errors
        lastImageSaved: Last image saved log
    """

    def __init__(self):
        """
        Class constructor

        Reads configuration file and sets variables
        """
        config = configparser.ConfigParser()
        config.read('./config/config.ini')

        self.TimeoutLoadImage = 30
        if config.has_option('esp32', 'TimeoutLoadImage'):
            self.TimeoutLoadImage = int(config['esp32']['TimeoutLoadImage'])
            
        self.URLImageSource = ""
        if config.has_option('esp32', 'URLImageSource'):
            self.URLImageSource = config['esp32']['URLImageSource']

        self.logImage = ""
        if config.has_option('esp32', 'LogImageLocation'):
            self.logImage = config['esp32']['LogImageLocation']   

        self.logOnlyFalsePictures = False
        if config.has_option('esp32', 'logOnlyFalsePictures'):
            self.logOnlyFalsePictures = bool(config['esp32']['logOnlyFalsePictures'])
        
        self.lastImageSaved = ""

    def ReadURL(self, event, url, target):
        """
        Read URL function

        Requests the url and returns image to target

        Args:
            event: Thread to set
            url: URL to get
            target: Image destination file
        """
        urllib.request.urlretrieve(url, target)
        event.set()

    def LoadImageFromURL(self, url, target):
        """
        Load Image from URL

        Starts the thread to get URL

        Args:
            url: URL to get
            target: Image destination file

        If no url is set uses one from config file
        """
        self.lastImageSaved = ''
        if not url:
            url = self.URLImageSource
        event = Event()
        action_process = Process(target=self.ReadURL, args=(event, url, target))
        action_process.start()
        action_process.join(timeout=self.TimeoutLoadImage)
        action_process.close()

        logtime = time.strftime('%Y-%m-%d_%H-%M-%S', time.localtime())
        if event.is_set():
            if self.VerifyImage(target) == True:
                result = ''
            else:
                result = 'Error - Image file is corrupted'
        else:
            result = 'Error - Problem during Image load (file not exists or timeout)'
        return (result, logtime)

    def VerifyImage(self, img_file):
        """
        Verify Image

        Checks if file is an image

        Args:
            img_file: Image to check

        Returns:
            True: if file is image
            False: if not
        """
        try:
            v_image = Image.open(img_file)
            v_image.verify()
            return True
        except OSError:
            return False