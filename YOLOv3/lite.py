import numpy as np
import tflite_runtime.interpreter as tflite
from PIL import Image
import time

from yolo3.postprocess_np import yolo3_postprocess_np
from common.utils import get_classes, get_anchors, get_colors, draw_boxes
from common.data_utils import preprocess_image

def load_labels(filename):
    with open(filename, 'r') as f:
        return [line.strip() for line in f.readlines()]

# Load TFLite model and allocate tensors.
interpreter = tflite.Interpreter(model_path="weights/emsfiiot_lite.h5")
interpreter.allocate_tensors()

# Get input and output tensors.
input_details = interpreter.get_input_details()
output_details = interpreter.get_output_details()

# check the type of the input tensor
floating_model = input_details[0]['dtype'] == np.float32

input_shape = input_details[0]['shape']

#input_data = np.array(np.random.random_sample(input_shape), dtype=np.float32)
height = input_details[0]['shape'][1]
width = input_details[0]['shape'][2]
model_image_size = (width, height)
image = Image.open("data/EMSfIIoT/20200331013237_192.168.1.2.jpg")

image_data = preprocess_image(image, model_image_size)
image_shape = image.size

print(image_data.shape)

interpreter.set_tensor(input_details[0]['index'], image_data)

while True:
    start = time.time()
    interpreter.invoke()
    end = time.time()

    # The function `get_tensor()` returns a copy of the tensor data.
    # Use `tensor()` in order to get a pointer to the tensor.
    output_data = [interpreter.get_tensor(output_details[2]['index']), interpreter.get_tensor(output_details[0]['index']), interpreter.get_tensor(output_details[1]['index'])]

    anchors = get_anchors("configs/yolo3_anchors.txt")
    class_names = get_classes("configs/emsfiiot_classes.txt")
    colors = get_colors(class_names)
    out_boxes, out_classes, out_scores = yolo3_postprocess_np(output_data, image_shape, anchors, len(class_names), model_image_size, max_boxes=20)

    print('Found {} boxes for {}'.format(len(out_boxes), 'img'))
    print("Inference time: {:.8f}s".format(end - start))

    image_array = np.array(image, dtype='uint8')
    image_array = draw_boxes(image_array, out_boxes, out_classes, out_scores, class_names, colors)

Image.fromarray(image_array).show()