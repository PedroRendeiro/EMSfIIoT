#! /usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Convert SVHN BoundingBoxes file
"""
import os, argparse, h5py
import numpy as np
from os import getcwd
import scipy.io
from progress.bar import Bar

parser = argparse.ArgumentParser(description='convert COCO dataset annotation to txt annotation file')
parser.add_argument('--dataset_path', type=str, required=False, help='path to SVHN dataset, default is data\\SVHN', default='data\\SVHN')
parser.add_argument('--output_path', type=str, required=False,  help='output path for generated annotation txt files, default is data\\SVHN', default='data\\SVHN')
parser.add_argument('--classes_path', type=str, required=False, help='path to class definitions, default is configs\\svhn_classes.txt', default=getcwd()+'\\configs\\svhn_classes.txt')
parser.add_argument('--include_no_obj', action="store_true", help='to include no object image', default=False)
args = parser.parse_args()

def getName(n):
    return ''.join([chr(c[0]) for c in digitStruct[digitStructName[n][0]][()]])

def getBoundingBox(n):
    bbox = {}
    bb = digitStructBbox[n].item()
    bbox['height'] = parseBoundingBox(digitStruct[bb]["height"])
    bbox['label'] = parseBoundingBox(digitStruct[bb]["label"])
    bbox['left'] = parseBoundingBox(digitStruct[bb]["left"])
    bbox['top'] = parseBoundingBox(digitStruct[bb]["top"])
    bbox['width'] = parseBoundingBox(digitStruct[bb]["width"])
    return bbox

def parseBoundingBox(attr):
    if (len(attr) > 1):
        attr = [digitStruct[attr[()][j].item()][()][0][0] for j in range(len(attr))]
    else:
        attr = [attr[()][0][0]]
    
    attr = np.array(str(attr)[1:-1].split(",")).astype(np.float32).astype(np.int32)
    
    return attr

def getDigitStructure(n):
        s = getBoundingBox(n)
        s['name']=getName(n)
        return s      

if __name__ == '__main__':

    paths = ['train','test','extra']

    for path in paths:
        pathName = os.path.join(args.dataset_path, path)
        if not(os.path.isdir(pathName)):
            continue
        print('Processing', path, 'path')
        fileName = os.path.join(pathName, 'digitStruct.mat')

        digitStruct = h5py.File(fileName, 'r')
        digitStructName = digitStruct['digitStruct']['name']
        digitStructBbox = digitStruct['digitStruct']['bbox']

        fileName = path + '.txt'
        resultFilePath = os.path.join(args.dataset_path, fileName)
        f = open(resultFilePath, "w")

        bar = Bar('Processing', max=len(digitStructName))
        
        for idx in range(len(digitStructName)):
            imageData = getDigitStructure(idx)

            f.write(os.path.join(args.dataset_path, path, imageData['name']))
            
            for i in range(len(imageData['label'])):

                x_min = imageData['left'][i]
                y_min = imageData['top'][i]
                x_max = x_min + imageData['width'][i]
                y_max = y_min + imageData['height'][i]
                class_id = imageData['label'][i]

                toWrite = ' ' + str(x_min) + ',' + str(y_min) + ',' + str(x_max) + ',' + str(y_max) + ',' + str(class_id)
                f.write(toWrite)
            
            f.write('\n')
            
            bar.next()
        
        f.close()

        print('\n')