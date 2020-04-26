from PIL import Image
import os, time, requests
from io import BytesIO

class ImageAcquisition():

    def __init__(self):
        pass

    def ReadFromURL(self, url):
        headers = {'Authorization': 'Basic Tm9kZVJlZDpUPkg8QmRKKE0ocjhDdXNi'}
        response = requests.get(url, headers=headers)

        if response.status_code == 200:
            print('Capture Done, URL: ' + url)
        else:
            print('Web site does not exist')
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