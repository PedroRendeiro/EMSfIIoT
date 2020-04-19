from PIL import Image
import os, time, requests
from io import BytesIO

class ImageAcquisition():

    def __init__(self):
        pass

    def ReadFromURL(self, url):
        response = requests.get(url)

        if response.status_code == 200:
            print('Capture Done')
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

response = requests.get("http://loja:drogaria321@loja.drogariasantoantonio.pt/ISAPI/Streaming/channels/301/Picture")