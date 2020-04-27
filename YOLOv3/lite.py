import numpy as np
try:
    import tflite_runtime.interpreter as tflite
except:
    import tensorflow as tf
from PIL import Image
import time

from yolo3.postprocess_np import yolo3_postprocess_np
from common.utils import get_classes, get_anchors, get_colors, draw_boxes
from common.data_utils import preprocess_image

class YOLO_lite:
    def __init__(self):
        # Load TFLite model and allocate tensors.
        try:
            self.interpreter = tflite.Interpreter(model_path="weights/emsfiiot_lite.h5")
        except:
            self.interpreter = tf.lite.Interpreter(model_path="weights/emsfiiot_lite.h5")
        self.interpreter.allocate_tensors()

        # Get input and output tensors.
        self.input_details = self.interpreter.get_input_details()
        self.output_details = self.interpreter.get_output_details()

        # check the type of the input tensor
        self.floating_model = self.input_details[0]['dtype'] == np.float32

        height = self.input_details[0]['shape'][1]
        width = self.input_details[0]['shape'][2]
        self.model_image_size = (width, height)

        self.anchors = get_anchors("configs/yolo3_anchors.txt")
        self.class_names = get_classes("configs/emsfiiot_classes.txt")
        self.colors = get_colors(self.class_names)

    def detect_image(self, image):
        if self.model_image_size != (None, None):
            assert self.model_image_size[0]%32 == 0, 'Multiples of 32 required'
            assert self.model_image_size[1]%32 == 0, 'Multiples of 32 required'

        image = Image.open(image)

        image_data = preprocess_image(image, self.model_image_size)
        image_shape = image.size

        self.interpreter.set_tensor(self.input_details[0]['index'], image_data)

        start = time.time()
        self.interpreter.invoke()
        output_data = [self.interpreter.get_tensor(self.output_details[2]['index']), self.interpreter.get_tensor(self.output_details[0]['index']), self.interpreter.get_tensor(self.output_details[1]['index'])]
        out_boxes, out_classes, out_scores = yolo3_postprocess_np(output_data, image_shape, self.anchors, len(self.class_names), self.model_image_size, max_boxes=20, confidence=0.35)
        print('Found {} boxes for {}'.format(len(out_boxes), 'img'))
        end = time.time()
        print("Inference time: {:.8f}s".format(end - start))

        if out_classes is None or len(out_classes) == 0:
            return image_data, None, None

        order = out_boxes[:,0].argsort()
        out_boxes = out_boxes[order]
        out_classes = out_classes[order]
        out_scores = out_scores[order]

        yref_top = min(out_boxes[:,1])
        yref_top_idx = np.argmin(out_boxes[:,1])

        r_number, r_screen = ([] for i in range(2))

        for idx, box in enumerate(out_boxes):
            _, ymin, _, ymax = box

            if ymin < 195:
                r_screen.append(out_classes[idx])
            else:
                r_number.append(out_classes[idx])
        
        if len(r_number) < 1 or len(r_screen) < 1:
            r_number, r_screen = ("" for i in range(2))
        else:
            #r_number = int("".join("{0}".format(n) for n in r_number))
            r_number = "".join(str(n) for n in r_number)
            r_screen = "".join(str(n) for n in r_screen)

        #draw result on input image
        image_array = np.array(image, dtype='uint8')
        image_array = draw_boxes(image_array, out_boxes, out_classes, out_scores, self.class_names, self.colors)
        return Image.fromarray(image_array), r_number, r_screen

if __name__ == '__main__':

    yolo = YOLO_lite()

    image = "capture.jpg"
    
    r_image, r_number, r_screen = yolo.detect_image(image)

    print(r_number, r_screen)

    r_image.show()