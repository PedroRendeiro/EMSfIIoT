from PIL import Image
import os, time, requests, logging
from io import BytesIO

class ImageAcquisition():

    def __init__(self):
        self.log = logging.getLogger('EMSfIIoT')

    def ReadFromURL(self, url):
        headers = {'Authorization': 'Basic Tm9kZVJlZDpUPkg8QmRKKE0ocjhDdXNi'}
        response = requests.get(url, headers=headers)

        if response.status_code == 200:
            self.log.info('Capture Done, URL: ' + url)
        else:
            self.log.error("Status Code: " + str(response.status_code) + " | Body: " + response.content.decode('utf-8'))
            return

        img_bytes = BytesIO(response.content)
        assert self.verify(img_bytes)

        return img_bytes


    def verify(self, image):
        try:
            v_image = Image.open(image)
            v_image.verify()
            return True
        except OSError:
            return False