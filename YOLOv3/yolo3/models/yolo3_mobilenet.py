#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""YOLO_v3 MobileNet Model Defined in Keras."""

from tensorflow.keras.layers import UpSampling2D, Concatenate
from tensorflow.keras.models import Model
from tensorflow.keras.applications.mobilenet import MobileNet

from yolo3.models.layers import compose, DarknetConv2D, DarknetConv2D_BN_Leaky, Depthwise_Separable_Conv2D_BN_Leaky, make_last_layers, make_depthwise_separable_last_layers, make_spp_depthwise_separable_last_layers


def yolo3_mobilenet_body(inputs, num_anchors, num_classes, alpha=1.0):
    """Create YOLO_V3 MobileNet model CNN body in Keras."""
    '''
    Layer Name: input_1 Output: Tensor("input_1:0", shape=(?, 416, 416, 3), dtype=float32)
    Layer Name: conv1_pad Output: Tensor("conv1_pad/Pad:0", shape=(?, 417, 417, 3), dtype=float32)
    Layer Name: conv1 Output: Tensor("conv1/convolution:0", shape=(?, 208, 208, 32), dtype=float32)
    Layer Name: conv1_bn Output: Tensor("conv1_bn/cond/Merge:0", shape=(?, 208, 208, 32), dtype=float32)
    Layer Name: conv1_relu Output: Tensor("conv1_relu/Minimum:0", shape=(?, 208, 208, 32), dtype=float32)
    Layer Name: conv_pad_1 Output: Tensor("conv_pad_1/Pad:0", shape=(?, 210, 210, 32), dtype=float32)
    Layer Name: conv_dw_1 Output: Tensor("conv_dw_1/depthwise:0", shape=(?, 208, 208, 32), dtype=float32)
    Layer Name: conv_dw_1_bn Output: Tensor("conv_dw_1_bn/cond/Merge:0", shape=(?, 208, 208, 32), dtype=float32)
    Layer Name: conv_dw_1_relu Output: Tensor("conv_dw_1_relu/Minimum:0", shape=(?, 208, 208, 32), dtype=float32)
    Layer Name: conv_pw_1 Output: Tensor("conv_pw_1/convolution:0", shape=(?, 208, 208, 64), dtype=float32)
    Layer Name: conv_pw_1_bn Output: Tensor("conv_pw_1_bn/cond/Merge:0", shape=(?, 208, 208, 64), dtype=float32)
    Layer Name: conv_pw_1_relu Output: Tensor("conv_pw_1_relu/Minimum:0", shape=(?, 208, 208, 64), dtype=float32)
    Layer Name: conv_pad_2 Output: Tensor("conv_pad_2/Pad:0", shape=(?, 210, 210, 64), dtype=float32)
    Layer Name: conv_dw_2 Output: Tensor("conv_dw_2/depthwise:0", shape=(?, 104, 104, 64), dtype=float32)
    Layer Name: conv_dw_2_bn Output: Tensor("conv_dw_2_bn/cond/Merge:0", shape=(?, 104, 104, 64), dtype=float32)
    Layer Name: conv_dw_2_relu Output: Tensor("conv_dw_2_relu/Minimum:0", shape=(?, 104, 104, 64), dtype=float32)
    Layer Name: conv_pw_2 Output: Tensor("conv_pw_2/convolution:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_pw_2_bn Output: Tensor("conv_pw_2_bn/cond/Merge:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_pw_2_relu Output: Tensor("conv_pw_2_relu/Minimum:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_pad_3 Output: Tensor("conv_pad_3/Pad:0", shape=(?, 106, 106, 128), dtype=float32)
    Layer Name: conv_dw_3 Output: Tensor("conv_dw_3/depthwise:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_dw_3_bn Output: Tensor("conv_dw_3_bn/cond/Merge:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_dw_3_relu Output: Tensor("conv_dw_3_relu/Minimum:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_pw_3 Output: Tensor("conv_pw_3/convolution:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_pw_3_bn Output: Tensor("conv_pw_3_bn/cond/Merge:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_pw_3_relu Output: Tensor("conv_pw_3_relu/Minimum:0", shape=(?, 104, 104, 128), dtype=float32)
    Layer Name: conv_pad_4 Output: Tensor("conv_pad_4/Pad:0", shape=(?, 106, 106, 128), dtype=float32)
    Layer Name: conv_dw_4 Output: Tensor("conv_dw_4/depthwise:0", shape=(?, 52, 52, 128), dtype=float32)
    Layer Name: conv_dw_4_bn Output: Tensor("conv_dw_4_bn/cond/Merge:0", shape=(?, 52, 52, 128), dtype=float32)
    Layer Name: conv_dw_4_relu Output: Tensor("conv_dw_4_relu/Minimum:0", shape=(?, 52, 52, 128), dtype=float32)
    Layer Name: conv_pw_4 Output: Tensor("conv_pw_4/convolution:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_pw_4_bn Output: Tensor("conv_pw_4_bn/cond/Merge:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_pw_4_relu Output: Tensor("conv_pw_4_relu/Minimum:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_pad_5 Output: Tensor("conv_pad_5/Pad:0", shape=(?, 54, 54, 256), dtype=float32)
    Layer Name: conv_dw_5 Output: Tensor("conv_dw_5/depthwise:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_dw_5_bn Output: Tensor("conv_dw_5_bn/cond/Merge:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_dw_5_relu Output: Tensor("conv_dw_5_relu/Minimum:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_pw_5 Output: Tensor("conv_pw_5/convolution:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_pw_5_bn Output: Tensor("conv_pw_5_bn/cond/Merge:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_pw_5_relu Output: Tensor("conv_pw_5_relu/Minimum:0", shape=(?, 52, 52, 256), dtype=float32)
    Layer Name: conv_pad_6 Output: Tensor("conv_pad_6/Pad:0", shape=(?, 54, 54, 256), dtype=float32)
    Layer Name: conv_dw_6 Output: Tensor("conv_dw_6/depthwise:0", shape=(?, 26, 26, 256), dtype=float32)
    Layer Name: conv_dw_6_bn Output: Tensor("conv_dw_6_bn/cond/Merge:0", shape=(?, 26, 26, 256), dtype=float32)
    Layer Name: conv_dw_6_relu Output: Tensor("conv_dw_6_relu/Minimum:0", shape=(?, 26, 26, 256), dtype=float32)
    Layer Name: conv_pw_6 Output: Tensor("conv_pw_6/convolution:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_6_bn Output: Tensor("conv_pw_6_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_6_relu Output: Tensor("conv_pw_6_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pad_7 Output: Tensor("conv_pad_7/Pad:0", shape=(?, 28, 28, 512), dtype=float32)
    Layer Name: conv_dw_7 Output: Tensor("conv_dw_7/depthwise:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_7_bn Output: Tensor("conv_dw_7_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_7_relu Output: Tensor("conv_dw_7_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_7 Output: Tensor("conv_pw_7/convolution:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_7_bn Output: Tensor("conv_pw_7_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_7_relu Output: Tensor("conv_pw_7_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pad_8 Output: Tensor("conv_pad_8/Pad:0", shape=(?, 28, 28, 512), dtype=float32)
    Layer Name: conv_dw_8 Output: Tensor("conv_dw_8/depthwise:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_8_bn Output: Tensor("conv_dw_8_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_8_relu Output: Tensor("conv_dw_8_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_8 Output: Tensor("conv_pw_8/convolution:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_8_bn Output: Tensor("conv_pw_8_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_8_relu Output: Tensor("conv_pw_8_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pad_9 Output: Tensor("conv_pad_9/Pad:0", shape=(?, 28, 28, 512), dtype=float32)
    Layer Name: conv_dw_9 Output: Tensor("conv_dw_9/depthwise:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_9_bn Output: Tensor("conv_dw_9_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_9_relu Output: Tensor("conv_dw_9_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_9 Output: Tensor("conv_pw_9/convolution:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_9_bn Output: Tensor("conv_pw_9_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_9_relu Output: Tensor("conv_pw_9_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pad_10 Output: Tensor("conv_pad_10/Pad:0", shape=(?, 28, 28, 512), dtype=float32)
    Layer Name: conv_dw_10 Output: Tensor("conv_dw_10/depthwise:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_10_bn Output: Tensor("conv_dw_10_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_10_relu Output: Tensor("conv_dw_10_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_10 Output: Tensor("conv_pw_10/convolution:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_10_bn Output: Tensor("conv_pw_10_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_10_relu Output: Tensor("conv_pw_10_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pad_11 Output: Tensor("conv_pad_11/Pad:0", shape=(?, 28, 28, 512), dtype=float32)
    Layer Name: conv_dw_11 Output: Tensor("conv_dw_11/depthwise:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_11_bn Output: Tensor("conv_dw_11_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_dw_11_relu Output: Tensor("conv_dw_11_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_11 Output: Tensor("conv_pw_11/convolution:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_11_bn Output: Tensor("conv_pw_11_bn/cond/Merge:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pw_11_relu Output: Tensor("conv_pw_11_relu/Minimum:0", shape=(?, 26, 26, 512), dtype=float32)
    Layer Name: conv_pad_12 Output: Tensor("conv_pad_12/Pad:0", shape=(?, 28, 28, 512), dtype=float32)
    Layer Name: conv_dw_12 Output: Tensor("conv_dw_12/depthwise:0", shape=(?, 13, 13, 512), dtype=float32)
    Layer Name: conv_dw_12_bn Output: Tensor("conv_dw_12_bn/cond/Merge:0", shape=(?, 13, 13, 512), dtype=float32)
    Layer Name: conv_dw_12_relu Output: Tensor("conv_dw_12_relu/Minimum:0", shape=(?, 13, 13, 512), dtype=float32)
    Layer Name: conv_pw_12 Output: Tensor("conv_pw_12/convolution:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: conv_pw_12_bn Output: Tensor("conv_pw_12_bn/cond/Merge:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: conv_pw_12_relu Output: Tensor("conv_pw_12_relu/Minimum:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: conv_pad_13 Output: Tensor("conv_pad_13/Pad:0", shape=(?, 15, 15, 1024), dtype=float32)
    Layer Name: conv_dw_13 Output: Tensor("conv_dw_13/depthwise:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: conv_dw_13_bn Output: Tensor("conv_dw_13_bn/cond/Merge:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: conv_dw_13_relu Output: Tensor("conv_dw_13_relu/Minimum:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: conv_pw_13 Output: Tensor("conv_pw_13/convolution:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: conv_pw_13_bn Output: Tensor("conv_pw_13_bn/cond/Merge:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: conv_pw_13_relu Output: Tensor("conv_pw_13_relu/Minimum:0", shape=(?, 13, 13, 1024), dtype=float32)
    Layer Name: global_average_pooling2d_1 Output: Tensor("global_average_pooling2d_1/Mean:0", shape=(?, 1024), dtype=float32)
    Layer Name: reshape_1 Output: Tensor("reshape_1/Reshape:0", shape=(?, 1, 1, 1024), dtype=float32)
    Layer Name: dropout Output: Tensor("dropout/cond/Merge:0", shape=(?, 1, 1, 1024), dtype=float32)
    Layer Name: conv_preds Output: Tensor("conv_preds/BiasAdd:0", shape=(?, 1, 1, 1000), dtype=float32)
    Layer Name: act_softmax Output: Tensor("act_softmax/truediv:0", shape=(?, 1, 1, 1000), dtype=float32)
    Layer Name: reshape_2 Output: Tensor("reshape_2/Reshape:0", shape=(?, 1000), dtype=float32)
    '''

    mobilenet = MobileNet(input_tensor=inputs, weights='imagenet', include_top=False, alpha=alpha)

    # input: 416 x 416 x 3
    # conv_pw_13_relu :13 x 13 x (1024*alpha)
    # conv_pw_11_relu :26 x 26 x (512*alpha)
    # conv_pw_5_relu : 52 x 52 x (256*alpha)

    f1 = mobilenet.get_layer('conv_pw_13_relu').output
    # f1 :13 x 13 x (1024*alpha)
    x, y1 = make_last_layers(f1, int(512*alpha), num_anchors * (num_classes + 5))

    x = compose(
            DarknetConv2D_BN_Leaky(int(256*alpha), (1,1)),
            UpSampling2D(2))(x)

    f2 = mobilenet.get_layer('conv_pw_11_relu').output
    # f2: 26 x 26 x (512*alpha)
    x = Concatenate()([x,f2])

    x, y2 = make_last_layers(x, int(256*alpha), num_anchors*(num_classes+5))

    x = compose(
            DarknetConv2D_BN_Leaky(int(128*alpha), (1,1)),
            UpSampling2D(2))(x)

    f3 = mobilenet.get_layer('conv_pw_5_relu').output
    # f3 : 52 x 52 x  (256*alpha)
    x = Concatenate()([x, f3])
    x, y3 = make_last_layers(x, int(128*alpha), num_anchors*(num_classes+5))

    return Model(inputs = inputs, outputs=[y1,y2,y3])


def yolo3lite_mobilenet_body(inputs, num_anchors, num_classes, alpha=1.0):
    '''Create YOLO_v3 Lite MobileNet model CNN body in keras.'''
    mobilenet = MobileNet(input_tensor=inputs, weights='imagenet', include_top=False, alpha=alpha)

    # input: 416 x 416 x 3
    # conv_pw_13_relu :13 x 13 x (1024*alpha)
    # conv_pw_11_relu :26 x 26 x (512*alpha)
    # conv_pw_5_relu : 52 x 52 x (256*alpha)

    f1 = mobilenet.get_layer('conv_pw_13_relu').output
    # f1 :13 x 13 x (1024*alpha)
    x, y1 = make_depthwise_separable_last_layers(f1, int(512*alpha), num_anchors * (num_classes + 5), block_id_str='14')

    x = compose(
            DarknetConv2D_BN_Leaky(int(256*alpha), (1,1)),
            UpSampling2D(2))(x)

    f2 = mobilenet.get_layer('conv_pw_11_relu').output
    # f2: 26 x 26 x (512*alpha)
    x = Concatenate()([x,f2])

    x, y2 = make_depthwise_separable_last_layers(x, int(256*alpha), num_anchors * (num_classes + 5), block_id_str='15')

    x = compose(
            DarknetConv2D_BN_Leaky(int(128*alpha), (1,1)),
            UpSampling2D(2))(x)

    f3 = mobilenet.get_layer('conv_pw_5_relu').output
    # f3 : 52 x 52 x (256*alpha)
    x = Concatenate()([x, f3])
    x, y3 = make_depthwise_separable_last_layers(x, int(128*alpha), num_anchors * (num_classes + 5), block_id_str='16')

    return Model(inputs = inputs, outputs=[y1,y2,y3])


def yolo3lite_spp_mobilenet_body(inputs, num_anchors, num_classes, alpha=1.0):
    '''Create YOLO_v3 Lite SPP MobileNet model CNN body in keras.'''
    mobilenet = MobileNet(input_tensor=inputs, weights='imagenet', include_top=False, alpha=alpha)

    # input: 416 x 416 x 3
    # conv_pw_13_relu :13 x 13 x (1024*alpha)
    # conv_pw_11_relu :26 x 26 x (512*alpha)
    # conv_pw_5_relu : 52 x 52 x (256*alpha)

    f1 = mobilenet.get_layer('conv_pw_13_relu').output
    # f1 :13 x 13 x (1024*alpha)
    #x, y1 = make_depthwise_separable_last_layers(f1, int(512*alpha), num_anchors * (num_classes + 5), block_id_str='14')
    x, y1 = make_spp_depthwise_separable_last_layers(f1, int(512*alpha), num_anchors * (num_classes + 5), block_id_str='14')

    x = compose(
            DarknetConv2D_BN_Leaky(int(256*alpha), (1,1)),
            UpSampling2D(2))(x)

    f2 = mobilenet.get_layer('conv_pw_11_relu').output
    # f2: 26 x 26 x (512*alpha)
    x = Concatenate()([x,f2])

    x, y2 = make_depthwise_separable_last_layers(x, int(256*alpha), num_anchors * (num_classes + 5), block_id_str='15')

    x = compose(
            DarknetConv2D_BN_Leaky(int(128*alpha), (1,1)),
            UpSampling2D(2))(x)

    f3 = mobilenet.get_layer('conv_pw_5_relu').output
    # f3 : 52 x 52 x (256*alpha)
    x = Concatenate()([x, f3])
    x, y3 = make_depthwise_separable_last_layers(x, int(128*alpha), num_anchors * (num_classes + 5), block_id_str='16')

    return Model(inputs = inputs, outputs=[y1,y2,y3])


def tiny_yolo3_mobilenet_body(inputs, num_anchors, num_classes, alpha=1.0):
    '''Create Tiny YOLO_v3 MobileNet model CNN body in keras.'''
    mobilenet = MobileNet(input_tensor=inputs, weights='imagenet', include_top=False, alpha=alpha)

    # input: 416 x 416 x 3
    # conv_pw_13_relu :13 x 13 x (1024*alpha)
    # conv_pw_11_relu :26 x 26 x (512*alpha)
    # conv_pw_5_relu : 52 x 52 x (256*alpha)

    x1 = mobilenet.get_layer('conv_pw_11_relu').output

    x2 = mobilenet.get_layer('conv_pw_13_relu').output
    x2 = DarknetConv2D_BN_Leaky(int(512*alpha), (1,1))(x2)

    y1 = compose(
            DarknetConv2D_BN_Leaky(int(1024*alpha), (3,3)),
            #Depthwise_Separable_Conv2D_BN_Leaky(filters=int(1024*alpha), kernel_size=(3, 3), block_id_str='14'),
            DarknetConv2D(num_anchors*(num_classes+5), (1,1)))(x2)

    x2 = compose(
            DarknetConv2D_BN_Leaky(int(256*alpha), (1,1)),
            UpSampling2D(2))(x2)
    y2 = compose(
            Concatenate(),
            DarknetConv2D_BN_Leaky(int(512*alpha), (3,3)),
            #Depthwise_Separable_Conv2D_BN_Leaky(filters=int(512*alpha), kernel_size=(3, 3), block_id_str='15'),
            DarknetConv2D(num_anchors*(num_classes+5), (1,1)))([x2,x1])

    return Model(inputs, [y1,y2])


def tiny_yolo3lite_mobilenet_body(inputs, num_anchors, num_classes, alpha=1.0):
    '''Create Tiny YOLO_v3 Lite MobileNet model CNN body in keras.'''
    mobilenet = MobileNet(input_tensor=inputs, weights='imagenet', include_top=False, alpha=alpha)

    # input: 416 x 416 x 3
    # conv_pw_13_relu :13 x 13 x (1024*alpha)
    # conv_pw_11_relu :26 x 26 x (512*alpha)
    # conv_pw_5_relu : 52 x 52 x (256*alpha)

    x1 = mobilenet.get_layer('conv_pw_11_relu').output

    x2 = mobilenet.get_layer('conv_pw_13_relu').output
    x2 = DarknetConv2D_BN_Leaky(int(512*alpha), (1,1))(x2)

    y1 = compose(
            #DarknetConv2D_BN_Leaky(int(1024*alpha), (3,3)),
            Depthwise_Separable_Conv2D_BN_Leaky(filters=int(1024*alpha), kernel_size=(3, 3), block_id_str='14'),
            DarknetConv2D(num_anchors*(num_classes+5), (1,1)))(x2)

    x2 = compose(
            DarknetConv2D_BN_Leaky(int(256*alpha), (1,1)),
            UpSampling2D(2))(x2)
    y2 = compose(
            Concatenate(),
            #DarknetConv2D_BN_Leaky(int(512*alpha), (3,3)),
            Depthwise_Separable_Conv2D_BN_Leaky(filters=int(512*alpha), kernel_size=(3, 3), block_id_str='15'),
            DarknetConv2D(num_anchors*(num_classes+5), (1,1)))([x2,x1])

    return Model(inputs, [y1,y2])

